using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class SpaceTypePricingDTO
    {
        public Guid? PricingId { get; set; }
        public SpaceType SpaceType { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal DailyRate { get; set; }
        public decimal WeeklyRate { get; set; }
        public bool IsConfigured { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ParkingPricingDTO
    {
        public Guid ParkingId { get; set; }
        public string ParkingName { get; set; } = string.Empty;
        public List<SpaceTypePricingDTO> Rates { get; set; } = [];
    }

    public class PricingLocationDTO
    {
        public Guid ParkingId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UpsertSpaceTypePricingDTO
    {
        [Required]
        public SpaceType SpaceType { get; set; }
        [Range(0.01, double.MaxValue)]
        public decimal HourlyRate { get; set; }
        [Range(0.01, double.MaxValue)]
        public decimal DailyRate { get; set; }
        [Range(0.01, double.MaxValue)]
        public decimal WeeklyRate { get; set; }
    }
}
