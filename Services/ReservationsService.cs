using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Parkly_Backend.Services
{
    public class ReservationsService : IReservationsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPricingService _pricingService;
        private readonly IAvailabilityService _availabilityService;
        private readonly IMapper _mapper;

        public ReservationsService(IUnitOfWork unitOfWork, IPricingService pricingService, IAvailabilityService availabilityService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
            _availabilityService = availabilityService;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ReservationResponseDTO>> CreateAsync(Guid userId, CreateReservationDTO dto)
        {
            if (dto.ArrivalTime >= dto.DepartureTime)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Departure time must be after arrival time.");
            }

            var space = await _unitOfWork.Repository<ParkingSpace>().GetByIdAsync(dto.SpaceId);
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

                await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var response = await BuildResponseAsync(reservation.ReservationId);
                return ApiResponse<ReservationResponseDTO>.Success("Reservation created successfully.", response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<ReservationResponseDTO>.Failure("An error occurred while creating the reservation.");
            }
        }

        public async Task<ApiResponse<ReservationResponseDTO>> UpdateAsync(Guid userId, Guid reservationId, UpdateReservationDTO dto)
        {
            if (dto.ArrivalTime >= dto.DepartureTime)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Departure time must be after arrival time.");
            }

            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(reservationId);
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

                var response = await BuildResponseAsync(reservationId);
                return ApiResponse<ReservationResponseDTO>.Success("Reservation updated successfully.", response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<ReservationResponseDTO>.Failure("An error occurred while updating the reservation.");
            }
        }

        public async Task<ApiResponse> CancelAsync(Guid userId, Guid reservationId)
        {
            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(reservationId);
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
                return ApiResponse.Success("Reservation cancelled successfully.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse.Failure("An error occurred while cancelling the reservation.");
            }
        }

        public async Task<ApiResponse<string>> GetQrCodeAsync(Guid userId, Guid reservationId)
        {
            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(reservationId);
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
            var keyString = Environment.GetEnvironmentVariable("SecretKey");
            var key = Encoding.UTF8.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                { 
                    new Claim("ReservationId", reservationId.ToString())
                }),
                Expires = reservation.DepartureTime.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = Environment.GetEnvironmentVariable("Issuer"),
                Audience = Environment.GetEnvironmentVariable("Audience")
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return ApiResponse<string>.Success("QR code generated successfully.", jwt);
        }

        private async Task<ReservationResponseDTO> BuildResponseAsync(Guid reservationId)
        {
            var reservation = await _unitOfWork.Repository<Reservation>().Query()
                .Include(r => r.ParkingSpace)
                .FirstAsync(r => r.ReservationId == reservationId);

            return _mapper.Map<ReservationResponseDTO>(reservation);
        }
    }
}