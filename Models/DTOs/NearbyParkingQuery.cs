using System;
using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Query parameters for discovering nearby parking options around user coordinates.</summary>
    public class NearbyParkingQuery
    {
        /// <summary>The user's current latitude coordinate (-90 to 90).</summary>
        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal Latitude { get; set; }

        /// <summary>The user's current longitude coordinate (-180 to 180).</summary>
        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; }

        /// <summary>Maximum search radius in kilometers (defaults to 5.0 km, max 100 km).</summary>
        [Range(0.1, 100.0, ErrorMessage = "RadiusKm must be between 0.1 and 100 km.")]
        public double RadiusKm { get; set; } = 5.0;

        /// <summary>Optional filter to only include spaces that support this vehicle size.</summary>
        public VehicleSize? VehicleSize { get; set; }

        /// <summary>Start of the planned parking or arrival window (defaults to UTC now).</summary>
        public DateTime? Arrival { get; set; }

        /// <summary>End of the planned parking or departure window (defaults to one hour after arrival).</summary>
        public DateTime? Departure { get; set; }

        /// <summary>Optional maximum hourly rate filter.</summary>
        [Range(0.0, 10000.0, ErrorMessage = "MaxRate must be a positive number.")]
        public decimal? MaxRate { get; set; }

        /// <summary>If true, only returns facilities or spots that have active availability for the requested window. Defaults to true.</summary>
        public bool OnlyAvailable { get; set; } = true;

        /// <summary>If true, includes facilities that are currently closed or outside operating hours. Defaults to false.</summary>
        public bool IncludeClosed { get; set; } = false;

        /// <summary>Determines the ordering of the results (Distance or Price). Defaults to Distance.</summary>
        public NearbySortBy SortBy { get; set; } = NearbySortBy.Distance;

        /// <summary>The page number for paginated results (1-indexed, default 1).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
        public int Page { get; set; } = 1;

        /// <summary>The number of items per page (default 20, max 100).</summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 20;
    }
}
