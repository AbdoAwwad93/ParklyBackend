using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    public class VerifyResetOtpDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }
}
