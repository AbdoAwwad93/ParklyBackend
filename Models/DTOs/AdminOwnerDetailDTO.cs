namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Detailed information about a parking owner, including business details and locations.
    /// </summary>
    public class AdminOwnerDetailDTO
    {
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? StreetAddress { get; set; }
        public string? CityStateZip { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? BusinessVerifiedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LocationsCount { get; set; }
        public int TotalSpaces { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<AdminOwnerParkingDTO> Parkings { get; set; } = new();
    }

    /// <summary>
    /// Summary of a parking location operated by the owner.
    /// </summary>
    public class AdminOwnerParkingDTO
    {
        public Guid ParkingId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalSpaces { get; set; }
        public int ActiveSpaces { get; set; }
        public double AverageRating { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
