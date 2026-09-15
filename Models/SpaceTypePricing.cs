using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    /// <summary>Rates configured by an owner for one space type at one parking location.</summary>
    public class SpaceTypePricing
    {
        [Key]
        public Guid SpaceTypePricingId { get; set; } = Guid.NewGuid();
        public Guid ParkingId { get; set; }
        public SpaceType SpaceType { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal HourlyRate { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal DailyRate { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal WeeklyRate { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ParkingId))]
        public Parking Parking { get; set; } = null!;
    }
}
