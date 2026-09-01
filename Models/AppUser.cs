using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;
        [MaxLength(50)]
        public UserRole Role { get; set; } = UserRole.Driver;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePictureUrl { get; set; }
        public ParkingOwner? ParkingOwner { get; set; }
        public List<Reservation> Reservations { get; set; } = new List<Reservation>();
        public List<Dispute> Disputes { get; set; } = new List<Dispute>();
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public List<SavedPlace> SavedPlaces { get; set; } = new List<SavedPlace>();
    }
}
