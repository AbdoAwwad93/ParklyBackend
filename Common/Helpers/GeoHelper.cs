using System;
using System.Globalization;

namespace Parkly_Backend.Common.Helpers
{
    /// <summary>
    /// Utility methods for geospatial calculations and operating hours checks.
    /// </summary>
    public static class GeoHelper
    {
        public const double EarthRadiusKm = 6371.0;
        public const double KmPerLatitudeDegree = 111.0;

        /// <summary>
        /// Calculates the great-circle distance in kilometers between two geographic coordinates using the Haversine formula.
        /// </summary>
        public static double DistanceKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
            var dLng = (double)(lng2 - lng1) * Math.PI / 180.0;

            var sinLat = Math.Sin(dLat / 2);
            var sinLng = Math.Sin(dLng / 2);

            var cosLat1 = Math.Cos((double)lat1 * Math.PI / 180.0);
            var cosLat2 = Math.Cos((double)lat2 * Math.PI / 180.0);

            var a = (sinLat * sinLat) + (cosLat1 * cosLat2 * sinLng * sinLng);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return Math.Round(EarthRadiusKm * c, 2);
        }

        /// <summary>
        /// Computes a geographic bounding box [minLat, maxLat, minLng, maxLng] around a center coordinate
        /// for a given radius in kilometers.
        /// </summary>
        public static (decimal MinLat, decimal MaxLat, decimal MinLng, decimal MaxLng) GetBoundingBox(decimal lat, decimal lng, double radiusKm)
        {
            var deltaLat = (decimal)(radiusKm / KmPerLatitudeDegree);
            var minLat = Math.Max(-90m, lat - deltaLat);
            var maxLat = Math.Min(90m, lat + deltaLat);

            // Avoid division by zero near the poles
            var latRad = (double)lat * Math.PI / 180.0;
            var cosLat = Math.Cos(latRad);

            decimal deltaLng;
            if (Math.Abs(cosLat) < 0.01)
            {
                deltaLng = 180m;
            }
            else
            {
                deltaLng = (decimal)(radiusKm / (KmPerLatitudeDegree * cosLat));
            }

            var minLng = Math.Max(-180m, lng - deltaLng);
            var maxLng = Math.Min(180m, lng + deltaLng);

            return (minLat, maxLat, minLng, maxLng);
        }

        /// <summary>
        /// Parses operating hours formatted as "HH:mm - HH:mm" into opening and closing times.
        /// </summary>
        public static (TimeOnly? Open, TimeOnly? Close) ParseOperatingHours(string? operatingHours)
        {
            if (string.IsNullOrWhiteSpace(operatingHours))
            {
                return (null, null);
            }

            var parts = operatingHours.Split('-', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return (null, null);
            }

            if (!TimeOnly.TryParseExact(parts[0], "HH\\:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var open))
            {
                return (null, null);
            }

            if (!TimeOnly.TryParseExact(parts[1], "HH\\:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var close))
            {
                return (null, null);
            }

            return (open, close);
        }

        /// <summary>
        /// Determines whether the facility is open at a specific instant.
        /// If operating hours are unspecified, it is considered open 24/7.
        /// </summary>
        public static bool IsOpenAt(string? operatingHours, DateTime dateTime)
        {
            var (open, close) = ParseOperatingHours(operatingHours);
            if (open == null || close == null)
            {
                return true;
            }

            var time = TimeOnly.FromTimeSpan(dateTime.TimeOfDay);
            if (open.Value <= close.Value)
            {
                return time >= open.Value && time <= close.Value;
            }

            // Overnight schedule, e.g. 20:00 - 06:00
            return time >= open.Value || time <= close.Value;
        }

        /// <summary>
        /// Determines whether the entire booking window [arrival, departure] falls within operating hours.
        /// </summary>
        public static bool IsWindowWithinOperatingHours(string? operatingHours, DateTime arrival, DateTime departure)
        {
            var (open, close) = ParseOperatingHours(operatingHours);
            if (open == null || close == null)
            {
                return true;
            }

            var arrTime = TimeOnly.FromTimeSpan(arrival.TimeOfDay);
            var depTime = TimeOnly.FromTimeSpan(departure.TimeOfDay);

            if (open.Value <= close.Value)
            {
                return arrTime >= open.Value && depTime <= close.Value;
            }

            // Overnight schedule, e.g. 20:00 - 06:00
            var arrivalValid = arrTime >= open.Value || arrTime <= close.Value;
            var departureValid = depTime >= open.Value || depTime <= close.Value;

            return arrivalValid && departureValid;
        }
    }
}
