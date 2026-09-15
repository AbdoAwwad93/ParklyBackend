using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class PricingService : IPricingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public PricingService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
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

            var managedRate = await _unitOfWork.SpaceTypePricings
                .GetByParkingAndTypeAsync(space.ParkingId, space.SpaceType);
            if (managedRate == null)
            {
                return Math.Round(total, 2);
            }

            // Apply daily/weekly offers while preserving any time-based markup or discount already calculated above.
            var durationHours = (decimal)(departure - arrival).TotalHours;
            var regularHourlyTotal = space.BaseHourlyRate * durationHours;
            var tieredTotal = CalculateTieredTotal(durationHours, managedRate);
            return regularHourlyTotal > 0
                ? Math.Round(total * tieredTotal / regularHourlyTotal, 2)
                : Math.Round(tieredTotal, 2);
        }

        public async Task<ApiResponse<List<PricingLocationDTO>>> GetOwnerLocationsAsync(Guid ownerId)
        {
            var parkings = await _unitOfWork.Parkings.GetByOwnerIdWithSpacesAsync(ownerId);
            return ApiResponse<List<PricingLocationDTO>>.Success("Pricing locations retrieved successfully.",
                parkings.Select(p => new PricingLocationDTO { ParkingId = p.ParkingId, Name = p.Name }).ToList());
        }

        public async Task<ApiResponse<ParkingPricingDTO>> GetParkingPricingAsync(Guid ownerId, Guid parkingId)
        {
            var parking = await _unitOfWork.Parkings.GetByIdWithSpacesAsync(parkingId);
            if (parking == null || parking.OwnerId != ownerId)
                return ApiResponse<ParkingPricingDTO>.Failure("Parking not found or you do not have permission.");

            var configuredRates = await _unitOfWork.SpaceTypePricings.GetByParkingIdAsync(parkingId);
            var rates = Enum.GetValues<SpaceType>().Select(type =>
            {
                var configured = configuredRates.FirstOrDefault(rate => rate.SpaceType == type);
                var defaultHourly = parking.ParkingSpaces.Where(space => space.SpaceType == type)
                    .Select(space => (decimal?)space.BaseHourlyRate).FirstOrDefault() ?? 0m;
                return new SpaceTypePricingDTO
                {
                    PricingId = configured?.SpaceTypePricingId,
                    SpaceType = type,
                    HourlyRate = configured?.HourlyRate ?? defaultHourly,
                    DailyRate = configured?.DailyRate ?? defaultHourly * 24m,
                    WeeklyRate = configured?.WeeklyRate ?? defaultHourly * 24m * 7m,
                    IsConfigured = configured != null,
                    UpdatedAt = configured?.UpdatedAt
                };
            }).ToList();

            return ApiResponse<ParkingPricingDTO>.Success("Parking pricing retrieved successfully.", new ParkingPricingDTO
            {
                ParkingId = parking.ParkingId,
                ParkingName = parking.Name,
                Rates = rates
            });
        }

        public async Task<ApiResponse<SpaceTypePricingDTO>> UpsertSpaceTypePricingAsync(Guid ownerId, Guid parkingId, UpsertSpaceTypePricingDTO dto)
        {
            var parking = await _unitOfWork.Parkings.GetByIdWithSpacesAsync(parkingId);
            if (parking == null || parking.OwnerId != ownerId)
                return ApiResponse<SpaceTypePricingDTO>.Failure("Parking not found or you do not have permission.");

            var rate = await _unitOfWork.SpaceTypePricings.GetByParkingAndTypeAsync(parkingId, dto.SpaceType);
            if (rate == null)
            {
                rate = new SpaceTypePricing { ParkingId = parkingId, SpaceType = dto.SpaceType };
                await _unitOfWork.SpaceTypePricings.AddAsync(rate);
            }
            rate.HourlyRate = dto.HourlyRate;
            rate.DailyRate = dto.DailyRate;
            rate.WeeklyRate = dto.WeeklyRate;
            rate.UpdatedAt = DateTime.UtcNow;

            // Keep the existing per-space reservation calculation in sync with the managed hourly rate.
            foreach (var space in parking.ParkingSpaces.Where(space => space.SpaceType == dto.SpaceType))
                space.BaseHourlyRate = dto.HourlyRate;

            await _unitOfWork.SaveChangesAsync();
            await _notificationService.CreateAsync(ownerId, NotificationType.Update,
                $"Pricing Updated — {parking.Name}",
                $"{dto.SpaceType} rates were updated: ${dto.HourlyRate:F2}/hour, ${dto.DailyRate:F2}/day, ${dto.WeeklyRate:F2}/week.",
                parkingId);

            return ApiResponse<SpaceTypePricingDTO>.Success("Pricing updated successfully.", new SpaceTypePricingDTO
            {
                PricingId = rate.SpaceTypePricingId, SpaceType = rate.SpaceType, HourlyRate = rate.HourlyRate,
                DailyRate = rate.DailyRate, WeeklyRate = rate.WeeklyRate, IsConfigured = true, UpdatedAt = rate.UpdatedAt
            });
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

        private static decimal CalculateTieredTotal(decimal durationHours, SpaceTypePricing rate)
        {
            var weeks = decimal.Floor(durationHours / 168m);
            var remainder = durationHours - weeks * 168m;
            var days = decimal.Floor(remainder / 24m);
            var hours = remainder - days * 24m;
            return weeks * rate.WeeklyRate + days * rate.DailyRate + hours * rate.HourlyRate;
        }
    }
}
