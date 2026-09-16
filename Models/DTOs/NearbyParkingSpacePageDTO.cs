using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Paginated response envelope for nearby individual parking spaces.</summary>
    public class NearbyParkingSpacePageDTO : PagedResult<NearbyParkingSpaceDTO>
    {
        public NearbyParkingSpacePageDTO() { }

        public NearbyParkingSpacePageDTO(List<NearbyParkingSpaceDTO> items, int totalItems, int page, int pageSize)
            : base(items, totalItems, page, pageSize) { }
    }
}
