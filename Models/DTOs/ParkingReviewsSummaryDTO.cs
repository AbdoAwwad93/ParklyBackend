using System;
using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    public class ParkingReviewsSummaryDTO
    {
        public Guid ParkingId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ReviewResponseDTO> Reviews { get; set; } = new List<ReviewResponseDTO>();
    }
}
