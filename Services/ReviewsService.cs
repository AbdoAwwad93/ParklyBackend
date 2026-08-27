using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
            var reservation = await _unitOfWork.Repository<Reservation>().Query()
                .Include(r => r.Review)
                .Include(r => r.User)
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
                .FirstOrDefaultAsync(r => r.ReservationId == dto.ReservationId);

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

            await _unitOfWork.Repository<Review>().AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<ReviewResponseDTO>(review);
            
            // Map manual fields not covered by basic AutoMapper (or handled gracefully)
            response.UserName = reservation.User.FullName;
            response.ParkingName = reservation.ParkingSpace.Parking.Name;

            return ApiResponse<ReviewResponseDTO>.Success("Review submitted successfully.", response);
        }

        public async Task<ApiResponse<ReviewResponseDTO>> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewDTO dto)
        {
            var review = await _unitOfWork.Repository<Review>().Query()
                .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
                .Include(r => r.Reservation.ParkingSpace.Parking)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

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

            var response = _mapper.Map<ReviewResponseDTO>(review);
            response.UserName = review.Reservation.User.FullName;
            response.ParkingName = review.Reservation.ParkingSpace.Parking.Name;

            return ApiResponse<ReviewResponseDTO>.Success("Review updated successfully.", response);
        }

        public async Task<ApiResponse<ParkingReviewsSummaryDTO>> GetParkingReviewsAsync(Guid parkingId, int page = 1, int pageSize = 20)
        {
            var parking = await _unitOfWork.Repository<Parking>().GetByIdAsync(parkingId);
            if (parking == null)
            {
                return ApiResponse<ParkingReviewsSummaryDTO>.Failure("Parking facility not found.");
            }

            var query = _unitOfWork.Repository<Review>().Query()
                .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
                .Include(r => r.Reservation.ParkingSpace.Parking)
                .Where(r => r.Reservation.ParkingSpace.ParkingId == parkingId);

            int totalReviews = await query.CountAsync();
            
            double averageRating = 0;
            if (totalReviews > 0)
            {
                averageRating = await query.AverageAsync(r => r.Rating);
                averageRating = Math.Round(averageRating, 1);
            }

            page = page > 0 ? page : 1;
            pageSize = pageSize > 0 ? pageSize : 20;

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
            var reviews = await _unitOfWork.Repository<Review>().Query()
                .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
                .Include(r => r.Reservation.ParkingSpace.Parking)
                .Where(r => r.Reservation.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

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
            var review = await _unitOfWork.Repository<Review>().Query()
                .Include(r => r.Reservation)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
            {
                return ApiResponse.Failure("Review not found.");
            }

            if (review.Reservation.UserId != userId)
            {
                return ApiResponse.Failure("You do not have permission to delete this review.");
            }

            _unitOfWork.Repository<Review>().Delete(review);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Review deleted successfully.");
        }
    }
}
