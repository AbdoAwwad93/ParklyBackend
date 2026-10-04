namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Aggregated metrics for the 4 header cards on the Admin Parking Locations page.
    /// </summary>
    public class AdminLocationStatsDTO
    {
        /// <summary>Total number of registered parking locations across the network.</summary>
        public int TotalLocations { get; set; }

        /// <summary>Number of active parking locations (locations with active spaces accepting bookings).</summary>
        public int Active { get; set; }

        /// <summary>Total number of parking spaces across all locations in the network.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Total platform revenue generated from completed reservations across all locations.</summary>
        public decimal NetworkRevenue { get; set; }
    }
}
