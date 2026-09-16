using System;
using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Query parameters for the parking discovery/search endpoint.</summary>
    public class SearchParkingQuery
    {
        /// <summary>The reference latitude for a radius search.</summary>
        public decimal? Latitude { get; set; }

        /// <summary>The reference longitude for a radius search.</summary>
        public decimal? Longitude { get; set; }

        /// <summary>Maximum distance in kilometers around the given coordinates.</summary>
        public double? RadiusKm { get; set; }

        /// <summary>Keyword to match against the parking name or address.</summary>
        public string? Keyword { get; set; }

        /// <summary>Only show parkings that have a space accommodating this vehicle size.</summary>
        public VehicleSize? VehicleSize { get; set; }

        /// <summary>Start of the availability window. Defaults to now.</summary>
        public DateTime? Arrival { get; set; }

        /// <summary>End of the availability window. Defaults to one hour after arrival.</summary>
        public DateTime? Departure { get; set; }

        /// <summary>Only show parkings that have a space at or below this hourly rate.</summary>
        public decimal? MaxRate { get; set; }

        /// <summary>The page number for paginated results (1-indexed, default 1).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
        public int Page { get; set; } = 1;

        /// <summary>The number of items per page (default 20, max 100).</summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 20;
    }
}