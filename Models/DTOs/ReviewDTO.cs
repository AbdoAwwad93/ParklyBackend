using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    public class ReviewDTO
    {
        public string UserName { get; set; }
        public Guid ReservationId { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
