namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Aggregated summary data for the dashboard header banner and statistics cards.
    /// </summary>
    public class DashboardSummaryDTO
    {
        /// <summary>Overall occupancy percentage across all owner's locations right now.</summary>
        public int OccupancyPercentage { get; set; }

        /// <summary>Weighted average rating across all of the owner's parking locations.</summary>
        public double AverageRating { get; set; }

        /// <summary>Number of reservations created today (UTC) across all owner's locations.</summary>
        public int TodayBookingsCount { get; set; }

        /// <summary>Total number of active (IsActive) parking spaces across all locations.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Number of spaces that are not currently occupied (checked-in).</summary>
        public int AvailableNow { get; set; }

        /// <summary>Available spaces as a percentage of total (e.g. 37.6 for 37.6%).</summary>
        public double AvailablePercentage { get; set; }

        /// <summary>Count of reservations in Confirmed or CheckedIn status across all owner locations.</summary>
        public int ActiveReservations { get; set; }

        /// <summary>Count of active reservations that are checking in soon (arrival within next 2 hours).</summary>
        public int CheckingInSoon { get; set; }

        /// <summary>Total revenue from completed transactions today (UTC).</summary>
        public decimal TodayRevenue { get; set; }

        /// <summary>Configurable daily revenue target. Defaults to 1500.</summary>
        public decimal RevenueTarget { get; set; }

        /// <summary>Today's revenue as a percentage of the target (e.g. 83 for 83%).</summary>
        public int RevenueTargetPercentage { get; set; }
    }
}
