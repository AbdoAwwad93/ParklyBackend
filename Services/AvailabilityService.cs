using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;
using System.Globalization;

namespace Parkly_Backend.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AvailabilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> IsSpaceAvailableAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId = null)
        {
            if (arrival >= departure)
            {
                return false;
            }

            var space = await GetSpaceWithRulesAsync(spaceId);

            if (!space.IsActive
                || RulesOverlapBlackout(space.Parking.PricingRules, arrival, departure)
                || OutsideOperatingHours(space.Parking.OperatingHours, arrival, departure))
            {
                return false;
            }

            return !await HasOverlappingReservationAsync(spaceId, arrival, departure, excludeReservationId);
        }

        public async Task<List<ParkingSpace>> GetAvailableSpacesAsync(Guid parkingId, DateTime arrival, DateTime departure)
        {
            if (arrival >= departure)
            {
                throw new ArgumentException("Departure time must be after arrival time.");
            }

            var spaces = await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                    .ThenInclude(p => p.PricingRules)
                .Where(s => s.ParkingId == parkingId && s.IsActive)
                .ToListAsync();

            if (spaces.Count == 0)
            {
                return new List<ParkingSpace>();
            }

            var spaceIds = spaces.Select(s => s.SpaceId).ToList();
            var reservedSpaceIds = await _unitOfWork.Repository<Reservation>().Query()
                .Where(r => spaceIds.Contains(r.SpaceId) &&
                            r.Status != ReservationStatus.Cancelled &&
                            r.ArrivalTime < departure &&
                            r.DepartureTime > arrival)
                .Select(r => r.SpaceId)
                .Distinct()
                .ToListAsync();

            var available = new List<ParkingSpace>();
            foreach (var space in spaces)
            {
                if (RulesOverlapBlackout(space.Parking.PricingRules, arrival, departure))
                {
                    continue;
                }

                if (OutsideOperatingHours(space.Parking.OperatingHours, arrival, departure))
                {
                    continue;
                }

                if (reservedSpaceIds.Contains(space.SpaceId))
                {
                    continue;
                }

                available.Add(space);
            }

            return available;
        }

        private async Task<bool> HasOverlappingReservationAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId)
        {
            return await _unitOfWork.Repository<Reservation>()
                .AnyAsync(r =>
                    r.SpaceId == spaceId &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.ArrivalTime < departure &&
                    r.DepartureTime > arrival &&
                    (excludeReservationId == null || r.ReservationId != excludeReservationId));
        }

        private async Task<ParkingSpace> GetSpaceWithRulesAsync(Guid spaceId)
        {
            var space = await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                    .ThenInclude(p => p.PricingRules)
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId);

            return space ?? throw new KeyNotFoundException("Parking space not found.");
        }

        private static bool RulesOverlapBlackout(List<PricingRule> rules, DateTime arrival, DateTime departure)
        {
            return rules.Any(r =>
                r.RuleType == PricingRuleType.Blackout &&
                r.StartTime < departure &&
                r.EndTime > arrival);
        }

        private static bool OutsideOperatingHours(string? operatingHours, DateTime arrival, DateTime departure)
        {
            var (open, close) = ParseOperatingHours(operatingHours);
            if (open == null || close == null)
            {
                return false;
            }

            return arrival.TimeOfDay < open.Value.ToTimeSpan()
                || departure.TimeOfDay > close.Value.ToTimeSpan();
        }

        private static (TimeOnly? Open, TimeOnly? Close) ParseOperatingHours(string? operatingHours)
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
    }
}