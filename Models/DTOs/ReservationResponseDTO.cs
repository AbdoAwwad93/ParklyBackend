using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of a reservation returned by the API.</summary>
    public class ReservationResponseDTO
    {
        /// <summary>The unique id of the reservation.</summary>
        public Guid ReservationId { get; set; }
        /// <summary>The id of the user who owns the reservation.</summary>
        public Guid UserId { get; set; }
        /// <summary>The id of the reserved parking space.</summary>
        public Guid SpaceId { get; set; }
        /// <summary>The id of the parking facility.</summary>
        public Guid ParkingId { get; set; }
        /// <summary>The spot number assigned within the parking.</summary>
        public string? SpotNumber { get; set; }
        /// <summary>The arrival time.</summary>
        public DateTime ArrivalTime { get; set; }
        /// <summary>The departure time.</summary>
        public DateTime DepartureTime { get; set; }
        /// <summary>The calculated total price.</summary>
        public decimal TotalPrice { get; set; }
        /// <summary>The current status of the reservation.</summary>
        public ReservationStatus Status { get; set; }
        /// <summary>When the reservation was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}