using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Paginated response envelope for recommended parking facilities.</summary>
    public class RecommendParkingPageDTO : PagedResult<RecommendParkingDTO>
    {
        public RecommendParkingPageDTO() { }

        public RecommendParkingPageDTO(List<RecommendParkingDTO> items, int totalItems, int page, int pageSize)
            : base(items, totalItems, page, pageSize) { }
    }
}
