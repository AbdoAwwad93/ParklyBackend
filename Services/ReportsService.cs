using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<RevenueReportsDTO>> GetRevenueReportAsync(Guid ownerId, string? period, DateTime? from, DateTime? to)
        {
            var normalizedPeriod = NormalizePeriod(period);
            if (normalizedPeriod == null)
            {
                return ApiResponse<RevenueReportsDTO>.Failure("Period must be 'daily', 'weekly', or 'monthly'.");
            }

            var (rangeStart, rangeEnd) = ResolveRange(normalizedPeriod, from, to);
            if (rangeStart >= rangeEnd)
            {
                return ApiResponse<RevenueReportsDTO>.Failure("The report end date must be after the start date.");
            }

            var reservations = await GetOwnerReservationsInRangeAsync(ownerId, rangeStart, rangeEnd);
            var parkings = await _unitOfWork.Parkings.Query()
                .Include(p => p.ParkingSpaces)
                .Where(p => p.OwnerId == ownerId)
                .ToListAsync();
            var completed = reservations
                .Where(r => r.Status == ReservationStatus.Completed)
                .ToList();

            var previousStart = rangeStart.AddTicks(-(rangeEnd - rangeStart).Ticks);
            var previousReservations = await GetOwnerReservationsInRangeAsync(ownerId, previousStart, rangeStart);
            var previousCompleted = previousReservations
                .Where(r => r.Status == ReservationStatus.Completed)
                .ToList();

            var buckets = BuildBuckets(normalizedPeriod, rangeStart, rangeEnd);
            var revenueSeries = buckets.Select(bucket => new RevenueReportDataPointDTO
            {
                Label = bucket.Label,
                From = bucket.From,
                To = bucket.To,
                Revenue = completed
                    .Where(r => r.DepartureTime >= bucket.From && r.DepartureTime < bucket.To)
                    .Sum(r => r.TotalPrice)
            }).ToList();

            var bookingSeries = buckets.Select(bucket => new BookingVolumeDataPointDTO
            {
                Label = bucket.Label,
                From = bucket.From,
                To = bucket.To,
                Bookings = reservations.Count(r => r.ArrivalTime >= bucket.From && r.ArrivalTime < bucket.To)
            }).ToList();

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            var currentMonthReservations = await GetOwnerReservationsInRangeAsync(ownerId, monthStart, now);
            var previousMonthReservations = await GetOwnerReservationsInRangeAsync(ownerId, monthStart.AddMonths(-1), monthStart);
            var thisMonthTotal = currentMonthReservations
                .Where(r => r.Status == ReservationStatus.Completed)
                .Sum(r => r.TotalPrice);
            var previousMonthTotal = previousMonthReservations
                .Where(r => r.Status == ReservationStatus.Completed)
                .Sum(r => r.TotalPrice);

            var totalRevenue = completed.Sum(r => r.TotalPrice);
            var totalBookings = reservations.Count;
            var locationRows = BuildLocationRows(parkings, reservations, totalRevenue);

            var report = new RevenueReportsDTO
            {
                Period = normalizedPeriod,
                From = rangeStart,
                To = rangeEnd,
                Summary = new RevenueReportSummaryDTO
                {
                    TotalRevenue = totalRevenue,
                    TotalRevenueGrowthPercent = CalculateGrowth(totalRevenue, previousCompleted.Sum(r => r.TotalPrice)),
                    TotalBookings = totalBookings,
                    TotalBookingsGrowthPercent = CalculateGrowth(totalBookings, previousReservations.Count),
                    AverageRevenuePerPeriod = buckets.Count == 0 ? 0 : Math.Round(totalRevenue / buckets.Count, 2),
                    ThisMonthTotal = thisMonthTotal,
                    ThisMonthGrowthPercent = CalculateGrowth(thisMonthTotal, previousMonthTotal),
                    ThisMonthLabel = $"{monthStart:MMM d}-{now:MMM d, yyyy}"
                },
                RevenueOverTime = revenueSeries,
                BookingVolume = bookingSeries,
                RevenueByLocation = locationRows
            };

            return ApiResponse<RevenueReportsDTO>.Success("Revenue report retrieved successfully.", report);
        }

        public async Task<ApiResponse<string>> ExportRevenueByLocationCsvAsync(Guid ownerId, string? period, DateTime? from, DateTime? to)
        {
            var reportResponse = await GetRevenueReportAsync(ownerId, period, from, to);
            if (!reportResponse.IsSuccess || reportResponse.Data == null)
            {
                return ApiResponse<string>.Failure(reportResponse.Message ?? "Revenue report export failed.", reportResponse.Errors);
            }

            var csv = new StringBuilder();
            csv.AppendLine("Location,City,Revenue,Share,Bookings,AveragePerBooking,Status");
            foreach (var row in reportResponse.Data.RevenueByLocation)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(row.LocationName),
                    EscapeCsv(row.City),
                    row.Revenue.ToString("0.00", CultureInfo.InvariantCulture),
                    row.SharePercent.ToString("0.##", CultureInfo.InvariantCulture),
                    row.Bookings.ToString(CultureInfo.InvariantCulture),
                    row.AveragePerBooking.ToString("0.00", CultureInfo.InvariantCulture),
                    EscapeCsv(row.Status)));
            }

            return ApiResponse<string>.Success("Revenue report exported successfully.", csv.ToString());
        }

        private async Task<List<Reservation>> GetOwnerReservationsInRangeAsync(Guid ownerId, DateTime from, DateTime to)
        {
            return await _unitOfWork.Reservations.Query()
                .Include(r => r.ParkingSpace)
                    .ThenInclude(s => s.Parking)
                        .ThenInclude(p => p.ParkingSpaces)
                .Where(r =>
                    r.ParkingSpace.Parking.OwnerId == ownerId &&
                    r.ArrivalTime < to &&
                    r.DepartureTime >= from)
                .ToListAsync();
        }

        private static string? NormalizePeriod(string? period)
        {
            var normalized = period?.Trim().ToLowerInvariant();
            return normalized switch
            {
                null or "" => "monthly",
                "daily" or "weekly" or "monthly" => normalized,
                _ => null
            };
        }

        private static (DateTime Start, DateTime End) ResolveRange(string period, DateTime? from, DateTime? to)
        {
            if (from.HasValue || to.HasValue)
            {
                var customStart = DateTime.SpecifyKind((from ?? DateTime.UtcNow.Date.AddMonths(-1)).Date, DateTimeKind.Utc);
                var customEnd = DateTime.SpecifyKind((to ?? DateTime.UtcNow.Date).Date.AddDays(1), DateTimeKind.Utc);
                return (customStart, customEnd);
            }

            var now = DateTime.UtcNow;
            return period switch
            {
                "daily" => (now.Date.AddDays(-6), now.Date.AddDays(1)),
                "weekly" => (now.Date.AddDays(-27), now.Date.AddDays(1)),
                _ => (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), now.Date.AddDays(1))
            };
        }

        private static List<(string Label, DateTime From, DateTime To)> BuildBuckets(string period, DateTime start, DateTime end)
        {
            var buckets = new List<(string Label, DateTime From, DateTime To)>();
            var cursor = start.Date;

            while (cursor < end)
            {
                DateTime next;
                string label;
                if (period == "daily")
                {
                    next = cursor.AddDays(1);
                    label = cursor.ToString("ddd");
                }
                else if (period == "weekly")
                {
                    next = cursor.AddDays(7);
                    label = $"{cursor:MMM d}";
                }
                else
                {
                    next = cursor.AddMonths(1);
                    label = cursor.ToString("MMM");
                }

                buckets.Add((label, cursor, next > end ? end : next));
                cursor = next;
            }

            return buckets;
        }

        private static List<LocationRevenueDTO> BuildLocationRows(List<Parking> parkings, List<Reservation> reservations, decimal totalRevenue)
        {
            return parkings
                .Select(parking =>
                {
                    var parkingReservations = reservations
                        .Where(r => r.ParkingSpace.ParkingId == parking.ParkingId)
                        .ToList();
                    var completedRevenue = parkingReservations
                        .Where(r => r.Status == ReservationStatus.Completed)
                        .Sum(r => r.TotalPrice);
                    var bookingCount = parkingReservations.Count;
                    var activeSpaces = parking.ParkingSpaces.Count(s => s.IsActive);

                    return new LocationRevenueDTO
                    {
                        ParkingId = parking.ParkingId,
                        LocationName = parking.Name,
                        City = ExtractCity(parking.Address),
                        Revenue = completedRevenue,
                        SharePercent = totalRevenue <= 0 ? 0 : Math.Round(completedRevenue * 100m / totalRevenue, 2),
                        Bookings = bookingCount,
                        AveragePerBooking = bookingCount == 0 ? 0 : Math.Round(completedRevenue / bookingCount, 2),
                        Status = activeSpaces > 0 ? "Active" : "Inactive"
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();
        }

        private static decimal CalculateGrowth(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                return current == 0 ? 0 : 100;
            }

            return Math.Round((current - previous) * 100m / previous, 2);
        }

        private static decimal CalculateGrowth(int current, int previous)
            => CalculateGrowth((decimal)current, previous);

        private static string ExtractCity(string address)
        {
            var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[^2]}, {parts[^1]}";
            }

            return address;
        }

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
