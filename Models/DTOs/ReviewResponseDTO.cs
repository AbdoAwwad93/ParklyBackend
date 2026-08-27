using System;

namespace Parkly_Backend.Models.DTOs
{
    public class ReviewResponseDTO
    {
        public Guid ReviewId { get; set; }
        public Guid ReservationId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ParkingName { get; set; } = string.Empty;
    }
}
