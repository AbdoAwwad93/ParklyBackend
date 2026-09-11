using System;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details returned upon a successful exit scan, check-out, or pre-checkout preview.</summary>
    public class CheckOutResponseDTO
    {
        /// <summary>The unique id of the reservation.</summary>
        public Guid ReservationId { get; set; }

        /// <summary>The unique id of the parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The name of the parking facility.</summary>
        public string ParkingName { get; set; } = string.Empty;

        /// <summary>The street address of the parking facility.</summary>
        public string ParkingAddress { get; set; } = string.Empty;

        /// <summary>The spot number assigned within the parking facility.</summary>
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>The current status of the reservation (e.g. "Completed").</summary>
        public string Status { get; set; } = "Completed";

        /// <summary>Formatted date string of the checkout session (e.g. "Sat, Aug 23, 2026").</summary>
        public string DateFormatted { get; set; } = string.Empty;

        /// <summary>The actual timestamp when the user checked in (UTC).</summary>
        public DateTime CheckInTime { get; set; }

        /// <summary>The actual timestamp when the user checked out (UTC).</summary>
        public DateTime CheckOutTime { get; set; }

        /// <summary>Formatted human-readable duration string (e.g. "2 hrs 47 min").</summary>
        public string DurationFormatted { get; set; } = string.Empty;

        /// <summary>Total parked duration in minutes.</summary>
        public double TotalMinutes { get; set; }

        /// <summary>The base hourly rate of the space.</summary>
        public decimal HourlyRate { get; set; }

        /// <summary>The calculated parking duration cost.</summary>
        public decimal DurationCost { get; set; }

        /// <summary>The service fee applied to the session.</summary>
        public decimal ServiceFee { get; set; }

        /// <summary>The total amount charged for the parking session.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>The user email to which the receipt was sent.</summary>
        public string UserEmail { get; set; } = string.Empty;
    }
}
