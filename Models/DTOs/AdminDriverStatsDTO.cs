namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Aggregated driver counts for the Users Management header cards.
    /// </summary>
    public class AdminDriverStatsDTO
    {
        /// <summary>Total number of registered drivers on the platform.</summary>
        public int TotalDrivers { get; set; }

        /// <summary>Drivers with verified email and not suspended.</summary>
        public int Active { get; set; }

        /// <summary>Drivers whose email is pending verification.</summary>
        public int Pending { get; set; }

        /// <summary>Drivers whose account is currently suspended (locked out).</summary>
        public int Suspended { get; set; }
    }
}
