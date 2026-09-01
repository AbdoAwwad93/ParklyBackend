using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class ReviewsService : IReviewsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReviewsService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ReviewResponseDTO>> CreateAsync(Guid userId, CreateReviewDTO dto)
        {
            var reservation = await _unitOfWork.Reservations.GetReservationWithIncludesAsync(dto.ReservationId);

            if (reservation == null)
            {
                return ApiResponse<ReviewResponseDTO>.Failure("Reservation not found.");
            }

            if (reservation.UserId != userId)
            {
                return ApiResponse<ReviewResponseDTO>.Failure("You do not have permission to review this reservation.");
            }

            if (reservation.Status != ReservationStatus.Completed)
            {
                return ApiResponse<ReviewResponseDTO>.Failure("Reviews can only be submitted after checkout / completion of the reservation.");
            }

            if (reservation.Review != null)
            {
                return ApiResponse<ReviewResponseDTO>.Failure("This reservation has already been reviewed.");
            }

            var review = new Review
            {
                ReservationId = dto.ReservationId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();
            await UpdateParkingRatingStatsAsync(reservation.ParkingSpace.ParkingId);

            var response = _mapper.Map<ReviewResponseDTO>(review);
            
            // Map manual fields not covered by basic AutoMapper (or handled gracefully)
            response.UserName = reservation.User.FullName;
            response.ParkingName = reservation.ParkingSpace.Parking.Name;

            return ApiResponse<ReviewResponseDTO>.Success("Review submitted successfully.", response);
        }

        public async Task<ApiResponse<ReviewResponseDTO>> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewDTO dto)
        {
            var review = await _unitOfWork.Reviews.GetReviewWithIncludesAsync(reviewId);

            if (review == null)
            {
                return ApiResponse<ReviewResponseDTO>.Failure("Review not found.");
            }

            if (review.Reservation.UserId != userId)
            {
                return ApiResponse<ReviewResponseDTO>.Failure("You do not have permission to update this review.");
            }

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            await _unitOfWork.SaveChangesAsync();
            await UpdateParkingRatingStatsAsync(review.Reservation.ParkingSpace.ParkingId);

            var response = _mapper.Map<ReviewResponseDTO>(review);
            response.UserName = review.Reservation.User.FullName;
            response.ParkingName = review.Reservation.ParkingSpace.Parking.Name;

            return ApiResponse<ReviewResponseDTO>.Success("Review updated successfully.", response);
        }

        public async Task<ApiResponse<ParkingReviewsSummaryDTO>> GetParkingReviewsAsync(Guid parkingId, int page = 1, int pageSize = 20)
        {
            var parking = await _unitOfWork.Parkings.GetByIdAsync(parkingId);
            if (parking == null)
            {
                return ApiResponse<ParkingReviewsSummaryDTO>.Failure("Parking facility not found.");
            }

            int totalReviews = parking.TotalReviews;
            double averageRating = parking.AverageRating;

            page = page > 0 ? page : 1;
            pageSize = pageSize > 0 ? pageSize : 20;
            var skip = (page - 1) * pageSize;

            var reviews = await _unitOfWork.Reviews.GetReviewsForParkingAsync(parkingId, skip, pageSize);

            var summary = new ParkingReviewsSummaryDTO
            {
                ParkingId = parkingId,
                AverageRating = averageRating,
                TotalReviews = totalReviews,
                Reviews = reviews.Select(r => 
                {
                    var dto = _mapper.Map<ReviewResponseDTO>(r);
                    dto.UserName = r.Reservation.User.FullName;
                    dto.ParkingName = r.Reservation.ParkingSpace.Parking.Name;
                    return dto;
                }).ToList()
            };

            return ApiResponse<ParkingReviewsSummaryDTO>.Success("Parking reviews retrieved successfully.", summary);
        }

        public async Task<ApiResponse<List<ReviewResponseDTO>>> GetUserReviewsAsync(Guid userId)
        {
            var reviews = await _unitOfWork.Reviews.GetUserReviewsAsync(userId);

            var response = reviews.Select(r => 
            {
                var dto = _mapper.Map<ReviewResponseDTO>(r);
                dto.UserName = r.Reservation.User.FullName;
                dto.ParkingName = r.Reservation.ParkingSpace.Parking.Name;
                return dto;
            }).ToList();

            return ApiResponse<List<ReviewResponseDTO>>.Success("User reviews retrieved successfully.", response);
        }

        public async Task<ApiResponse> DeleteAsync(Guid userId, Guid reviewId)
        {
            var review = await _unitOfWork.Reviews.GetReviewWithIncludesAsync(reviewId);

            if (review == null)
            {
                return ApiResponse.Failure("Review not found.");
            }

            if (review.Reservation.UserId != userId)
            {
                return ApiResponse.Failure("You do not have permission to delete this review.");
            }

            _unitOfWork.Reviews.Delete(review);
            await _unitOfWork.SaveChangesAsync();
            await UpdateParkingRatingStatsAsync(review.Reservation.ParkingSpace.ParkingId);

            return ApiResponse.Success("Review deleted successfully.");
        }

        private async Task UpdateParkingRatingStatsAsync(Guid parkingId)
        {
            var parking = await _unitOfWork.Parkings.GetByIdAsync(parkingId);
            if (parking != null)
            {
                parking.TotalReviews = await _unitOfWork.Reviews.GetTotalReviewsForParkingAsync(parkingId);
                parking.AverageRating = Math.Round(await _unitOfWork.Reviews.GetAverageRatingForParkingAsync(parkingId), 1);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
