namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Detailed profile information and history for a single driver.
    /// </summary>
    public class AdminDriverDetailDTO
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? CityState { get; set; }
        public string? Bio { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ReservationsCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastActive { get; set; }
        public List<AdminDriverReservationDTO> RecentReservations { get; set; } = new();
    }

    /// <summary>
    /// Summary of a single reservation in the driver's detail view.
    /// </summary>
    public class AdminDriverReservationDTO
    {
        public Guid ReservationId { get; set; }
        public string ParkingName { get; set; } = string.Empty;
        public string SpotNumber { get; set; } = string.Empty;
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
