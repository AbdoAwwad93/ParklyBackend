using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface ISavedPlacesService
    {
        Task<ApiResponse<List<SavedPlaceResponseDTO>>> GetUserSavedPlacesAsync(Guid userId);
        Task<ApiResponse<SavedPlaceResponseDTO>> GetByIdAsync(Guid userId, Guid placeId);
        Task<ApiResponse<SavedPlaceResponseDTO>> CreateAsync(Guid userId, CreateSavedPlaceDTO dto);
        Task<ApiResponse<SavedPlaceResponseDTO>> UpdateAsync(Guid userId, Guid placeId, UpdateSavedPlaceDTO dto);
        Task<ApiResponse> DeleteAsync(Guid userId, Guid placeId);
    }
}
