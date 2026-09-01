using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parkly_Backend.Models.DTOs
{
    public class ParkingDTO
    {
     
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Address { get; set; } = string.Empty;
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Latitude { get; set; }
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Longitude { get; set; }
        [MaxLength(100)]
        public string? OperatingHours { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ParkingSpace> ParkingSpaces { get; set; } = new List<ParkingSpace>();
        public List<PricingRule> PricingRules { get; set; } = new List<PricingRule>();
    }
}
