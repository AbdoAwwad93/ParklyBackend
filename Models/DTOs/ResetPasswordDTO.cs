using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for resetting the password using a received OTP.</summary>
    public class ResetPasswordDTO
    {
        /// <summary>The registered email address.</summary>
        [Required(ErrorMessage = "Email is required!")]
        [EmailAddress(ErrorMessage = "Email is not valid!")]
        public string Email { get; set; }
        /// <summary>The one-time password received by email.</summary>
        [Required(ErrorMessage = "OTP is required!")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        public string Otp { get; set; }
        /// <summary>The new password.</summary>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        /// <summary>Confirmation of the new password.</summary>
        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Password does not match.")]
        public string ConfirmPassword { get; set; }
    }
}