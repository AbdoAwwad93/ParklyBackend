namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Aggregated reservation metrics for the 4 header cards and tab pill badges on the Admin Reservations page.
    /// </summary>
    public class AdminReservationStatsDTO
    {
        /// <summary>Total reservations placed on the platform.</summary>
        public int TotalReservations { get; set; }

        /// <summary>Number of reservations currently active / checked-in.</summary>
        public int ActiveNow { get; set; }

        /// <summary>Number of upcoming confirmed reservations.</summary>
        public int Upcoming { get; set; }

        /// <summary>Total revenue processed from completed bookings.</summary>
        public decimal RevenueProcessed { get; set; }

        // Tab badge counters matching the UI pill filters
        /// <summary>All reservations count.</summary>
        public int AllCount { get; set; }

        /// <summary>Upcoming reservations count.</summary>
        public int UpcomingCount { get; set; }

        /// <summary>Active reservations count.</summary>
        public int ActiveCount { get; set; }

        /// <summary>Completed reservations count.</summary>
        public int CompletedCount { get; set; }

        /// <summary>Cancelled reservations count.</summary>
        public int CancelledCount { get; set; }
    }
}
