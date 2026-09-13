namespace Parkly_Backend.Models.DTOs
{
    /// <summary>Represents operating hours for a specific day range.</summary>
    public class OperatingHoursDTO
    {
        /// <summary>The day range, e.g. "Mon – Fri", "Sat – Sun".</summary>
        public string Days { get; set; } = string.Empty;

        /// <summary>The hours range, e.g. "6:00 AM – 11:00 PM".</summary>
        public string Hours { get; set; } = string.Empty;
    }
}
