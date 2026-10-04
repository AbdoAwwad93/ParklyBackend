namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Request body for cancelling a reservation by an administrator.
    /// </summary>
    public class AdminCancelReservationDTO
    {
        /// <summary>Optional administrative reason for cancellation.</summary>
        public string? Reason { get; set; }
    }
}
