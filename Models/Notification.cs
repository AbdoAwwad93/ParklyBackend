using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    /// <summary>A durable notification addressed to a parking owner.</summary>
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        public Guid RecipientUserId { get; set; }
        public NotificationType Type { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public Guid? ParkingId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? SpaceId { get; set; }

        [ForeignKey(nameof(RecipientUserId))]
        public AppUser RecipientUser { get; set; } = null!;
    }
}
