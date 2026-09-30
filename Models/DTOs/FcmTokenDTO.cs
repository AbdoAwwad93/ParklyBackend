using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload sent by the mobile client to register or refresh an FCM device token.</summary>
    public class RegisterFcmTokenDTO
    {
        /// <summary>The FCM registration token obtained from the Firebase SDK on the client.</summary>
        [Required, MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        /// <summary>Optional client-supplied device identifier.</summary>
        [MaxLength(50)]
        public string? DeviceId { get; set; }

        /// <summary>Platform hint: "android", "ios", or "web".</summary>
        [MaxLength(20)]
        public string? Platform { get; set; }
    }

    /// <summary>Payload sent by the mobile client to unregister an FCM device token (e.g. on logout).</summary>
    public class UnregisterFcmTokenDTO
    {
        /// <summary>The FCM registration token to remove.</summary>
        [Required, MaxLength(512)]
        public string Token { get; set; } = string.Empty;
    }
}
