using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AccessService> _logger;

        public AccessService(IUnitOfWork unitOfWork, IOccupancyService occupancyService, IOptions<JwtOptions> jwtOptions, ILogger<AccessService> logger)
        {
            _unitOfWork = unitOfWork;
            _occupancyService = occupancyService;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public async Task<ApiResponse> ProcessScanAsync(AccessScanDTO dto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _jwtOptions.SecretKey;
            var key = Encoding.UTF8.GetBytes(keyString);

            try
            {
                tokenHandler.ValidateToken(dto.QrToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var reservationIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "ReservationId")?.Value;
                
                if (string.IsNullOrEmpty(reservationIdClaim) || !Guid.TryParse(reservationIdClaim, out Guid reservationId))
                {
                    return ApiResponse.Failure("Invalid QR code payload.");
                }

                var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(reservationId);

                if (reservation == null)
                {
                    return ApiResponse.Failure("Reservation not found.");
                }

                await _unitOfWork.BeginTransactionAsync();

                if (dto.ScanType == ScanType.Entry)
                {
                    if (reservation.Status != ReservationStatus.Confirmed)
                    {
                        _logger.LogWarning("Failed entry scan. Reservation {ReservationId} status is {Status}", reservationId, reservation.Status);
                        return ApiResponse.Failure($"Cannot process Entry. Current status: {reservation.Status}");
                    }
                    reservation.Status = ReservationStatus.CheckedIn;
                }
                else if (dto.ScanType == ScanType.Exit)
                {
                    if (reservation.Status != ReservationStatus.CheckedIn)
                    {
                        _logger.LogWarning("Failed exit scan. Reservation {ReservationId} status is {Status}", reservationId, reservation.Status);
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
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "QR code has expired");
                return ApiResponse.Failure("QR code has expired.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid QR code presented");
                return ApiResponse.Failure("Invalid QR code.");
            }
        }
    }
}
