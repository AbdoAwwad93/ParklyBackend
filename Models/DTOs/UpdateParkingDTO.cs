using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for updating an existing parking facility.</summary>
    public class UpdateParkingDTO
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
    }
}