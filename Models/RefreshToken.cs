using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parkly_Backend.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public string JwtId { get; set; } = string.Empty;
        
        public bool IsUsed { get; set; }
        
        public bool IsRevoked { get; set; }
        
        public DateTime AddedDate { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        
        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; } = null!;
    }
}
