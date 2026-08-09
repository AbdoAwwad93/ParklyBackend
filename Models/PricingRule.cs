using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class PricingRule
    {
        [Key]
        public Guid RuleId { get; set; } = Guid.NewGuid();
        public Guid ParkingId { get; set; }
        [MaxLength(50)]
        public PricingRuleType RuleType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal PriceModifier { get; set; }
        [ForeignKey("ParkingId")]
        public Parking Parking { get; set; } = null!;
    }
}
