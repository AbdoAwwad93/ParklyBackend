namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Represents a parking owner application awaiting admin approval.
    /// </summary>
    public class PendingApprovalDTO
    {
        /// <summary>The owner's user ID.</summary>
        public Guid OwnerId { get; set; }

        /// <summary>Full name of the owner.</summary>
        public string OwnerName { get; set; } = string.Empty;

        /// <summary>Two-letter initials derived from the owner's name.</summary>
        public string OwnerInitials { get; set; } = string.Empty;

        /// <summary>The company or parking name.</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>City/state location of the owner.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Current verification status (always "Pending" in this context).</summary>
        public string VerificationStatus { get; set; } = string.Empty;

        /// <summary>When the owner application was submitted (based on user's CreatedAt).</summary>
        public DateTime AppliedAt { get; set; }
    }
}
