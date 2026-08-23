using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    public class ResendVerificationDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
