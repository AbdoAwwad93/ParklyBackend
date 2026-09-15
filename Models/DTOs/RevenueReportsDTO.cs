using System;
using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    public class RevenueReportsDTO
    {
        public string Period { get; set; } = "monthly";
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public RevenueReportSummaryDTO Summary { get; set; } = new();
        public List<RevenueReportDataPointDTO> RevenueOverTime { get; set; } = [];
        public List<BookingVolumeDataPointDTO> BookingVolume { get; set; } = [];
        public List<LocationRevenueDTO> RevenueByLocation { get; set; } = [];
    }

    public class RevenueReportSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalRevenueGrowthPercent { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalBookingsGrowthPercent { get; set; }
        public decimal AverageRevenuePerPeriod { get; set; }
        public decimal ThisMonthTotal { get; set; }
        public decimal ThisMonthGrowthPercent { get; set; }
        public string ThisMonthLabel { get; set; } = string.Empty;
    }

    public class RevenueReportDataPointDTO
    {
        public string Label { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BookingVolumeDataPointDTO
    {
        public string Label { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Bookings { get; set; }
    }

    public class LocationRevenueDTO
    {
        public Guid ParkingId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal SharePercent { get; set; }
        public int Bookings { get; set; }
        public decimal AveragePerBooking { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
