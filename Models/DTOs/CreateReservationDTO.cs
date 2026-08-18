using System.ComponentModel.DataAnnotations;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Payload for creating a new parking reservation.</summary>
    public class CreateReservationDTO
    {
        /// <summary>The id of the parking space to reserve.</summary>
        [Required(ErrorMessage = "SpaceId is required.")]
        public Guid SpaceId { get; set; }

        /// <summary>The planned arrival time.</summary>
        [Required(ErrorMessage = "Arrival time is required.")]
        public DateTime ArrivalTime { get; set; }

        /// <summary>The planned departure time.</summary>
        [Required(ErrorMessage = "Departure time is required.")]
        public DateTime DepartureTime { get; set; }
    }
}