using AutoMapper;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parkly_Backend.Configuration;

namespace Parkly_Backend.Services
{
    public class ReservationsService : IReservationsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPricingService _pricingService;
        private readonly IAvailabilityService _availabilityService;
        private readonly IMapper _mapper;
        private readonly ILogger<ReservationsService> _logger;
        private readonly IParkingSpacesService _spacesService;
        private readonly INotificationService _notificationService;

        public ReservationsService(IUnitOfWork unitOfWork,IParkingSpacesService spacesService, IPricingService pricingService, IAvailabilityService availabilityService, IMapper mapper, ILogger<ReservationsService> logger, INotificationService notificationService, IOptions<JwtOptions>? jwtOptions = null)
        {
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
            _availabilityService = availabilityService;
            _mapper = mapper;
            _logger = logger;
            _spacesService = spacesService;
            _notificationService = notificationService;
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
                var qrCode = await GenerateUniqueQrCodeAsync();

                var reservation = new Reservation
                {
                    UserId = userId,
                    SpaceId = dto.SpaceId,
                    ArrivalTime = dto.ArrivalTime,
                    DepartureTime = dto.DepartureTime,
                    TotalPrice = totalPrice,
                    Status = ReservationStatus.Confirmed,
                    QrCode = qrCode
                };

                await _unitOfWork.Reservations.AddAsync(reservation);
                await _unitOfWork.SaveChangesAsync();
                var now = DateTime.UtcNow;
                if (reservation.ArrivalTime <= now && reservation.DepartureTime > now
                    && space.Status == SpaceStatus.Available)
                {
                    space.Status = SpaceStatus.Reserved;
                    await _unitOfWork.SaveChangesAsync();
                }

                await CreateReservationNotificationAsync(reservation.ReservationId, NotificationType.Booking);

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

                await _spacesService.RefreshSpaceStatusAsync(reservation.SpaceId);

                await CreateReservationNotificationAsync(reservation.ReservationId, NotificationType.Cancellation);

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

            if (string.IsNullOrEmpty(reservation.QrCode))
            {
                reservation.QrCode = await GenerateUniqueQrCodeAsync();
                _unitOfWork.Reservations.Update(reservation);
                await _unitOfWork.SaveChangesAsync();
            }

            return ApiResponse<string>.Success("QR code generated successfully.", reservation.QrCode);
        }

        private async Task<string> GenerateUniqueQrCodeAsync()
        {
            string code;
            do
            {
                code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            } while (await _unitOfWork.Reservations.IsQrCodeInUseAsync(code));

            return code;
        }

        private async Task<ReservationResponseDTO> BuildResponseAsync(Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(reservationId);
            
            return _mapper.Map<ReservationResponseDTO>(reservation);
        }

        private async Task CreateReservationNotificationAsync(Guid reservationId, NotificationType type)
        {
            var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(reservationId);
            if (reservation?.ParkingSpace?.Parking == null)
            {
                return;
            }

            var customerName = string.IsNullOrWhiteSpace(reservation.User?.FullName) ? "A customer" : reservation.User.FullName;
            var parking = reservation.ParkingSpace.Parking;
            var bookingReference = $"PK-{reservation.ReservationId.ToString("N")[^4..].ToUpperInvariant()}";
            var title = type == NotificationType.Booking
                ? $"New Booking — {parking.Name}"
                : $"Cancellation — Booking {bookingReference}";
            var message = type == NotificationType.Booking
                ? $"{customerName} reserved {reservation.ParkingSpace.SpotNumber} for {reservation.ArrivalTime:g}–{reservation.DepartureTime:t}. Payment ${reservation.TotalPrice:F2} received."
                : $"{customerName} cancelled their reservation for {reservation.ParkingSpace.SpotNumber}."
                    + $" Booking {bookingReference} is now cancelled.";

            await _notificationService.CreateAsync(parking.OwnerId, type, title, message,
                parking.ParkingId, reservation.ReservationId, reservation.SpaceId);
        }

        public async Task<ApiResponse<List<ReservationResponseDTO>>> GetUserReservationsAsync(Guid userId)
        {
            var reservations = await _unitOfWork.Reservations.GetAllReservationsByUserAsync(userId);

            var response = _mapper.Map<List<ReservationResponseDTO>>(reservations);
            return ApiResponse<List<ReservationResponseDTO>>.Success("Reservations retrieved successfully.", response);
        }

        public async Task<ApiResponse<ReservationResponseDTO>> GetByIdAsync(Guid userId, Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(reservationId);
            if (reservation == null)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("Reservation not found.");
            }

            if (reservation.UserId != userId)
            {
                return ApiResponse<ReservationResponseDTO>.Failure("You do not have permission to view this reservation.");
            }

            var response = _mapper.Map<ReservationResponseDTO>(reservation);
            return ApiResponse<ReservationResponseDTO>.Success("Reservation retrieved successfully.", response);
        }

        public async Task<ApiResponse<CheckOutResponseDTO>> GetCheckoutPreviewAsync(Guid userId, Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(reservationId);
            if (reservation == null)
            {
                return ApiResponse<CheckOutResponseDTO>.Failure("Reservation not found.");
            }

            if (reservation.UserId != userId)
            {
                return ApiResponse<CheckOutResponseDTO>.Failure("You do not have permission to view this reservation.");
            }

            if (reservation.Status != ReservationStatus.CheckedIn)
            {
                return ApiResponse<CheckOutResponseDTO>.Failure("Only active checked-in reservations can be previewed for checkout.");
            }

            var entryLog = reservation.AccessLogs
                .Where(l => l.ScanType == ScanType.Entry)
                .OrderByDescending(l => l.ScanTimestamp)
                .FirstOrDefault();

            var checkInTime = entryLog?.ScanTimestamp ?? reservation.ArrivalTime;
            var checkOutTime = DateTime.UtcNow;
            var duration = checkOutTime - checkInTime;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            var hourlyRate = reservation.ParkingSpace.BaseHourlyRate;
            var durationCost = Math.Round((decimal)duration.TotalHours * hourlyRate, 2);
            var serviceFee = 0.75m;
            var totalAmount = durationCost + serviceFee;

            var preview = new CheckOutResponseDTO
            {
                ReservationId = reservation.ReservationId,
                ParkingId = reservation.ParkingSpace.ParkingId,
                ParkingName = reservation.ParkingSpace.Parking.Name,
                ParkingAddress = reservation.ParkingSpace.Parking.Address,
                SpotNumber = reservation.ParkingSpace.SpotNumber,
                Status = "Completed",
                DateFormatted = checkOutTime.ToString("ddd, MMM dd, yyyy"),
                CheckInTime = checkInTime,
                CheckOutTime = checkOutTime,
                DurationFormatted = FormatDuration(duration),
                TotalMinutes = Math.Round(duration.TotalMinutes, 1),
                HourlyRate = hourlyRate,
                DurationCost = durationCost,
                ServiceFee = serviceFee,
                TotalAmount = totalAmount,
                UserEmail = reservation.User?.Email ?? string.Empty
            };

            return ApiResponse<CheckOutResponseDTO>.Success("Checkout preview generated successfully.", preview);
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

        public async Task<ApiResponse<List<ReservationResponseDTO>>> GetActiveUserReservationsAsync(Guid userId)
        {
            var reservations = await _unitOfWork.Reservations.GetActiveReservationsByUserAsync(userId);

            var response = _mapper.Map<List<ReservationResponseDTO>>(reservations);
            return ApiResponse<List<ReservationResponseDTO>>.Success("Active reservations retrieved successfully.", response);
        }
    }
}
