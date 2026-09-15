using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IPricingService
    {
        Task<decimal> CalculateTotalPriceAsync(Guid spaceId, DateTime arrival, DateTime departure);
        Task<ApiResponse<List<PricingLocationDTO>>> GetOwnerLocationsAsync(Guid ownerId);
        Task<ApiResponse<ParkingPricingDTO>> GetParkingPricingAsync(Guid ownerId, Guid parkingId);
        Task<ApiResponse<SpaceTypePricingDTO>> UpsertSpaceTypePricingAsync(Guid ownerId, Guid parkingId, UpsertSpaceTypePricingDTO dto);
    }
}
