using System;
using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Query parameters for the parking recommendation endpoint.</summary>
    public class RecommendParkingQuery
    {
        /// <summary>The user's current latitude coordinate (optional, -90 to 90).</summary>
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal? Latitude { get; set; }

        /// <summary>The user's current longitude coordinate (optional, -180 to 180).</summary>
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal? Longitude { get; set; }

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

        /// <summary>The number of items to recommend (default 10, max 50).</summary>
        [Range(1, 50, ErrorMessage = "Limit must be between 1 and 50.")]
        public int Limit { get; set; } = 10;
    }
}
