namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// User settings for mobile push notifications.
    /// </summary>
    public class PushNotificationSettingsDTO
    {
        /// <summary>Booking alerts & reminders via mobile push notifications.</summary>
        public bool PushNotificationsEnabled { get; set; } = true;
    }
}
