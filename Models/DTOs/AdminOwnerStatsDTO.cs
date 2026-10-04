namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Aggregated owner metrics for the Parking Owners page header cards.
    /// </summary>
    public class AdminOwnerStatsDTO
    {
        /// <summary>Total number of registered parking owners.</summary>
        public int TotalOwners { get; set; }

        /// <summary>Number of verified / active owners.</summary>
        public int Active { get; set; }

        /// <summary>Number of owners awaiting business verification.</summary>
        public int Pending { get; set; }

        /// <summary>Total platform revenue generated across all parking owners.</summary>
        public decimal TotalRevenue { get; set; }
    }
}
