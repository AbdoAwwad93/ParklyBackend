using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for updating an existing reservation's times.</summary>
    public class UpdateReservationDTO
    {
        /// <summary>The new arrival time.</summary>
        [Required(ErrorMessage = "Arrival time is required.")]
        public DateTime ArrivalTime { get; set; }

        /// <summary>The new departure time.</summary>
        [Required(ErrorMessage = "Departure time is required.")]
        public DateTime DepartureTime { get; set; }
    }
}