namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// A single data point on the revenue chart (one bar/line segment).
    /// </summary>
    public class RevenueDataPointDTO
    {
        /// <summary>Display label for the x-axis (e.g. "Mon", "Jan", "Week 1").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Revenue amount for this data point.</summary>
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Revenue overview data for the dashboard chart, supporting daily, weekly, and monthly periods.
    /// </summary>
    public class RevenueOverviewDTO
    {
        /// <summary>The aggregation period: "daily", "weekly", or "monthly".</summary>
        public string Period { get; set; } = string.Empty;

        /// <summary>Total revenue for the entire displayed period.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Individual data points that make up the chart series.</summary>
        public List<RevenueDataPointDTO> DataPoints { get; set; } = new List<RevenueDataPointDTO>();
    }
}
