namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Enriched reservation row for the "Today's Reservations" table on the owner dashboard.
    /// </summary>
    public class OwnerReservationDTO
    {
        /// <summary>Unique identifier of the reservation.</summary>
        public Guid ReservationId { get; set; }

        /// <summary>Human-readable booking reference (e.g. "PK-4201").</summary>
        public string BookingReference { get; set; } = string.Empty;

        /// <summary>Full name of the customer who made the reservation.</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Up to two initials derived from the customer's name (e.g. "SM").</summary>
        public string CustomerInitials { get; set; } = string.Empty;

        /// <summary>Name of the parking location for this reservation.</summary>
        public string ParkingName { get; set; } = string.Empty;

        /// <summary>Parking space spot number (e.g. "S2").</summary>
        public string SpotNumber { get; set; } = string.Empty;

        /// <summary>Scheduled arrival time (UTC).</summary>
        public DateTime ArrivalTime { get; set; }

        /// <summary>Scheduled departure time (UTC).</summary>
        public DateTime DepartureTime { get; set; }

        /// <summary>Duration in hours (e.g. 2.0, 1.5).</summary>
        public double DurationHours { get; set; }

        /// <summary>Current reservation status (e.g. "Confirmed", "CheckedIn").</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Total price charged for this reservation.</summary>
        public decimal TotalPrice { get; set; }
    }
}
