namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Details of a parking facility returned by the API.</summary>
    public class ParkingResponseDTO
    {
        /// <summary>The unique id of the parking facility.</summary>
        public Guid ParkingId { get; set; }

        /// <summary>The id of the parking owner who owns the facility.</summary>
        public Guid OwnerId { get; set; }

        /// <summary>The name of the parking facility.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The street address of the parking facility.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>The latitude coordinate of the parking facility.</summary>
        public decimal Latitude { get; set; }

        /// <summary>The longitude coordinate of the parking facility.</summary>
        public decimal Longitude { get; set; }

        /// <summary>The operating hours of the parking facility.</summary>
        public string? OperatingHours { get; set; }
    }
}