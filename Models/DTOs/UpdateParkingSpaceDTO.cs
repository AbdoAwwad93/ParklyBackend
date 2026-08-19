using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for updating an existing parking space.</summary>
    public class UpdateParkingSpaceDTO
    {
        /// <summary>The spot number assigned within the parking (e.g. "A1").</summary>
        [Required(ErrorMessage = "SpotNumber is required.")]
        [MaxLength(50, ErrorMessage = "SpotNumber cannot exceed 50 characters.")]
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>The vehicle size the space accommodates.</summary>
        public VehicleSize? VehicleSize { get; set; }

        /// <summary>The base hourly rate for the space.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "BaseHourlyRate cannot be negative.")]
        public decimal BaseHourlyRate { get; set; }

        /// <summary>Whether the space is currently active and bookable.</summary>
        public bool IsActive { get; set; }
    }
}