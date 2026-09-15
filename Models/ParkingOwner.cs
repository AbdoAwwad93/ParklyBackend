using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class ParkingOwner
    {
        [Key]
        [ForeignKey(nameof(User))]
        public Guid OwnerId { get; set; }
        [Required]
        [MaxLength(255)]
        public string CompanyName { get; set; } = string.Empty;
        [MaxLength(255)]
        public string? PayoutAccount { get; set; }
        [MaxLength(50)]
        public string? TaxId { get; set; }
        [MaxLength(255)]
        public string? StreetAddress { get; set; }
        [MaxLength(255)]
        public string? CityStateZip { get; set; }
        [MaxLength(50)]
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public DateTime? BusinessVerifiedAt { get; set; }
        public bool NotifyNewBookings { get; set; } = true;
        public bool NotifyCancellations { get; set; } = true;
        public bool NotifyRevenueMilestones { get; set; } = true;
        public bool NotifySpaceAlerts { get; set; } = true;
        public bool NotifyMarketingUpdates { get; set; }
        public AppUser User { get; set; } = null!;
        public List<Parking> Parkings { get; set; } = new List<Parking>();
    }
}
