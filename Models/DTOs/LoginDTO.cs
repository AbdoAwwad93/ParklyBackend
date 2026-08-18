using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Credentials used to authenticate an existing user.</summary>
    public class LoginDTO
    {
        /// <summary>The registered email address.</summary>
        [Required(ErrorMessage ="Email is required!")]
        [EmailAddress]
        public string Email { get; set; }
        /// <summary>The account password.</summary>
        [Required(ErrorMessage ="Password is required!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }


    }
}
