using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parkly_Backend.Models
{
    /// <summary>Stores an FCM device token for push notifications. One user may have multiple tokens (multiple devices).</summary>
    public class UserFcmToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        /// <summary>The Firebase Cloud Messaging registration token.</summary>
        [Required, MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        /// <summary>Optional client-supplied device identifier to distinguish devices.</summary>
        [MaxLength(50)]
        public string? DeviceId { get; set; }

        /// <summary>Platform hint: "android", "ios", or "web".</summary>
        [MaxLength(20)]
        public string? Platform { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; } = null!;
    }
}
