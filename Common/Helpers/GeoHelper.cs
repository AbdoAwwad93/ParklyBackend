using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Parkly_Backend.Models.DTOs;

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
        /// Retained for backward compatibility.
        /// </summary>
        public static (TimeOnly? Open, TimeOnly? Close) ParseOperatingHours(string? operatingHours)
        {
            if (string.IsNullOrWhiteSpace(operatingHours))
            {
                return (null, null);
            }

            return ParseTimeRange(operatingHours);
        }

        /// <summary>
        /// Formats a list of OperatingHoursDTO into a stored string representation.
        /// </summary>
        public static string? FormatOperatingHoursFromDTOs(IEnumerable<OperatingHoursDTO>? dtos)
        {
            if (dtos == null)
            {
                return null;
            }

            var valid = dtos
                .Where(d => !string.IsNullOrWhiteSpace(d.Days) && !string.IsNullOrWhiteSpace(d.Hours))
                .Select(d => $"{NormalizeDayRange(d.Days)}: {d.Hours.Trim()}");

            var result = string.Join("; ", valid);
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        /// <summary>
        /// Converts raw operating hours string (delimited days/hours or simple range) into structured OperatingHoursDTO list.
        /// </summary>
        public static List<OperatingHoursDTO> ToOperatingHoursDTOs(string? operatingHours)
        {
            if (string.IsNullOrWhiteSpace(operatingHours))
            {
                return new List<OperatingHoursDTO>();
            }

            try
            {
                // Handle JSON array if stored as JSON
                var trimmed = operatingHours.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var items = JsonSerializer.Deserialize<List<OperatingHoursDTO>>(trimmed, options);
                        if (items != null && items.Count > 0)
                        {
                            return items;
                        }
                    }
                    catch
                    {
                        // Fall back to line/delimiter parsing
                    }
                }

                // Split by semicolon, newline, pipe, or comma followed by day specifier
                var lines = Regex.Split(trimmed, @"[;\n|]|\r\n|,\s*(?=[A-Za-z]+[\s–\-]+[A-Za-z]+:)");
                var results = new List<OperatingHoursDTO>();

                foreach (var line in lines)
                {
                    var entry = line.Trim();
                    if (string.IsNullOrWhiteSpace(entry))
                    {
                        continue;
                    }

                    var colonMatch = Regex.Match(entry, @"^([A-Za-z\s–—\-]+?)\s*:\s*(.*)$");
                    if (colonMatch.Success)
                    {
                        var daysPart = NormalizeDayRange(colonMatch.Groups[1].Value);
                        var hoursPart = colonMatch.Groups[2].Value.Trim();

                        var (open, close) = ParseTimeRange(hoursPart);
                        string formattedHours = (open.HasValue && close.HasValue)
                            ? FormatTimeRange(open.Value, close.Value)
                            : hoursPart;

                        results.Add(new OperatingHoursDTO
                        {
                            Days = daysPart,
                            Hours = formattedHours
                        });
                    }
                    else
                    {
                        var dayPatternMatch = Regex.Match(entry, @"^((?:Mon(?:day)?|Tue(?:sday)?|Wed(?:nesday)?|Thu(?:rsday)?|Fri(?:day)?|Sat(?:urday)?|Sun(?:day)?|Daily|Everyday|All Days)(?:\s*[–—\-]\s*(?:Mon(?:day)?|Tue(?:sday)?|Wed(?:nesday)?|Thu(?:rsday)?|Fri(?:day)?|Sat(?:urday)?|Sun(?:day)?))?)\s+(.*)$", RegexOptions.IgnoreCase);
                        if (dayPatternMatch.Success)
                        {
                            var daysPart = NormalizeDayRange(dayPatternMatch.Groups[1].Value);
                            var hoursPart = dayPatternMatch.Groups[2].Value.Trim();

                            var (open, close) = ParseTimeRange(hoursPart);
                            string formattedHours = (open.HasValue && close.HasValue)
                                ? FormatTimeRange(open.Value, close.Value)
                                : hoursPart;

                            results.Add(new OperatingHoursDTO
                            {
                                Days = daysPart,
                                Hours = formattedHours
                            });
                        }
                        else
                        {
                            var (open, close) = ParseTimeRange(entry);
                            if (open.HasValue && close.HasValue)
                            {
                                results.Add(new OperatingHoursDTO
                                {
                                    Days = "Mon – Sun",
                                    Hours = FormatTimeRange(open.Value, close.Value)
                                });
                            }
                            else
                            {
                                results.Add(new OperatingHoursDTO
                                {
                                    Days = "Mon – Sun",
                                    Hours = entry
                                });
                            }
                        }
                    }
                }

                return results.Count > 0 ? results : new List<OperatingHoursDTO>
                {
                    new OperatingHoursDTO { Days = "Mon – Sun", Hours = operatingHours }
                };
            }
            catch
            {
                return new List<OperatingHoursDTO>
                {
                    new OperatingHoursDTO { Days = "Mon – Sun", Hours = operatingHours ?? "24/7" }
                };
            }
        }

        public static string NormalizeDayRange(string days)
        {
            var trimmed = days.Trim();
            var separators = new[] { " – ", " — ", " - ", "–", "—", "-" };
            foreach (var sep in separators)
            {
                if (trimmed.Contains(sep))
                {
                    var parts = trimmed.Split(sep, 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        return $"{parts[0]} – {parts[1]}";
                    }
                }
            }
            return trimmed;
        }

        public static (TimeOnly? Open, TimeOnly? Close) ParseTimeRange(string timeRangeStr)
        {
            if (string.IsNullOrWhiteSpace(timeRangeStr))
            {
                return (null, null);
            }

            var separators = new[] { " – ", " — ", " - ", "–", "—", "-", " to " };
            string? sep = separators.FirstOrDefault(s => timeRangeStr.Contains(s));
            if (sep == null)
            {
                return (null, null);
            }

            var parts = timeRangeStr.Split(sep, 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return (null, null);
            }

            try
            {
                if (TryParseTime(parts[0], out var open) && TryParseTime(parts[1], out var close))
                {
                    return (open, close);
                }
            }
            catch
            {
                return (null, null);
            }

            return (null, null);
        }

        public static bool TryParseTime(string str, out TimeOnly time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            str = str.Trim();

            // Safe formats: no single-character custom specifiers like "H" without "%"
            string[] formats = {
                "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt", "h:mm t", "hh:mm t",
                "h tt", "hh tt", "htt", "hhtt", "h t", "hh t",
                "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
                "h:mm:ss tt", "hh:mm:ss tt",
                "%H", "HH", "%h", "hh",
                "t", "T"
            };

            try
            {
                if (TimeOnly.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                if (TimeOnly.TryParse(str, CultureInfo.InvariantCulture, out time))
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                if (TimeOnly.TryParse(str, out time))
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    time = TimeOnly.FromDateTime(dt);
                    return true;
                }
            }
            catch
            {
            }

            // Fallback for simple integer hour or hour with am/pm (e.g. "6", "18", "6am", "11pm")
            try
            {
                var clean = str.Replace(" ", "").ToLowerInvariant();
                bool isPm = clean.EndsWith("pm");
                bool isAm = clean.EndsWith("am");
                if (isPm || isAm)
                {
                    var numPart = clean.Substring(0, clean.Length - 2);
                    if (int.TryParse(numPart, out int h))
                    {
                        if (isPm && h < 12) h += 12;
                        if (isAm && h == 12) h = 0;
                        if (h >= 0 && h <= 23)
                        {
                            time = new TimeOnly(h, 0);
                            return true;
                        }
                    }
                }
                else if (int.TryParse(clean, out int h))
                {
                    if (h >= 0 && h <= 23)
                    {
                        time = new TimeOnly(h, 0);
                        return true;
                    }
                    if (h == 24)
                    {
                        time = new TimeOnly(23, 59, 59);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static string FormatTimeRange(TimeOnly open, TimeOnly close)
        {
            string openStr = open.ToString("h:mm tt", CultureInfo.InvariantCulture);
            string closeStr = close.ToString("h:mm tt", CultureInfo.InvariantCulture);
            return $"{openStr} – {closeStr}";
        }

        public static HashSet<DayOfWeek> ParseDayRange(string dayRangeStr)
        {
            var days = new HashSet<DayOfWeek>();
            dayRangeStr = dayRangeStr.Trim();

            if (dayRangeStr.Equals("Daily", StringComparison.OrdinalIgnoreCase) ||
                dayRangeStr.Equals("Everyday", StringComparison.OrdinalIgnoreCase) ||
                dayRangeStr.Equals("All Days", StringComparison.OrdinalIgnoreCase))
            {
                foreach (DayOfWeek d in Enum.GetValues<DayOfWeek>())
                {
                    days.Add(d);
                }
                return days;
            }

            var separators = new[] { " – ", " — ", " - ", "–", "—", "-", " to " };
            string? matchedSep = separators.FirstOrDefault(s => dayRangeStr.Contains(s));

            if (matchedSep != null)
            {
                var parts = dayRangeStr.Split(matchedSep, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && TryParseDay(parts[0], out var startDay) && TryParseDay(parts[1], out var endDay))
                {
                    var cur = startDay;
                    while (true)
                    {
                        days.Add(cur);
                        if (cur == endDay)
                        {
                            break;
                        }
                        cur = (DayOfWeek)(((int)cur + 1) % 7);
                    }
                    return days;
                }
            }

            var commaParts = dayRangeStr.Split(new[] { ',', '/' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in commaParts)
            {
                if (TryParseDay(p, out var d))
                {
                    days.Add(d);
                }
            }

            return days;
        }

        private static bool TryParseDay(string str, out DayOfWeek day)
        {
            str = str.Trim().ToLowerInvariant();
            if (str.StartsWith("mon")) { day = DayOfWeek.Monday; return true; }
            if (str.StartsWith("tue")) { day = DayOfWeek.Tuesday; return true; }
            if (str.StartsWith("wed")) { day = DayOfWeek.Wednesday; return true; }
            if (str.StartsWith("thu")) { day = DayOfWeek.Thursday; return true; }
            if (str.StartsWith("fri")) { day = DayOfWeek.Friday; return true; }
            if (str.StartsWith("sat")) { day = DayOfWeek.Saturday; return true; }
            if (str.StartsWith("sun")) { day = DayOfWeek.Sunday; return true; }
            day = DayOfWeek.Sunday;
            return false;
        }

        /// <summary>
        /// Determines whether the facility is open at a specific instant.
        /// If operating hours are unspecified, it is considered open 24/7.
        /// </summary>
        public static bool IsOpenAt(string? operatingHours, DateTime dateTime)
        {
            if (string.IsNullOrWhiteSpace(operatingHours))
            {
                return true;
            }

            var dtos = ToOperatingHoursDTOs(operatingHours);
            if (dtos.Count == 0)
            {
                return true;
            }

            var currentDay = dateTime.DayOfWeek;
            var currentTime = TimeOnly.FromTimeSpan(dateTime.TimeOfDay);

            foreach (var dto in dtos)
            {
                var days = ParseDayRange(dto.Days);
                if (days.Count == 0 || days.Contains(currentDay))
                {
                    var (open, close) = ParseTimeRange(dto.Hours);
                    if (open == null || close == null)
                    {
                        return true;
                    }

                    if (open.Value <= close.Value)
                    {
                        if (currentTime >= open.Value && currentTime <= close.Value)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Overnight schedule, e.g. 20:00 - 06:00
                        if (currentTime >= open.Value || currentTime <= close.Value)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the entire booking window [arrival, departure] falls within operating hours.
        /// </summary>
        public static bool IsWindowWithinOperatingHours(string? operatingHours, DateTime arrival, DateTime departure)
        {
            if (string.IsNullOrWhiteSpace(operatingHours))
            {
                return true;
            }

            var dtos = ToOperatingHoursDTOs(operatingHours);
            if (dtos.Count == 0)
            {
                return true;
            }

            var arrDay = arrival.DayOfWeek;
            var depDay = departure.DayOfWeek;
            var arrTime = TimeOnly.FromTimeSpan(arrival.TimeOfDay);
            var depTime = TimeOnly.FromTimeSpan(departure.TimeOfDay);

            foreach (var dto in dtos)
            {
                var days = ParseDayRange(dto.Days);
                if (days.Count == 0 || (days.Contains(arrDay) && days.Contains(depDay)))
                {
                    var (open, close) = ParseTimeRange(dto.Hours);
                    if (open == null || close == null)
                    {
                        return true;
                    }

                    if (open.Value <= close.Value)
                    {
                        if (arrTime >= open.Value && depTime <= close.Value)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        var arrivalValid = arrTime >= open.Value || arrTime <= close.Value;
                        var departureValid = depTime >= open.Value || depTime <= close.Value;
                        if (arrivalValid && departureValid)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
