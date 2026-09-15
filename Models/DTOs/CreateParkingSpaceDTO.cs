using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for creating a new parking space within a parking facility.</summary>
    public class CreateParkingSpaceDTO
    {
        /// <summary>The id of the parking facility that owns the space.</summary>
        [Required(ErrorMessage = "ParkingId is required.")]
        public Guid ParkingId { get; set; }

        /// <summary>The spot number assigned within the parking (e.g. "A1").</summary>
        [Required(ErrorMessage = "SpotNumber is required.")]
        [MaxLength(50, ErrorMessage = "SpotNumber cannot exceed 50 characters.")]
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>The vehicle size the space accommodates.</summary>
        public VehicleSize? VehicleSize { get; set; }

        /// <summary>The type/purpose of the space (Standard, EVCharging, Accessible, Compact). Defaults to Standard.</summary>
        public SpaceType SpaceType { get; set; } = SpaceType.Standard;

        /// <summary>The floor/level where the space is located (e.g. "B1", "L1", "Ground"). Optional.</summary>
        [MaxLength(20, ErrorMessage = "Level cannot exceed 20 characters.")]
        public string? Level { get; set; }

        /// <summary>The base hourly rate for the space.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "BaseHourlyRate cannot be negative.")]
        public decimal BaseHourlyRate { get; set; }

        /// <summary>Whether the space is currently active and bookable.</summary>
        public bool IsActive { get; set; } = true;
    }
}