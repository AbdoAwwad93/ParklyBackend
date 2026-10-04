namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Platform-wide aggregated statistics for the admin dashboard header cards.
    /// </summary>
    public class AdminDashboardSummaryDTO
    {
        /// <summary>Total number of users with the Driver role.</summary>
        public int RegisteredDrivers { get; set; }

        /// <summary>Drivers who have at least one reservation in the last 30 days.</summary>
        public int ActiveOnPlatform { get; set; }

        /// <summary>Total number of users with the ParkingOwner role.</summary>
        public int RegisteredOwners { get; set; }

        /// <summary>Total number of parking spaces across all locations.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Number of reservations currently in CheckedIn status platform-wide.</summary>
        public int ActiveNow { get; set; }

        /// <summary>Total revenue from completed reservations today (UTC).</summary>
        public decimal GrossRevenueToday { get; set; }

        /// <summary>Platform commission amount processed today.</summary>
        public decimal PlatformProcessed { get; set; }

        /// <summary>Percentage of spaces that are currently active/live (e.g. 85.3 for 85.3%).</summary>
        public double SpacesLivePercent { get; set; }
    }
}
