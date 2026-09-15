using Parkly_Backend.Models.DTOs;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Aggregated details for a single owner parking location (Location Details modal).</summary>
    public class LocationDetailsDTO
    {
        /// <summary>Unique identifier of the parking location.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>Display name of the parking location.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Full street address of the parking location.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>City portion derived from the address (e.g. "New York, NY").</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>Total number of active spaces.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Number of spaces currently free (Total - Occupied - Reserved).</summary>
        public int AvailableSpaces { get; set; }

        /// <summary>Number of spaces currently occupied (CheckedIn reservations).</summary>
        public int OccupiedSpaces { get; set; }

        /// <summary>Number of spaces currently reserved (Confirmed overlapping now).</summary>
        public int ReservedSpaces { get; set; }

        /// <summary>Lowest base hourly rate among active spaces (shown as Base Price).</summary>
        public decimal? BasePrice { get; set; }

        /// <summary>Average user rating of the parking facility.</summary>
        public double AverageRating { get; set; }

        /// <summary>Total number of reviews for the parking facility.</summary>
        public int TotalReviews { get; set; }

        /// <summary>Revenue for the current month (Completed reservations).</summary>
        public decimal ThisMonthRevenue { get; set; }

        /// <summary>Whether the location has at least one active space.</summary>
        public bool IsActive { get; set; }

        /// <summary>Whether the facility is currently open based on operating hours.</summary>
        public bool IsOpenNow { get; set; }

        /// <summary>Display status (e.g. "Active & Accepting Bookings", "Closed", "Inactive").</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>Operating hours broken down by days and hours.</summary>
        public List<OperatingHoursDTO> OperatingHours { get; set; } = new List<OperatingHoursDTO>();

        /// <summary>Amenities or features available at the parking facility.</summary>
        public List<string> Features { get; set; } = new List<string>();
    }
}
