namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of a parking facility returned by the API.</summary>
    public class ParkingResponseDTO
    {
        /// <summary>The unique id of the parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The id of the parking owner who owns the facility.</summary>
        public Guid OwnerId { get; set; }

        /// <summary>The name of the parking facility.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The street address of the parking facility.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>The latitude coordinate of the parking facility.</summary>
        public decimal Latitude { get; set; }

        /// <summary>The longitude coordinate of the parking facility.</summary>
        public decimal Longitude { get; set; }

        /// <summary>The operating hours of the parking facility broken down by days and hours.</summary>
        public List<OperatingHoursDTO> OperatingHours { get; set; } = new List<OperatingHoursDTO>();

        /// <summary>Indicates whether the facility is currently open based on operating hours.</summary>
        public bool IsOpenNow { get; set; }

        /// <summary>Distance from the query coordinates in kilometers, if coordinates were provided.</summary>
        public double? DistanceKm { get; set; }

        /// <summary>Number of spaces available in the requested (or default) time window.</summary>
        public int AvailableSpaces { get; set; }

        /// <summary>Total active spaces configured in the facility.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>The lowest base hourly rate among active or available spaces.</summary>
        public decimal? MinHourlyRate { get; set; }

        /// <summary>List of amenities or features available at the parking facility.</summary>
        public List<string> Features { get; set; } = new List<string>();

        /// <summary>The average user rating of the parking facility.</summary>
        public double AverageRating { get; set; }

        /// <summary>The total number of reviews for the parking facility.</summary>
        public int TotalReviews { get; set; }
    }
}