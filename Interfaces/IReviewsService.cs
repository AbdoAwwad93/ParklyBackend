using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IReviewsService
    {
        Task<ApiResponse<ReviewResponseDTO>> CreateAsync(Guid userId, CreateReviewDTO dto);
        Task<ApiResponse<ReviewResponseDTO>> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewDTO dto);
        Task<ApiResponse<ParkingReviewsSummaryDTO>> GetParkingReviewsAsync(Guid parkingId, int page = 1, int pageSize = 20);
        Task<ApiResponse<List<ReviewResponseDTO>>> GetUserReviewsAsync(Guid userId);
        Task<ApiResponse> DeleteAsync(Guid userId, Guid reviewId);
    }
}
