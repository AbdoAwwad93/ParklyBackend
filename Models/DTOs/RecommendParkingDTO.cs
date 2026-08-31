using System;
using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    public class RecommendParkingDTO
    {
        public Guid ParkingId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? OperatingHours { get; set; }
        public bool IsOpenNow { get; set; }
        public double? DistanceKm { get; set; }
        public int AvailableSpaces { get; set; }
        public int TotalSpaces { get; set; }
        public decimal? MinHourlyRate { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        
        /// <summary>
        /// A string explaining why this parking was recommended.
        /// Examples: "Near your Home", "Frequently visited", "Near your current location", "Popular near you".
        /// </summary>
        public string? RecommendationReason { get; set; }
    }
}
