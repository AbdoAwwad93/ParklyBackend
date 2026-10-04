using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Request body for updating a parking owner's verification status.
    /// </summary>
    public class UpdateOwnerStatusDTO
    {
        /// <summary>Target status: "Active" (Verified), "Pending", or "Suspended".</summary>
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Active|Pending|Suspended)$", ErrorMessage = "Status must be 'Active', 'Pending', or 'Suspended'.")]
        public string Status { get; set; } = string.Empty;
    }
}
