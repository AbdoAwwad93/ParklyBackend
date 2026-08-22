using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    public class UpdateProfileDTO
    {
        [Required]
        [MaxLength(128)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
