using System;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of a nearby parking facility returned relative to user location.</summary>
    public class NearbyParkingDTO
    {
        /// <summary>The unique identifier of the parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The unique identifier of the parking owner.</summary>
        public Guid OwnerId { get; set; }

        /// <summary>The name of the parking facility.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The street address of the parking facility.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>The latitude coordinate of the facility.</summary>
        public decimal Latitude { get; set; }

        /// <summary>The longitude coordinate of the facility.</summary>
        public decimal Longitude { get; set; }

        /// <summary>The operating hours of the facility (e.g. "08:00 - 22:00").</summary>
        public string? OperatingHours { get; set; }

        /// <summary>Indicates whether the facility is currently open based on operating hours.</summary>
        public bool IsOpenNow { get; set; }

        /// <summary>Distance in kilometers from the user's coordinates, rounded to two decimal places.</summary>
        public double DistanceKm { get; set; }

        /// <summary>Number of available spaces during the requested time window.</summary>
        public int AvailableSpaces { get; set; }

        /// <summary>Total active spaces configured in the facility.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>The minimum base hourly rate among active spaces in the facility.</summary>
        public decimal? MinHourlyRate { get; set; }
    }
}
