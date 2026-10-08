namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// A single item in the admin dashboard's platform-wide Recent Activity feed.
    /// </summary>
    public class AdminActivityFeedItemDTO
    {
        /// <summary>
        /// Category of the activity event.
        /// Possible values: "NewAccount", "OwnerApplication", "NewBooking", "OccupancyAlert", "RevenueMilestone", "NewParking".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Human-readable description of the event.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the event occurred.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Relative time string (e.g. "5 min ago", "1h ago").</summary>
        public string TimeAgo { get; set; } = string.Empty;
    }
}
