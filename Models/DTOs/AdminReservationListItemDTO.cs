using System;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Represents a single reservation row in the Admin Reservations table matching the UI layout.
    /// </summary>
    public class AdminReservationListItemDTO
    {
        /// <summary>Unique identifier of the reservation.</summary>
        public Guid ReservationId { get; set; }

        /// <summary>Booking reference code (e.g. "PK-8492").</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Customer user ID.</summary>
        public Guid CustomerId { get; set; }

        /// <summary>Customer's full name.</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Customer initials for avatar icon.</summary>
        public string CustomerInitials { get; set; } = string.Empty;

        /// <summary>Customer's email address.</summary>
        public string CustomerEmail { get; set; } = string.Empty;

        /// <summary>Location / Parking name.</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>Unique identifier of the parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>Parking space spot number (e.g. "A-12").</summary>
        public string Space { get; set; } = string.Empty;

        /// <summary>Unique identifier of the parking space.</summary>
        public Guid SpaceId { get; set; }

        /// <summary>Display date of the reservation (e.g. "Oct 04, 2026").</summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>Display time window (e.g. "10:00 AM - 12:00 PM").</summary>
        public string Time { get; set; } = string.Empty;

        /// <summary>Display duration (e.g. "2h" or "45m").</summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>Total booking price.</summary>
        public decimal Amount { get; set; }

        /// <summary>Reservation status string: "Upcoming", "Active", "Completed", or "Cancelled".</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Raw UTC arrival timestamp.</summary>
        public DateTime ArrivalTime { get; set; }

        /// <summary>Raw UTC departure timestamp.</summary>
        public DateTime DepartureTime { get; set; }
    }
}
