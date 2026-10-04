namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Represents a single driver row in the Users Management table.
    /// </summary>
    public class AdminDriverListItemDTO
    {
        /// <summary>The driver's user ID.</summary>
        public Guid UserId { get; set; }

        /// <summary>Full name of the driver.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Initials for avatar fallback (e.g. "JD").</summary>
        public string Initials { get; set; } = string.Empty;

        /// <summary>Driver's email address.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Driver's phone number.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Profile picture URL, if available.</summary>
        public string? ProfilePictureUrl { get; set; }

        /// <summary>UTC timestamp when the driver registered.</summary>
        public DateTime RegisteredAt { get; set; }

        /// <summary>Total number of reservations made by this driver.</summary>
        public int ReservationsCount { get; set; }

        /// <summary>Total money spent across all non-cancelled reservations.</summary>
        public decimal TotalSpent { get; set; }

        /// <summary>UTC timestamp of latest reservation or registration date.</summary>
        public DateTime LastActive { get; set; }

        /// <summary>Current account status: "Active", "Pending", or "Suspended".</summary>
        public string Status { get; set; } = string.Empty;
    }
}
