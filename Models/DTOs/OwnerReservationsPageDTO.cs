using System;
using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    public class OwnerReservationsPageDTO
    {
        public OwnerReservationsSummaryDTO Summary { get; set; } = new();
        public List<OwnerReservationListItemDTO> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class OwnerReservationsSummaryDTO
    {
        public int TotalBookings { get; set; }
        public int Upcoming { get; set; }
        public int ActiveNow { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }

    public class OwnerReservationListItemDTO
    {
        public Guid ReservationId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerInitials { get; set; } = string.Empty;
        public Guid ParkingId { get; set; }
        public string ParkingName { get; set; } = string.Empty;
        public string ParkingAddress { get; set; } = string.Empty;
        public Guid SpaceId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public double DurationHours { get; set; }
        public string DurationFormatted { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
