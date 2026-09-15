using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parkly_Backend.Configuration;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class AccessService : IAccessService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOccupancyService _occupancyService;
        private readonly ILogger<AccessService> _logger;
        private readonly IParkingSpacesService _spacesService;
        private readonly INotificationService _notificationService;

        public AccessService(IUnitOfWork unitOfWork, IOccupancyService occupancyService, IOptions<JwtOptions>? jwtOptions, ILogger<AccessService> logger,IParkingSpacesService spacesService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _occupancyService = occupancyService;
            _logger = logger;
            _spacesService =spacesService;
            _notificationService = notificationService;

        }

        public async Task<ApiResponse> ProcessScanAsync(AccessScanDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.QrToken))
            {
                return ApiResponse.Failure("QR code cannot be empty.");
            }

            var code = dto.QrToken.Trim();

            try
            {
                var reservation = await _unitOfWork.Reservations.GetByQrCodeWithIncludesAsync(code);

                if (reservation == null)
                {
                    return ApiResponse.Failure("Invalid QR code or reservation not found.");
                }

                await _unitOfWork.BeginTransactionAsync();

                if (dto.ScanType == ScanType.Entry)
                {
                    if (reservation.Status != ReservationStatus.Confirmed)
                    {
                        _logger.LogWarning("Failed entry scan. Reservation {ReservationId} status is {Status}", reservation.ReservationId, reservation.Status);
                        return ApiResponse.Failure($"Cannot process Entry. Current status: {reservation.Status}");
                    }
                    reservation.Status = ReservationStatus.CheckedIn;
                    var entrySpace = await _unitOfWork.ParkingSpaces.GetByIdAsync(reservation.SpaceId);
                    if (entrySpace != null)
                    {
                        entrySpace.Status = SpaceStatus.Occupied;
                    }
                }
                else if (dto.ScanType == ScanType.Exit)
                {
                    if (reservation.Status != ReservationStatus.CheckedIn)
                    {
                        _logger.LogWarning("Failed exit scan. Reservation {ReservationId} status is {Status}", reservation.ReservationId, reservation.Status);
                        return ApiResponse.Failure($"Cannot process Exit. Current status: {reservation.Status}");
                    }
                    reservation.Status = ReservationStatus.Completed;
                    await _unitOfWork.SaveChangesAsync();
                    await _spacesService.RefreshSpaceStatusAsync(reservation.SpaceId);
                }

                var accessLog = new AccessLog
                {
                    ReservationId = reservation.ReservationId,
                    ScanType = dto.ScanType,
                    ScanTimestamp = DateTime.UtcNow
                };

                await _unitOfWork.AccessLogs.AddAsync(accessLog);
                await _unitOfWork.SaveChangesAsync();
                await CreateAccessNotificationAsync(reservation, dto.ScanType, accessLog.ScanTimestamp);
                await _unitOfWork.CommitTransactionAsync();

                await _occupancyService.BroadcastOccupancyUpdateAsync(reservation.ParkingSpace.ParkingId);

                _logger.LogInformation("Successfully processed {ScanType} for Reservation {ReservationId}", dto.ScanType, reservation.ReservationId);

                return ApiResponse.Success($"{dto.ScanType} processed successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing {ScanType} scan for QR code", dto.ScanType);
                return ApiResponse.Failure("An error occurred while processing the scan.");
            }
        }

        public async Task<ApiResponse<CheckInResponseDTO>> CheckInAsync(string qrToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return ApiResponse<CheckInResponseDTO>.Failure("QR code cannot be empty.");
            }

            var code = qrToken.Trim();

            try
            {
                var reservation = await _unitOfWork.Reservations.GetByQrCodeWithIncludesAsync(code);

                if (reservation == null)
                {
                    return ApiResponse<CheckInResponseDTO>.Failure("Invalid QR code or reservation not found.");
                }

                if (reservation.Status != ReservationStatus.Confirmed)
                {
                    _logger.LogWarning("Failed check-in. Reservation {ReservationId} status is {Status}", reservation.ReservationId, reservation.Status);
                    return ApiResponse<CheckInResponseDTO>.Failure($"Cannot process Check-In. Current status: {reservation.Status}");
                }

                await _unitOfWork.BeginTransactionAsync();

                reservation.Status = ReservationStatus.CheckedIn;
                var checkInSpace = await _unitOfWork.ParkingSpaces.GetByIdAsync(reservation.SpaceId);
                if (checkInSpace != null)
                {
                    checkInSpace.Status = SpaceStatus.Occupied;
                }

                var entryTimestamp = DateTime.UtcNow;

                var accessLog = new AccessLog
                {
                    ReservationId = reservation.ReservationId,
                    ScanType = ScanType.Entry,
                    ScanTimestamp = entryTimestamp
                };

                await _unitOfWork.AccessLogs.AddAsync(accessLog);
                await _unitOfWork.SaveChangesAsync();
                await CreateAccessNotificationAsync(reservation, ScanType.Entry, entryTimestamp);
                await _unitOfWork.CommitTransactionAsync();

                await _occupancyService.BroadcastOccupancyUpdateAsync(reservation.ParkingSpace.ParkingId);
                _logger.LogInformation("Successfully checked in Reservation {ReservationId}", reservation.ReservationId);

                var response = BuildCheckInResponse(reservation, entryTimestamp);
                return ApiResponse<CheckInResponseDTO>.Success("Checked in successfully.", response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing check-in for QR code");
                return ApiResponse<CheckInResponseDTO>.Failure("An error occurred while processing check-in.");
            }
        }

        public async Task<ApiResponse<CheckOutResponseDTO>> CheckOutAsync(string qrToken)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return ApiResponse<CheckOutResponseDTO>.Failure("QR code cannot be empty.");
            }

            var code = qrToken.Trim();

            try
            {
                var reservation = await _unitOfWork.Reservations.GetByQrCodeWithIncludesAsync(code);

                if (reservation == null)
                {
                    return ApiResponse<CheckOutResponseDTO>.Failure("Invalid QR code or reservation not found.");
                }

                if (reservation.Status != ReservationStatus.CheckedIn)
                {
                    _logger.LogWarning("Failed check-out. Reservation {ReservationId} status is {Status}", reservation.ReservationId, reservation.Status);
                    return ApiResponse<CheckOutResponseDTO>.Failure($"Cannot process Check-Out. Current status: {reservation.Status}");
                }

                await _unitOfWork.BeginTransactionAsync();

                reservation.Status = ReservationStatus.Completed;
                await _unitOfWork.SaveChangesAsync();
                await _spacesService.RefreshSpaceStatusAsync(reservation.SpaceId);

                var exitTimestamp = DateTime.UtcNow;

                var accessLog = new AccessLog
                {
                    ReservationId = reservation.ReservationId,
                    ScanType = ScanType.Exit,
                    ScanTimestamp = exitTimestamp
                };

                await _unitOfWork.AccessLogs.AddAsync(accessLog);
                await _unitOfWork.SaveChangesAsync();
                await CreateAccessNotificationAsync(reservation, ScanType.Exit, exitTimestamp);
                await _unitOfWork.CommitTransactionAsync();

                await _occupancyService.BroadcastOccupancyUpdateAsync(reservation.ParkingSpace.ParkingId);
                _logger.LogInformation("Successfully checked out Reservation {ReservationId}", reservation.ReservationId);

                var response = BuildCheckOutResponse(reservation, exitTimestamp);
                return ApiResponse<CheckOutResponseDTO>.Success("Checked out successfully.", response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing check-out for QR code");
                return ApiResponse<CheckOutResponseDTO>.Failure("An error occurred while processing check-out.");
            }
        }

        private static CheckInResponseDTO BuildCheckInResponse(Reservation reservation, DateTime checkInTime)        {
            var durationHours = Math.Round((reservation.DepartureTime - reservation.ArrivalTime).TotalHours, 1);

            return new CheckInResponseDTO
            {
                ReservationId = reservation.ReservationId,
                ParkingId = reservation.ParkingSpace.ParkingId,
                ParkingName = reservation.ParkingSpace.Parking.Name,
                ParkingAddress = reservation.ParkingSpace.Parking.Address,
                SpaceId = reservation.SpaceId,
                SpotNumber = reservation.ParkingSpace.SpotNumber,
                Status = "CheckedIn",
                CheckInTime = checkInTime,
                ScheduledArrivalTime = reservation.ArrivalTime,
                ScheduledDepartureTime = reservation.DepartureTime,
                TotalHours = durationHours,
                TotalPrice = reservation.TotalPrice,
                QrCode = reservation.QrCode
            };
        }

        private async Task CreateAccessNotificationAsync(Reservation reservation, ScanType scanType, DateTime timestamp)
        {
            var parking = reservation.ParkingSpace?.Parking;
            if (parking == null)
            {
                return;
            }

            var customerName = string.IsNullOrWhiteSpace(reservation.User?.FullName) ? "A customer" : reservation.User.FullName;
            var action = scanType == ScanType.Entry ? "checked in to" : "checked out of";
            var title = scanType == ScanType.Entry
                ? $"Check-in — {parking.Name}"
                : $"Check-out — {parking.Name}";
            var message = $"{customerName} {action} {reservation.ParkingSpace!.SpotNumber}, {parking.Name} at {timestamp:t}.";

            await _notificationService.CreateAsync(parking.OwnerId, NotificationType.Update, title, message,
                parking.ParkingId, reservation.ReservationId, reservation.SpaceId);
        }

        private static CheckOutResponseDTO BuildCheckOutResponse(Reservation reservation, DateTime exitTime)
        {
            var entryLog = reservation.AccessLogs
                .Where(l => l.ScanType == ScanType.Entry)
                .OrderByDescending(l => l.ScanTimestamp)
                .FirstOrDefault();

            var checkInTime = entryLog?.ScanTimestamp ?? reservation.ArrivalTime;
            var duration = exitTime - checkInTime;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            var hourlyRate = reservation.ParkingSpace.BaseHourlyRate;
            var durationCost = Math.Round((decimal)duration.TotalHours * hourlyRate, 2);
            var serviceFee = 0.75m;
            var totalAmount = durationCost + serviceFee;

            return new CheckOutResponseDTO
            {
                ReservationId = reservation.ReservationId,
                ParkingId = reservation.ParkingSpace.ParkingId,
                ParkingName = reservation.ParkingSpace.Parking.Name,
                ParkingAddress = reservation.ParkingSpace.Parking.Address,
                SpotNumber = reservation.ParkingSpace.SpotNumber,
                Status = "Completed",
                DateFormatted = exitTime.ToString("ddd, MMM dd, yyyy"),
                CheckInTime = checkInTime,
                CheckOutTime = exitTime,
                DurationFormatted = FormatDuration(duration),
                TotalMinutes = Math.Round(duration.TotalMinutes, 1),
                HourlyRate = hourlyRate,
                DurationCost = durationCost,
                ServiceFee = serviceFee,
                TotalAmount = totalAmount,
                UserEmail = reservation.User?.Email ?? string.Empty
            };
        }

        private static string FormatDuration(TimeSpan duration)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            if (hours > 0)
            {
                return $"{hours} hr{(hours > 1 ? "s" : "")} {minutes} min";
            }
            return $"{minutes} min";
        }
    }
}
