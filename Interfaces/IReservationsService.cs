using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;
using System.Collections.Generic;

namespace Parkly_Backend.Interfaces
{
    public interface IReservationsService
    {
        Task<ApiResponse<ReservationResponseDTO>> CreateAsync(Guid userId, CreateReservationDTO dto);
        Task<ApiResponse<ReservationResponseDTO>> UpdateAsync(Guid userId, Guid reservationId, UpdateReservationDTO dto);
        Task<ApiResponse> CancelAsync(Guid userId, Guid reservationId);
        Task<ApiResponse<string>> GetQrCodeAsync(Guid userId, Guid reservationId);
        Task<ApiResponse<ReservationResponseDTO>> GetByIdAsync(Guid userId, Guid reservationId);
        Task<ApiResponse<CheckOutResponseDTO>> GetCheckoutPreviewAsync(Guid userId, Guid reservationId);
        Task<ApiResponse<List<ReservationResponseDTO>>> GetUserReservationsAsync(Guid userId);
        Task<ApiResponse<List<ReservationResponseDTO>>> GetActiveUserReservationsAsync(Guid userId);
        Task<ApiResponse<OwnerReservationsPageDTO>> GetOwnerReservationsAsync(Guid ownerId, string? status, string? search, int page, int pageSize);
        Task<ApiResponse<OwnerReservationListItemDTO>> GetOwnerReservationByIdAsync(Guid ownerId, Guid reservationId);
    }
}
