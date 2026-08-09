using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class Reservation
    {
        [Key]
        public Guid ReservationId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid SpaceId { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalPrice { get; set; }
        [MaxLength(50)]
        public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
        [MaxLength(255)]
        public string? QrCodeHash { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; } = null!;
        [ForeignKey(nameof(SpaceId))]
        public ParkingSpace ParkingSpace { get; set; } = null!;
        public Review? Review { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();
        public List<Dispute> Disputes { get; set; } = new List<Dispute>();
    }
}
