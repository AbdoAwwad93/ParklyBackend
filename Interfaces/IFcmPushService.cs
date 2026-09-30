namespace Parkly_Backend.Interfaces
{
    /// <summary>Sends Firebase Cloud Messaging push notifications to user devices.</summary>
    public interface IFcmPushService
    {
        /// <summary>
        /// Sends a push notification to all devices registered for the given user.
        /// This is a best-effort operation — failures are logged but do not throw.
        /// </summary>
        Task SendToUserAsync(Guid userId, string title, string body,
            Dictionary<string, string>? data = null);
    }
}
