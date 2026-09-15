namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Status and occupancy data for a single parking location, used in the dashboard Locations panel.
    /// </summary>
    public class LocationStatusDTO
    {
        /// <summary>Unique identifier of the parking location.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>Display name of the parking location.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Address of the parking location.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Whether this location has at least one active parking space.</summary>
        public bool IsActive { get; set; }

        /// <summary>Number of spaces currently occupied (status = CheckedIn).</summary>
        public int OccupiedSpaces { get; set; }

        /// <summary>Total number of active spaces at this location.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Occupancy percentage (0–100) for the progress bar.</summary>
        public int OccupancyPercentage { get; set; }
    }
}
