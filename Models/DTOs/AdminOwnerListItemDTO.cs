namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Represents a single owner row in the Parking Owners table.
    /// </summary>
    public class AdminOwnerListItemDTO
    {
        /// <summary>The owner's user ID.</summary>
        public Guid OwnerId { get; set; }

        /// <summary>The owner's full name.</summary>
        public string OwnerName { get; set; } = string.Empty;

        /// <summary>Initials for avatar rendering (e.g. "AW").</summary>
        public string Initials { get; set; } = string.Empty;

        /// <summary>Registered company / business name.</summary>
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>Owner's email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Owner's phone number.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the account was created.</summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>Formatted registration status string (e.g. "Live Registered").</summary>
        public string RegisteredDisplay { get; set; } = "Live Registered";

        /// <summary>Total number of parking locations operated by this owner.</summary>
        public int LocationsCount { get; set; }

        /// <summary>Total number of parking spaces across all locations.</summary>
        public int TotalSpaces { get; set; }

        /// <summary>Total revenue generated across all parking spaces owned by this owner.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Owner's current status: "Active", "Pending", or "Suspended".</summary>
        public string Status { get; set; } = string.Empty;
    }
}
