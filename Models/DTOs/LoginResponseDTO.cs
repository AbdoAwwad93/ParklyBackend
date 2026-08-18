using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload returned after a successful login.</summary>
    public class LoginResponseDTO
    {
        /// <summary>The JWT used to authenticate subsequent requests.</summary>
        public string Token { get; set; }
        /// <summary>The authenticated user's identifier.</summary>
        public Guid Id { get; set; }
        /// <summary>The authenticated user's username.</summary>
        public string UserName { get; set; }
        /// <summary>The authenticated user's full name.</summary>
        public string FullName { get; set; }
        /// <summary>The authenticated user's email.</summary>
        public string Email { get; set; }
        /// <summary>The authenticated user's role.</summary>
        public UserRole Role { get; set; }
    }
}