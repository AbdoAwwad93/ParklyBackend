using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models
{
    public class ParkingSpace
    {
        [Key]
        public Guid SpaceId { get; set; } = Guid.NewGuid();
        public Guid ParkingId { get; set; }
        [Required]
        [MaxLength(50)]
        public string SpotNumber { get; set; } = string.Empty;
        [MaxLength(50)]
        public VehicleSize? VehicleSize { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal BaseHourlyRate { get; set; }
        public bool IsActive { get; set; } = true;
        [ForeignKey("ParkingId")]
        public Parking Parking { get; set; } = null!;
        public List<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
