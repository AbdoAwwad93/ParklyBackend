using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Services
{
    public class PricingService : IPricingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PricingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<decimal> CalculateTotalPriceAsync(Guid spaceId, DateTime arrival, DateTime departure)
        {
            if (arrival >= departure)
            {
                throw new ArgumentException("Departure time must be after arrival time.");
            }

            var space = await GetSpaceWithRulesAsync(spaceId);

            if (RulesOverlapBlackout(space.Parking.PricingRules, arrival, departure))
            {
                throw new InvalidOperationException("The requested time window overlaps a blackout period.");
            }

            decimal total = 0;
            var current = arrival;
            while (current < departure)
            {
                var sliceEnd = current.AddHours(1) < departure ? current.AddHours(1) : departure;
                var hours = (decimal)(sliceEnd - current).TotalHours;

                var activeModifiers = space.Parking.PricingRules
                    .Where(r => r.RuleType != PricingRuleType.Blackout &&
                                r.StartTime <= current && current < r.EndTime)
                    .Sum(r => r.PriceModifier);

                var rate = space.BaseHourlyRate * (1 + activeModifiers / 100);
                total += rate * hours;
                current = sliceEnd;
            }

            return Math.Round(total, 2);
        }

        public async Task<bool> IsSpaceAvailableAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId = null)
        {
            if (arrival >= departure)
            {
                return false;
            }

            var space = await GetSpaceWithRulesAsync(spaceId);

            if (!space.IsActive || RulesOverlapBlackout(space.Parking.PricingRules, arrival, departure))
            {
                return false;
            }

            var hasOverlap = await _unitOfWork.Repository<Reservation>()
                .AnyAsync(r =>
                    r.SpaceId == spaceId &&
                    r.Status != ReservationStatus.Cancelled &&
                    r.ArrivalTime < departure &&
                    r.DepartureTime > arrival &&
                    (excludeReservationId == null || r.ReservationId != excludeReservationId));

            return !hasOverlap;
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
    }
}