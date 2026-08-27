using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for creating a new saved favorite place.</summary>
    public class CreateSavedPlaceDTO
    {
        /// <summary>Label or title for the saved place (e.g. "Home", "Work").</summary>
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Street or descriptive address (e.g. "742 Evergreen Ter.").</summary>
        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string Address { get; set; } = string.Empty;

        /// <summary>Geographic latitude coordinate (-90 to 90).</summary>
        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal Latitude { get; set; }

        /// <summary>Geographic longitude coordinate (-180 to 180).</summary>
        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; }

        /// <summary>Category of place (Home or Work).</summary>
        [Required(ErrorMessage = "PlaceType is required.")]
        public PlaceType PlaceType { get; set; }
    }
}
