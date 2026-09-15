namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// A single item in the dashboard's Recent Activity feed.
    /// </summary>
    public class ActivityFeedItemDTO
    {
        /// <summary>
        /// Category of the activity event.
        /// Possible values: "CheckIn", "CheckOut", "NewBooking", "Cancellation", "Milestone".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Human-readable description of the event (e.g. "Sarah Mitchell checked in to CityPark Central — A-02").</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Relative time string (e.g. "4 min ago", "1h 10m ago").</summary>
        public string TimeAgo { get; set; } = string.Empty;
    }
}
