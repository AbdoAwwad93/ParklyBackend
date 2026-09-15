using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class NotificationTypeCountDTO
    {
        public NotificationType Type { get; set; }
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
    }

    public class NotificationSummaryDTO
    {
        public int UnreadCount { get; set; }
        public List<NotificationTypeCountDTO> ByType { get; set; } = [];
    }
}
