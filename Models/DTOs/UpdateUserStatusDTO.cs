using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Request body for updating a user account status (e.g. suspend or activate).
    /// </summary>
    public class UpdateUserStatusDTO
    {
        /// <summary>Target status: "Active" or "Suspended".</summary>
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Active|Suspended)$", ErrorMessage = "Status must be either 'Active' or 'Suspended'.")]
        public string Status { get; set; } = string.Empty;
    }
}
