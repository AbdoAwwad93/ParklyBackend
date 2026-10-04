using System;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Detailed information about a reservation for admin inspection.
    /// </summary>
    public class AdminReservationDetailDTO
    {
        public Guid ReservationId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string? QrCode { get; set; }

        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public string DateFormatted { get; set; } = string.Empty;
        public string TimeWindowFormatted { get; set; } = string.Empty;
        public string DurationFormatted { get; set; } = string.Empty;

        // Customer details
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerInitials { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Parking details
        public Guid ParkingId { get; set; }
        public string ParkingName { get; set; } = string.Empty;
        public string ParkingAddress { get; set; } = string.Empty;
        public string OwnerBusinessName { get; set; } = string.Empty;

        // Space details
        public Guid SpaceId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public string? Level { get; set; }
        public string SpaceType { get; set; } = string.Empty;
        public string? VehicleSize { get; set; }

        // Review (if any)
        public int? ReviewRating { get; set; }
        public string? ReviewComment { get; set; }
        public DateTime? ReviewDate { get; set; }
    }
}
