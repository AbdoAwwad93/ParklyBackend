using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    public class VerifyEmailDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 characters.")]
        public string Otp { get; set; } = string.Empty;
    }
}
