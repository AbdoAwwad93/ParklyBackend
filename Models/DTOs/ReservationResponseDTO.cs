using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class ReservationResponseDTO
    {
        public Guid ReservationId { get; set; }
        public Guid UserId { get; set; }
        public Guid SpaceId { get; set; }
        public Guid ParkingId { get; set; }
        public string? SpotNumber { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public decimal TotalPrice { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}