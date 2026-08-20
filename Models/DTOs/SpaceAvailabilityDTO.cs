namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Indicates whether a parking space is available for a requested time window.</summary>
    public class SpaceAvailabilityDTO
    {
        /// <summary>The id of the parking space.</summary>
        public Guid SpaceId { get; set; }

        /// <summary>Whether the space is available for the requested window.</summary>
        public bool IsAvailable { get; set; }
    }
}