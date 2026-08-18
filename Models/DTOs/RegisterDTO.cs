using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for creating a new user account.</summary>
    public class RegisterDTO
    {
        /// <summary>The user's first name.</summary>
        public string FirstName { get; set; }
        /// <summary>The user's last name.</summary>
        public string LastName { get; set; }
        /// <summary>The desired username.</summary>
        public string UserName { get; set; }
        /// <summary>The user's email address. Must be unique.</summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
        /// <summary>The user's phone number.</summary>
        public string Phone { get; set; }
        /// <summary>The password. At least 8 characters.</summary>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        /// <summary>Confirmation of the password. Must match <see cref="Password"/>.</summary>
        [Required(ErrorMessage = "Confirm Password is required.")]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password does not match.")]
        public string ConfirmPassword { get; set; }
       
    }
}
