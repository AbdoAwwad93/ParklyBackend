using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class Transaction
    {
        [Key]
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public Guid ReservationId { get; set; }
        [MaxLength(50)]
        public TransactionType TransactionType { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }
        [MaxLength(50)]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        [MaxLength(255)]
        public string? GatewayReference { get; set; }
        [ForeignKey("ReservationId")]
        public Reservation Reservation { get; set; } = null!;
    }
}
