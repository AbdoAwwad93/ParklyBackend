using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of a parking space returned by the API.</summary>
    public class ParkingSpaceResponseDTO
    {
        /// <summary>The unique id of the parking space.</summary>
        public Guid SpaceId { get; set; }

        /// <summary>The id of the parking facility that owns the space.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The name of the parent parking facility.</summary>
        public string? ParkingName { get; set; }

        /// <summary>The spot number assigned within the parking.</summary>
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>The vehicle size the space accommodates.</summary>
        public VehicleSize? VehicleSize { get; set; }

        /// <summary>The type/purpose of the space (Standard, EVCharging, Accessible, Compact).</summary>
        public SpaceType SpaceType { get; set; }

        /// <summary>The floor/level where the space is located (e.g. "B1", "L1", "Ground").</summary>
        public string? Level { get; set; }

        /// <summary>The base hourly rate for the space.</summary>
        public decimal BaseHourlyRate { get; set; }

        /// <summary>The current operational status of the space (Available, Occupied, Reserved).</summary>
        public SpaceStatus Status { get; set; }

        /// <summary>Whether the space is currently active and bookable.</summary>
        public bool IsActive { get; set; }
    }
}