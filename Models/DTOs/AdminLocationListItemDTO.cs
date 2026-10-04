namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Represents a single row in the Admin Parking Locations table.
    /// </summary>
    public class AdminLocationListItemDTO
    {
        /// <summary>Unique identifier of the parking location.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>Parking location name (e.g. "Tahrir Square Underground Garage").</summary>
        public string LocationName { get; set; } = string.Empty;

        /// <summary>User ID of the parking owner.</summary>
        public Guid OwnerId { get; set; }

        /// <summary>Registered business or owner name (e.g. "Al-Rashid Parking LLC").</summary>
        public string OwnerName { get; set; } = string.Empty;

        /// <summary>City extracted from location address (e.g. "Downtown", "Assiut", "Al-Korba").</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>Full street address of the location.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Total number of parking spaces at this location.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Number of active spaces accepting bookings.</summary>
        public int ActiveSpaces { get; set; }

        /// <summary>Total revenue generated from completed reservations at this location.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Average customer rating for this location.</summary>
        public double AverageRating { get; set; }

        /// <summary>Display status string: "Active" or "Inactive".</summary>
        public string Status { get; set; } = "Active";

        /// <summary>Boolean indicating whether the location has active spaces.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Date and time when the location was registered.</summary>
        public DateTime? CreatedAt { get; set; }
    }
}
