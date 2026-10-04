namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Detailed information about a parking location for the admin view.
    /// </summary>
    public class AdminLocationDetailDTO
    {
        public Guid ParkingId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerBusinessName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? OperatingHours { get; set; }
        public int TotalSpaces { get; set; }
        public int ActiveSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public List<string> Features { get; set; } = new();
        public List<AdminLocationPricingRuleDTO> PricingRules { get; set; } = new();
    }

    /// <summary>
    /// Pricing rule item for the admin location detail view.
    /// </summary>
    public class AdminLocationPricingRuleDTO
    {
        public Guid RuleId { get; set; }
        public string RuleType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal PriceModifier { get; set; }
    }
}
