using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class NotificationDTO
    {
        public Guid Id { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public Guid? ParkingId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? SpaceId { get; set; }
        public string? ActionUrl { get; set; }
    }
}
