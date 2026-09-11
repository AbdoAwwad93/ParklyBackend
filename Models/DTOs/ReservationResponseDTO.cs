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
        /// <summary>The name of the parking facility.</summary>
        public string ParkingName { get; set; } = string.Empty;
        /// <summary>The address of the parking facility.</summary>
        public string ParkingAddress { get; set; } = string.Empty;
        /// <summary>The spot number assigned within the parking.</summary>
        public string? SpotNumber { get; set; }
        /// <summary>The base hourly rate for the reserved space.</summary>
        public decimal? HourlyRate { get; set; }
        /// <summary>The scheduled arrival time.</summary>
        public DateTime ArrivalTime { get; set; }
        /// <summary>The scheduled departure time.</summary>
        public DateTime DepartureTime { get; set; }
        /// <summary>The actual timestamp when the user checked in (UTC), if checked in.</summary>
        public DateTime? CheckInTime { get; set; }
        /// <summary>The calculated total price.</summary>
        public decimal TotalPrice { get; set; }
        /// <summary>The current status of the reservation.</summary>
        public ReservationStatus Status { get; set; }
        /// <summary>The 6-digit access / QR code for gate entry.</summary>
        public string? QrCode { get; set; }
        /// <summary>When the reservation was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}