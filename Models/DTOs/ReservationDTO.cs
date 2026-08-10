using Parkly_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parkly_Backend.Models.DTOs
{
    public class ReservationDTO
    {
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
     
    }
}
