using AutoMapper;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Parkly_Backend.Configuration;

namespace Parkly_Backend.Services
{
    public class ReservationsService : IReservationsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPricingService _pricingService;
        private readonly IAvailabilityService _availabilityService;
        private readonly IMapper _mapper;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<ReservationsService> _logger;

        public ReservationsService(IUnitOfWork unitOfWork, IPricingService pricingService, IAvailabilityService availabilityService, IMapper mapper, IOptions<JwtOptions> jwtOptions, ILogger<ReservationsService> logger)
        {
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
            _availabilityService = availabilityService;
            _mapper = mapper;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public async Task<ApiResponse<ReservationResponseDTO>> CreateAsync(Guid userId, CreateReservationDTO dto)
        {
            if (dto.ArrivalTime >= dto.DepartureTime)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Departure time must be after arrival time.");
            }

            var space = await _unitOfWork.ParkingSpaces.GetByIdAsync(dto.SpaceId);
            if (space == null)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Parking space not found.");
            }
            if (!space.IsActive)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Parking space is not active.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!await _availabilityService.IsSpaceAvailableAsync(dto.SpaceId, dto.ArrivalTime, dto.DepartureTime))
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return ApiResponse<ReservationResponseDTO>.Failure("The parking space is unavailable for the requested time window.");
                }

                var totalPrice = await _pricingService.CalculateTotalPriceAsync(dto.SpaceId, dto.ArrivalTime, dto.DepartureTime);

                var reservation = new Reservation
                {
                    UserId = userId,
                    SpaceId = dto.SpaceId,
                    ArrivalTime = dto.ArrivalTime,
                    DepartureTime = dto.DepartureTime,
                    TotalPrice = totalPrice,
                    Status = ReservationStatus.Confirmed
                };

                await _unitOfWork.Reservations.AddAsync(reservation);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Reservation {ReservationId} created successfully for User {UserId} at Space {SpaceId}", reservation.ReservationId, userId, dto.SpaceId);

                var response = await BuildResponseAsync(reservation.ReservationId);
                return ApiResponse<ReservationResponseDTO>.Success("Reservation created successfully.", response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating reservation for User {UserId} at Space {SpaceId}", userId, dto.SpaceId);
                return ApiResponse<ReservationResponseDTO>.Failure("An error occurred while creating the reservation.");
            }
        }

        public async Task<ApiResponse<ReservationResponseDTO>> UpdateAsync(Guid userId, Guid reservationId, UpdateReservationDTO dto)
        {
            if (dto.ArrivalTime >= dto.DepartureTime)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Departure time must be after arrival time.");
            }

            var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
            if (reservation == null)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Reservation not found.");
            }
            if (reservation.UserId != userId)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("You do not have permission to modify this reservation.");
            }
            if (reservation.Status != ReservationStatus.Confirmed)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Only confirmed reservations can be modified.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!await _availabilityService.IsSpaceAvailableAsync(reservation.SpaceId, dto.ArrivalTime, dto.DepartureTime, reservationId))
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return ApiResponse<ReservationResponseDTO>.Failure("The parking space is unavailable for the requested time window.");
                }

                reservation.ArrivalTime = dto.ArrivalTime;
                reservation.DepartureTime = dto.DepartureTime;
                reservation.TotalPrice = await _pricingService.CalculateTotalPriceAsync(reservation.SpaceId, dto.ArrivalTime, dto.DepartureTime);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Reservation {ReservationId} updated successfully by User {UserId}", reservationId, userId);

                var response = await BuildResponseAsync(reservationId);
                return ApiResponse<ReservationResponseDTO>.Success("Reservation updated successfully.", response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating Reservation {ReservationId} for User {UserId}", reservationId, userId);
                return ApiResponse<ReservationResponseDTO>.Failure("An error occurred while updating the reservation.");
            }
        }

        public async Task<ApiResponse> CancelAsync(Guid userId, Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
            if (reservation == null)
            {
                return ApiResponse.Failure("Reservation not found.");
            }
            if (reservation.UserId != userId)
            {
                return ApiResponse.Failure("You do not have permission to cancel this reservation.");
            }
            if (reservation.Status != ReservationStatus.Confirmed)
            {
                return ApiResponse.Failure("Only confirmed reservations can be cancelled.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                reservation.Status = ReservationStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Reservation {ReservationId} cancelled successfully by User {UserId}", reservationId, userId);

                return ApiResponse.Success("Reservation cancelled successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error cancelling Reservation {ReservationId} for User {UserId}", reservationId, userId);
                return ApiResponse.Failure("An error occurred while cancelling the reservation.");
            }
        }

        public async Task<ApiResponse<string>> GetQrCodeAsync(Guid userId, Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
            if (reservation == null)
            {
                return ApiResponse<string>.Failure("Reservation not found.");
            }
            if (reservation.UserId != userId)
            {
                return ApiResponse<string>.Failure("You do not have permission to view this reservation.");
            }
            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ApiResponse<string>.Failure("Cannot generate QR code for a cancelled reservation.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _jwtOptions.SecretKey;
            var key = Encoding.UTF8.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                { 
                    new Claim("ReservationId", reservationId.ToString())
                }),
                Expires = reservation.DepartureTime.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return ApiResponse<string>.Success("QR code generated successfully.", jwt);
        }

        private async Task<ReservationResponseDTO> BuildResponseAsync(Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(reservationId);
            
            return _mapper.Map<ReservationResponseDTO>(reservation);
        }

        public async Task<ApiResponse<List<ReservationResponseDTO>>> GetUserReservationsAsync(Guid userId)
        {
            var reservations = await _unitOfWork.Reservations.GetAllReservationsByUserAsync(userId);

            var response = _mapper.Map<List<ReservationResponseDTO>>(reservations);
            return ApiResponse<List<ReservationResponseDTO>>.Success("Reservations retrieved successfully.", response);
        }

        public async Task<ApiResponse<List<ReservationResponseDTO>>> GetActiveUserReservationsAsync(Guid userId)
        {
            var reservations = await _unitOfWork.Reservations.GetActiveReservationsByUserAsync(userId);

            var response = _mapper.Map<List<ReservationResponseDTO>>(reservations);
            return ApiResponse<List<ReservationResponseDTO>>.Success("Active reservations retrieved successfully.", response);
        }
    }
}