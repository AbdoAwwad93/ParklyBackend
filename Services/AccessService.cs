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

        public AccessService(IUnitOfWork unitOfWork, IOccupancyService occupancyService, IOptions<JwtOptions>? jwtOptions, ILogger<AccessService> logger)
        {
            _unitOfWork = unitOfWork;
            _occupancyService = occupancyService;
            _logger = logger;
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
                }
                else if (dto.ScanType == ScanType.Exit)
                {
                    if (reservation.Status != ReservationStatus.CheckedIn)
                    {
                        _logger.LogWarning("Failed exit scan. Reservation {ReservationId} status is {Status}", reservation.ReservationId, reservation.Status);
                        return ApiResponse.Failure($"Cannot process Exit. Current status: {reservation.Status}");
                    }
                    reservation.Status = ReservationStatus.Completed;
                }

                var accessLog = new AccessLog
                {
                    ReservationId = reservation.ReservationId,
                    ScanType = dto.ScanType,
                    ScanTimestamp = DateTime.UtcNow
                };

                await _unitOfWork.AccessLogs.AddAsync(accessLog);
                await _unitOfWork.SaveChangesAsync();
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
    }
}
