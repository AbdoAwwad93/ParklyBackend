using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    public class ProfileSettingsDTO
    {
        public ProfileSettingsHeaderDTO Header { get; set; } = new();
        public PersonalSettingsDTO Personal { get; set; } = new();
        public BusinessSettingsDTO? Business { get; set; }
        public NotificationSettingsDTO? Notifications { get; set; }
        public SecuritySettingsDTO Security { get; set; } = new();
    }

    public class ProfileSettingsHeaderDTO
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsVerifiedOwner { get; set; } = true;
        public OwnerStatsDTO Stats { get; set; } = new();
    }

    public class OwnerStatsDTO
    {
        public int Locations { get; set; }
        public int TotalSpaces { get; set; }
        public int Bookings { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
        public double AverageRating { get; set; }
    }

    public class PersonalSettingsDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? CityState { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdatePersonalSettingsDTO
    {
        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? CityState { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }
    }

    public class BusinessSettingsDTO
    {
        public string BusinessName { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? StreetAddress { get; set; }
        public string? CityStateZip { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime? BusinessVerifiedAt { get; set; }
    }

    public class UpdateBusinessSettingsDTO
    {
        [Required]
        [MaxLength(255)]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TaxId { get; set; }

        [MaxLength(255)]
        public string? StreetAddress { get; set; }

        [MaxLength(255)]
        public string? CityStateZip { get; set; }
    }

    public class NotificationSettingsDTO
    {
        public bool NewBookings { get; set; } = true;
        public bool Cancellations { get; set; } = true;
        public bool RevenueMilestones { get; set; } = true;
        public bool SpaceAlerts { get; set; } = true;
        public bool MarketingUpdates { get; set; }
    }

    public class SecuritySettingsDTO
    {
        public bool SmsTwoFactorEnabled { get; set; }
        public List<ActiveSessionDTO> ActiveSessions { get; set; } = [];
    }

    public class UpdatePasswordDTO
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Password does not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UpdateTwoFactorDTO
    {
        public bool Enabled { get; set; }
    }

    public class ActiveSessionDTO
    {
        public Guid SessionId { get; set; }
        public string Device { get; set; } = "Unknown device";
        public string Location { get; set; } = "Unknown location";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class DeleteAccountDTO
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
