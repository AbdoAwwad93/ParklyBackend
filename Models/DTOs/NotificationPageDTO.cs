namespace Parkly_Backend.Models.DTOs
{
    public class NotificationPageDTO
    {
        public List<NotificationDTO> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
