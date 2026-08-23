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
        /// <summary>The secure reset token received after verifying the OTP.</summary>
        [Required(ErrorMessage = "ResetToken is required!")]
        public string ResetToken { get; set; }
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