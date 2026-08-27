using System;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of a user's saved favorite location.</summary>
    public class SavedPlaceResponseDTO
    {
        /// <summary>Unique identifier of the saved place.</summary>
        public Guid PlaceId { get; set; }

        /// <summary>Unique identifier of the user who owns this saved place.</summary>
        public Guid UserId { get; set; }

        /// <summary>Label or title (e.g. "Home", "Work", "Gym").</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Street address (e.g. "742 Evergreen Ter.").</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Geographic latitude coordinate.</summary>
        public decimal Latitude { get; set; }

        /// <summary>Geographic longitude coordinate.</summary>
        public decimal Longitude { get; set; }

        /// <summary>Category of place (Home, Work).</summary>
        public PlaceType PlaceType { get; set; }

        /// <summary>Timestamp when the place was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
