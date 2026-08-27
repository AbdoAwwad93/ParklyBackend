using System;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of an individual bookable parking space returned relative to user location.</summary>
    public class NearbyParkingSpaceDTO
    {
        /// <summary>The unique identifier of the parking space (use directly for reservation booking).</summary>
        public Guid SpaceId { get; set; }

        /// <summary>The spot number or label assigned within the parking facility.</summary>
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>The vehicle size supported by this space.</summary>
        public VehicleSize? VehicleSize { get; set; }

        /// <summary>The base hourly rate for this space.</summary>
        public decimal BaseHourlyRate { get; set; }

        /// <summary>The identifier of the parent parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The name of the parent parking facility.</summary>
        public string ParkingName { get; set; } = string.Empty;

        /// <summary>The physical address of the parent parking facility.</summary>
        public string ParkingAddress { get; set; } = string.Empty;

        /// <summary>The latitude coordinate of the facility.</summary>
        public decimal Latitude { get; set; }

        /// <summary>The longitude coordinate of the facility.</summary>
        public decimal Longitude { get; set; }

        /// <summary>Distance in kilometers from the user's coordinates, rounded to two decimal places.</summary>
        public double DistanceKm { get; set; }

        /// <summary>Whether this space is currently confirmed available for the requested time window.</summary>
        public bool IsAvailable { get; set; }
    }
}
