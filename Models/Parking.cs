using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parkly_Backend.Models
{
    public class Parking
    {
        [Key]
        public Guid ParkingId { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }
        [Required]
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
        [ForeignKey("OwnerId")]
        public ParkingOwner ParkingOwner { get; set; } = null!;
        public List<ParkingSpace> ParkingSpaces { get; set; } = new List<ParkingSpace>();
        public List<PricingRule> PricingRules { get; set; } = new List<PricingRule>();
    }
}
