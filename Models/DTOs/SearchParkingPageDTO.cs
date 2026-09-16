using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Paginated response envelope for search parking facilities.</summary>
    public class SearchParkingPageDTO : PagedResult<SearchParkingDTO>
    {
        public SearchParkingPageDTO() { }

        public SearchParkingPageDTO(List<SearchParkingDTO> items, int totalItems, int page, int pageSize)
            : base(items, totalItems, page, pageSize) { }
    }
}
