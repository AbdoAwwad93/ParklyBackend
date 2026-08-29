using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for creating a new parking facility.</summary>
    public class CreateParkingDTO
    {
        /// <summary>The name of the parking facility.</summary>
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>The street address of the parking facility.</summary>
        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        /// <summary>The latitude coordinate of the parking facility.</summary>
        public decimal Latitude { get; set; }

        /// <summary>The longitude coordinate of the parking facility.</summary>
        public decimal Longitude { get; set; }

        /// <summary>The operating hours of the parking facility (e.g. "06:00 - 22:00").</summary>
        [MaxLength(100, ErrorMessage = "Operating hours cannot exceed 100 characters.")]
        public string? OperatingHours { get; set; }

        /// <summary>List of amenities or features available at the parking facility.</summary>
        public List<ParkingFeature> Features { get; set; } = new List<ParkingFeature>();
    }
}