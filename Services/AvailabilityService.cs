using Parkly_Backend.Common.Helpers;
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
                || space.Status != SpaceStatus.Available
                || RulesOverlapBlackout(space.Parking.PricingRules, arrival, departure)
                || OutsideOperatingHours(space.Parking.OperatingHours, arrival, departure))
            {
                return false;
            }

            return !await HasOverlappingReservationAsync(spaceId, arrival, departure, excludeReservationId);
        }

        public async Task<List<ParkingSpace>> GetAvailableSpacesAsync(Guid parkingId, DateTime arrival, DateTime departure)
        {
            var dict = await GetAvailableSpacesForParkingsAsync(new[] { parkingId }, arrival, departure);
            return dict.GetValueOrDefault(parkingId) ?? new List<ParkingSpace>();
        }

        public async Task<Dictionary<Guid, List<ParkingSpace>>> GetAvailableSpacesForParkingsAsync(IEnumerable<Guid> parkingIds, DateTime arrival, DateTime departure)
        {
            if (arrival >= departure)
            {
                throw new ArgumentException("Departure time must be after arrival time.");
            }

            var idList = parkingIds.Distinct().ToList();
            var result = new Dictionary<Guid, List<ParkingSpace>>();
            foreach (var id in idList)
            {
                result[id] = new List<ParkingSpace>();
            }

            if (idList.Count == 0)
            {
                return result;
            }

            var spaces = await _unitOfWork.ParkingSpaces.GetActiveSpacesWithRulesForParkingsAsync(idList);

            if (spaces.Count == 0)
            {
                return result;
            }

            var spaceIds = spaces.Select(s => s.SpaceId).ToList();
            var reservedSpaces = await _unitOfWork.Reservations.GetOverlappingReservationsForSpacesAsync(spaceIds, arrival, departure);
            var reservedSpaceIds = reservedSpaces.Select(r => r.SpaceId).Distinct().ToList();

            foreach (var space in spaces)
            {
                if (space.Status != SpaceStatus.Available)
                {
                    continue;
                }

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

                if (result.TryGetValue(space.ParkingId, out var list))
                {
                    list.Add(space);
                }
            }

            return result;
        }

        private async Task<bool> HasOverlappingReservationAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId)
        {
            var overlapping = await _unitOfWork.Reservations.GetOverlappingReservationsAsync(spaceId, arrival, departure, excludeReservationId);
            return overlapping.Count > 0;
        }

        private async Task<ParkingSpace> GetSpaceWithRulesAsync(Guid spaceId)
        {
            var space = await _unitOfWork.ParkingSpaces.GetByIdWithParkingAsync(spaceId);

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
            return !GeoHelper.IsWindowWithinOperatingHours(operatingHours, arrival, departure);
        }
    }
}