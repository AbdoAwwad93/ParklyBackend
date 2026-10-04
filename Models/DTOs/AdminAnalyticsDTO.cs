using System;
using System.Collections.Generic;

namespace Parkly_Backend.Models.DTOs
{
    /// <summary>
    /// Consolidated platform-wide metrics and breakdowns for the Reports & Analytics dashboard.
    /// </summary>
    public class AdminAnalyticsDTO
    {
        public string Period { get; set; } = "last_6_months";
        public AdminAnalyticsSummaryDTO Summary { get; set; } = new();
        public List<AdminTrendDataPointDTO> Trend { get; set; } = new();
        public List<AdminOwnerRevenueShareDTO> RevenueByOwner { get; set; } = new();
        public List<AdminOwnerBookingsShareDTO> BookingsByOwner { get; set; } = new();
    }

    /// <summary>
    /// Top 4 summary metric cards for Reports & Analytics.
    /// </summary>
    public class AdminAnalyticsSummaryDTO
    {
        /// <summary>Total revenue generated on the platform in the selected period.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Total bookings placed on the platform in the selected period.</summary>
        public int TotalBookings { get; set; }

        /// <summary>Average occupancy rate across active spaces during the period (percentage).</summary>
        public double AvgOccupancy { get; set; }

        /// <summary>Total number of active parking locations across the network.</summary>
        public int ActiveLocations { get; set; }
    }

    /// <summary>
    /// Data point for the dual-series Revenue & Bookings Trend chart.
    /// </summary>
    public class AdminTrendDataPointDTO
    {
        /// <summary>Label for chart x-axis (e.g. "Nov", "Dec", "Mon", "Week 1").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Date or timestamp of the bucket.</summary>
        public DateTime Date { get; set; }

        /// <summary>Total bookings count in this interval.</summary>
        public int Bookings { get; set; }

        /// <summary>Total revenue in this interval.</summary>
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Performance item for the Revenue by Owner widget.
    /// </summary>
    public class AdminOwnerRevenueShareDTO
    {
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int LocationsCount { get; set; }
        public decimal Revenue { get; set; }
        public double SharePercentage { get; set; }
    }

    /// <summary>
    /// Performance item for the Bookings by Owner chart.
    /// </summary>
    public class AdminOwnerBookingsShareDTO
    {
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int BookingsCount { get; set; }
        public double SharePercentage { get; set; }
    }
}
