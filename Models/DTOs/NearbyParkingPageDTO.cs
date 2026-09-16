using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Paginated response envelope for nearby parking facilities.</summary>
    public class NearbyParkingPageDTO : PagedResult<NearbyParkingDTO>
    {
        public NearbyParkingPageDTO() { }

        public NearbyParkingPageDTO(List<NearbyParkingDTO> items, int totalItems, int page, int pageSize)
            : base(items, totalItems, page, pageSize) { }
    }
}
