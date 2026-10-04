namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// A single data point on the bookings chart (one bar segment).
    /// </summary>
    public class BookingsDataPointDTO
    {
        /// <summary>Display label for the x-axis (e.g. "Week 1", "Mon").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Number of bookings for this data point.</summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Bookings chart data for the admin dashboard, supporting weekly and daily periods.
    /// </summary>
    public class BookingsChartDTO
    {
        /// <summary>The aggregation period: "daily" or "weekly".</summary>
        public string Period { get; set; } = string.Empty;

        /// <summary>Total bookings across the entire displayed period.</summary>
        public int TotalBookings { get; set; }

        /// <summary>Individual data points that make up the chart series.</summary>
        public List<BookingsDataPointDTO> DataPoints { get; set; } = new List<BookingsDataPointDTO>();
    }
}
