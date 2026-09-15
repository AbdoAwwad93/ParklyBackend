using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const decimal RevenueTarget = 1500m;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<DashboardSummaryDTO>> GetSummaryAsync(Guid ownerId)
        {
            var parkings = await _unitOfWork.Parkings.GetParkingsWithSpacesAsync();
            var ownerParkings = parkings.Where(p => p.OwnerId == ownerId).ToList();

            if (ownerParkings.Count == 0)
            {
                return ApiResponse<DashboardSummaryDTO>.Success("Dashboard summary retrieved successfully.", new DashboardSummaryDTO());
            }

            int totalSpaces = ownerParkings
                .SelectMany(p => p.ParkingSpaces)
                .Count(s => s.IsActive);

            int totalOccupied = 0;
            foreach (var parking in ownerParkings)
            {
                totalOccupied += await _unitOfWork.Reservations.GetCheckedInCountForParkingAsync(parking.ParkingId);
            }

            totalOccupied = Math.Min(totalOccupied, totalSpaces);

            int availableNow = Math.Max(0, totalSpaces - totalOccupied);
            int occupancyPct = totalSpaces > 0
                ? (int)Math.Round(totalOccupied * 100.0 / totalSpaces)
                : 0;
            double availablePct = totalSpaces > 0
                ? Math.Round(availableNow * 100.0 / totalSpaces, 1)
                : 0;

            double avgRating = 0;
            int totalReviews = ownerParkings.Sum(p => p.TotalReviews);
            if (totalReviews > 0)
            {
                avgRating = ownerParkings.Sum(p => p.AverageRating * p.TotalReviews) / totalReviews;
                avgRating = Math.Round(avgRating, 1);
            }

            int todayBookings = await _unitOfWork.Reservations.GetOwnerTodayBookingsCountAsync(ownerId);
            int activeReservations = await _unitOfWork.Reservations.GetOwnerActiveReservationsCountAsync(ownerId);

            var parkingIds = ownerParkings.Select(p => p.ParkingId).ToHashSet();
            var now = DateTime.UtcNow;
            int checkingInSoon = await _unitOfWork.Reservations.GetOwnerCheckingInSoonCountAsync(
                parkingIds, now, now.AddHours(2));

            var todayUtc = now.Date;
            decimal todayRevenue = await _unitOfWork.Reservations.GetOwnerTodayRevenueAsync(
                ownerId, todayUtc, todayUtc.AddDays(1));

            int revenuePct = RevenueTarget > 0
                ? (int)Math.Min(Math.Round(todayRevenue * 100m / RevenueTarget), 100)
                : 0;

            var dto = new DashboardSummaryDTO
            {
                OccupancyPercentage = occupancyPct,
                AverageRating = avgRating,
                TodayBookingsCount = todayBookings,
                TotalSpaces = totalSpaces,
                AvailableNow = availableNow,
                AvailablePercentage = availablePct,
                ActiveReservations = activeReservations,
                CheckingInSoon = checkingInSoon,
                TodayRevenue = todayRevenue,
                RevenueTarget = RevenueTarget,
                RevenueTargetPercentage = revenuePct
            };

            return ApiResponse<DashboardSummaryDTO>.Success("Dashboard summary retrieved successfully.", dto);
        }

        public async Task<ApiResponse<RevenueOverviewDTO>> GetRevenueOverviewAsync(Guid ownerId, string period)
        {
            var normalizedPeriod = period?.Trim().ToLowerInvariant() ?? "daily";
            if (normalizedPeriod != "daily" && normalizedPeriod != "weekly" && normalizedPeriod != "monthly")
            {
                return ApiResponse<RevenueOverviewDTO>.Failure("Period must be 'daily', 'weekly', or 'monthly'.");
            }

            var windowStart = normalizedPeriod == "monthly"
                ? DateTime.UtcNow.AddMonths(-12)
                : normalizedPeriod == "weekly"
                    ? DateTime.UtcNow.AddDays(-28)
                    : DateTime.UtcNow.AddDays(-7);

            var transactions = await _unitOfWork.Reservations.GetOwnerRevenueInWindowAsync(ownerId, windowStart);

            List<RevenueDataPointDTO> dataPoints;
            var now = DateTime.UtcNow;

            if (normalizedPeriod == "daily")
            {
                dataPoints = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = now.Date.AddDays(-(6 - i));
                        return new RevenueDataPointDTO
                        {
                            Label = day.ToString("ddd"),
                            Revenue = transactions
                                .Where(t => t.ArrivalTime.Date == day)
                                .Sum(t => t.TotalPrice)
                        };
                    })
                    .ToList();
            }
            else if (normalizedPeriod == "weekly")
            {
                dataPoints = Enumerable.Range(0, 4)
                    .Select(weeksAgo =>
                    {
                        var weekEnd = now.Date.AddDays(-(weeksAgo * 7));
                        var weekStart = weekEnd.AddDays(-6);
                        return new RevenueDataPointDTO
                        {
                            Label = $"Week {4 - weeksAgo}",
                            Revenue = transactions
                                .Where(t => t.ArrivalTime.Date >= weekStart && t.ArrivalTime.Date <= weekEnd)
                                .Sum(t => t.TotalPrice)
                        };
                    })
                    .Reverse()
                    .ToList();
            }
            else
            {
                dataPoints = Enumerable.Range(0, 12)
                    .Select(monthsAgo =>
                    {
                        var month = now.AddMonths(-monthsAgo);
                        var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var monthEnd = monthStart.AddMonths(1);
                        return new RevenueDataPointDTO
                        {
                            Label = month.ToString("MMM"),
                            Revenue = transactions
                                .Where(t => t.ArrivalTime >= monthStart && t.ArrivalTime < monthEnd)
                                .Sum(t => t.TotalPrice)
                        };
                    })
                    .Reverse()
                    .ToList();
            }

            return ApiResponse<RevenueOverviewDTO>.Success("Revenue overview retrieved successfully.", new RevenueOverviewDTO
            {
                Period = normalizedPeriod,
                TotalRevenue = dataPoints.Sum(dp => dp.Revenue),
                DataPoints = dataPoints
            });
        }

        public async Task<ApiResponse<List<LocationStatusDTO>>> GetLocationsStatusAsync(Guid ownerId)
        {
            var parkings = await _unitOfWork.Parkings.GetParkingsWithSpacesAsync();
            var ownerParkings = parkings.Where(p => p.OwnerId == ownerId).ToList();

            var result = new List<LocationStatusDTO>();
            foreach (var parking in ownerParkings)
            {
                var activeSpaces = parking.ParkingSpaces.Count(s => s.IsActive);
                var occupied = await _unitOfWork.Reservations.GetCheckedInCountForParkingAsync(parking.ParkingId);
                occupied = Math.Min(occupied, activeSpaces);
                var occupancyPct = activeSpaces > 0
                    ? (int)Math.Round(occupied * 100.0 / activeSpaces)
                    : 0;

                result.Add(new LocationStatusDTO
                {
                    ParkingId = parking.ParkingId,
                    Name = parking.Name,
                    Address = parking.Address,
                    IsActive = activeSpaces > 0,
                    OccupiedSpaces = occupied,
                    TotalSpaces = activeSpaces,
                    OccupancyPercentage = occupancyPct
                });
            }

            return ApiResponse<List<LocationStatusDTO>>.Success("Locations status retrieved successfully.", result);
        }

        public async Task<ApiResponse<List<OwnerReservationDTO>>> GetTodaysReservationsAsync(Guid ownerId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var reservations = await _unitOfWork.Reservations.GetOwnerTodaysReservationsAsync(ownerId, page, pageSize);

            var result = reservations.Select(r =>
            {
                var name = r.User?.UserName ?? "Unknown";
                var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                    : name.Length > 0 ? name[0].ToString().ToUpperInvariant() : "?";

                return new OwnerReservationDTO
                {
                    ReservationId = r.ReservationId,
                    BookingReference = $"PK-{r.ReservationId.ToString("N")[^4..].ToUpperInvariant()}",
                    CustomerName = name,
                    CustomerInitials = initials,
                    ParkingName = r.ParkingSpace?.Parking?.Name ?? string.Empty,
                    SpotNumber = r.ParkingSpace?.SpotNumber ?? string.Empty,
                    ArrivalTime = r.ArrivalTime,
                    DepartureTime = r.DepartureTime,
                    DurationHours = Math.Round((r.DepartureTime - r.ArrivalTime).TotalHours, 1),
                    Status = r.Status.ToString(),
                    TotalPrice = r.TotalPrice
                };
            }).ToList();

            return ApiResponse<List<OwnerReservationDTO>>.Success("Today's reservations retrieved successfully.", result);
        }

        public async Task<ApiResponse<List<ActivityFeedItemDTO>>> GetRecentActivityAsync(Guid ownerId, int limit)
        {
            limit = Math.Clamp(limit, 1, 50);
            var since = DateTime.UtcNow.AddDays(-7);

            var parkingIds = (await _unitOfWork.Parkings.GetParkingsWithSpacesAsync())
                .Where(p => p.OwnerId == ownerId)
                .Select(p => p.ParkingId)
                .ToHashSet();

            var accessLogs = await _unitOfWork.AccessLogs.GetRecentByParkingsAsync(parkingIds, since, limit * 2);
            var recentReservations = await _unitOfWork.Reservations.GetRecentByParkingsAsync(parkingIds, since, limit * 2);

            var now = DateTime.UtcNow;
            var feedItems = new List<ActivityFeedItemDTO>();

            foreach (var log in accessLogs)
            {
                var reservation = log.Reservation;
                var userName = reservation?.User?.UserName ?? "Unknown";
                var parkingName = reservation?.ParkingSpace?.Parking?.Name ?? "Unknown";
                var spotNumber = reservation?.ParkingSpace?.SpotNumber ?? "?";

                feedItems.Add(new ActivityFeedItemDTO
                {
                    Type = log.ScanType == ScanType.Entry ? "CheckIn" : "CheckOut",
                    Description = log.ScanType == ScanType.Entry
                        ? $"{userName} checked in to {parkingName} — {spotNumber}"
                        : $"{userName} checked out from {parkingName} — {spotNumber}",
                    Timestamp = log.ScanTimestamp,
                    TimeAgo = FormatTimeAgo(log.ScanTimestamp, now)
                });
            }

            foreach (var reservation in recentReservations)
            {
                var userName = reservation.User?.UserName ?? "Unknown";
                var bookingRef = $"PK-{reservation.ReservationId.ToString("N")[^4..].ToUpperInvariant()}";

                if (reservation.Status == ReservationStatus.Cancelled)
                {
                    feedItems.Add(new ActivityFeedItemDTO
                    {
                        Type = "Cancellation",
                        Description = $"{userName} cancelled booking {bookingRef} — refund issued",
                        Timestamp = reservation.ArrivalTime,
                        TimeAgo = FormatTimeAgo(reservation.ArrivalTime, now)
                    });
                }
                else if (reservation.Status == ReservationStatus.Confirmed || reservation.Status == ReservationStatus.CheckedIn)
                {
                    feedItems.Add(new ActivityFeedItemDTO
                    {
                        Type = "NewBooking",
                        Description = $"New booking confirmed — {userName} · {bookingRef}",
                        Timestamp = reservation.ArrivalTime,
                        TimeAgo = FormatTimeAgo(reservation.ArrivalTime, now)
                    });
                }
            }

            var sortedFeed = feedItems
                .OrderByDescending(f => f.Timestamp)
                .Take(limit)
                .ToList();

            return ApiResponse<List<ActivityFeedItemDTO>>.Success("Recent activity retrieved successfully.", sortedFeed);
        }

        private static string FormatTimeAgo(DateTime timestamp, DateTime now)
        {
            var elapsed = now - timestamp;
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} min ago";
            if (elapsed.TotalHours < 24)
            {
                var h = (int)elapsed.TotalHours;
                var m = elapsed.Minutes;
                return m > 0 ? $"{h}h {m}m ago" : $"{h}h ago";
            }
            var d = (int)elapsed.TotalDays;
            return d == 1 ? "1 day ago" : $"{d} days ago";
        }
    }
}
