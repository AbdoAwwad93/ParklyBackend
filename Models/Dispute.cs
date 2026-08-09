using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class Dispute
    {
        [Key]
        public Guid DisputeId { get; set; } = Guid.NewGuid();
        public Guid ReservationId { get; set; }
        public Guid RaisedBy { get; set; }
        public string? Reason { get; set; }
        [MaxLength(50)]
        public DisputeStatus Status { get; set; } = DisputeStatus.Open;
        [ForeignKey(nameof(ReservationId))]
        public Reservation Reservation { get; set; } = null!;
        [ForeignKey(nameof(RaisedBy))]
        public AppUser User { get; set; } = null!;
    }
}
