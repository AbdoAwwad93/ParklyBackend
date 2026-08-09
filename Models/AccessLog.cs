using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class AccessLog
    {
        [Key]
        public Guid LogId { get; set; } = Guid.NewGuid();
        public Guid ReservationId { get; set; }
        [MaxLength(50)]
        public ScanType ScanType { get; set; }
        public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;
        [MaxLength(50)]
        public string? GateId { get; set; }
        [ForeignKey("ReservationId")]
        public Reservation Reservation { get; set; } = null!;
    }
}
