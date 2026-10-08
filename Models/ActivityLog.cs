using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parkly_Backend.Models
{
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        [Key]
        public Guid ActivityId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public Guid? ActorUserId { get; set; }

        [MaxLength(255)]
        public string? ActorName { get; set; }

        public Guid? TargetEntityId { get; set; }

        [MaxLength(50)]
        public string? TargetEntityType { get; set; }

        public Guid? ParkingId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
