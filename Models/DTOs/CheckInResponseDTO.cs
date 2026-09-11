using System;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details returned upon a successful entry scan or check-in.</summary>
    public class CheckInResponseDTO
    {
        /// <summary>The unique id of the reservation.</summary>
        public Guid ReservationId { get; set; }

        /// <summary>The unique id of the parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The name of the parking facility.</summary>
        public string ParkingName { get; set; } = string.Empty;

        /// <summary>The street address of the parking facility.</summary>
        public string ParkingAddress { get; set; } = string.Empty;

        /// <summary>The unique id of the assigned parking space.</summary>
        public Guid SpaceId { get; set; }

        /// <summary>The spot number assigned within the parking facility.</summary>
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>The current reservation status (e.g. "CheckedIn").</summary>
        public string Status { get; set; } = "CheckedIn";

        /// <summary>The actual timestamp when the user checked in (UTC).</summary>
        public DateTime CheckInTime { get; set; }

        /// <summary>The scheduled arrival time for the reservation (UTC).</summary>
        public DateTime ScheduledArrivalTime { get; set; }

        /// <summary>The scheduled departure time for the reservation (UTC).</summary>
        public DateTime ScheduledDepartureTime { get; set; }

        /// <summary>The total scheduled duration in hours.</summary>
        public double TotalHours { get; set; }

        /// <summary>The total price booked for the reservation.</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>The 6-digit access / QR code for the reservation.</summary>
        public string? QrCode { get; set; }
    }
}
