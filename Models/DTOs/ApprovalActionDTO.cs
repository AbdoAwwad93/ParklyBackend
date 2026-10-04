using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Request body for approving or rejecting a parking owner application.
    /// </summary>
    public class ApprovalActionDTO
    {
        /// <summary>The action to perform: "Approve" or "Reject".</summary>
        [Required]
        [RegularExpression("^(Approve|Reject)$", ErrorMessage = "Action must be 'Approve' or 'Reject'.")]
        public string Action { get; set; } = string.Empty;
    }
}
