using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Request body for updating a parking location's operational status.
    /// </summary>
    public class UpdateLocationStatusDTO
    {
        /// <summary>Target status: "Active" (enables spaces) or "Inactive" (disables spaces).</summary>
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public string Status { get; set; } = string.Empty;
    }
}
