using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for requesting a password reset OTP.</summary>
    public class ForgotPasswordDTO
    {
        /// <summary>The registered email address.</summary>
        [Required(ErrorMessage = "Email is required!")]
        [EmailAddress(ErrorMessage = "Email is not valid!")]
        public string Email { get; set; }
    }
}