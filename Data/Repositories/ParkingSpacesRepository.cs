using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data.Repositories
{
    public class ParkingSpacesRepository : GenericRepository<ParkingSpace>, IParkingSpacesRepository
    {
        public ParkingSpacesRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ParkingSpace>> GetAllWithParkingAsync()
        {
            return await _dbSet
                .Include(s => s.Parking)
                .ToListAsync();
        }

        public async Task<List<ParkingSpace>> GetByParkingIdWithParkingAsync(Guid parkingId)
        {
            return await _dbSet
                .Include(s => s.Parking)
                .Where(s => s.ParkingId == parkingId)
                .ToListAsync();
        }

        public async Task<ParkingSpace?> GetByIdWithParkingAsync(Guid spaceId)
        {
            return await _dbSet
                .Include(s => s.Parking)
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId);
        }

        public async Task<ParkingSpace?> GetOwnerSpaceAsync(Guid ownerId, Guid spaceId)
        {
            return await _dbSet
                .Include(s => s.Parking)
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId && s.Parking.OwnerId == ownerId);
        }

        public async Task<List<ParkingSpace>> GetActiveSpacesWithRulesForParkingsAsync(IEnumerable<Guid> parkingIds)
        {
            return await _dbSet
                .Include(s => s.Parking)
                    .ThenInclude(p => p.PricingRules)
                .Where(s => parkingIds.Contains(s.ParkingId) && s.IsActive)
                .ToListAsync();
        }

        public async Task<List<ParkingSpace>> GetCandidateSpacesInBoundingBoxAsync(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng, string? vehicleSize = null, decimal? maxRate = null)
        {
            var query = _dbSet
                .Include(s => s.Parking)
                    .ThenInclude(p => p.PricingRules)
                .Where(s => s.IsActive &&
                            s.Parking.Latitude >= minLat && s.Parking.Latitude <= maxLat &&
                            s.Parking.Longitude >= minLng && s.Parking.Longitude <= maxLng);

            if (!string.IsNullOrEmpty(vehicleSize))
            {
                if (Enum.TryParse<VehicleSize>(vehicleSize, true, out var parsedSize))
                {
                    query = query.Where(s => s.VehicleSize == parsedSize);
                }
            }

            if (maxRate.HasValue)
            {
                query = query.Where(s => s.BaseHourlyRate <= maxRate.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> HasActiveReservationsAsync(Guid spaceId)
        {
            return await _context.Set<Reservation>()
                .AnyAsync(r => r.SpaceId == spaceId && r.Status != ReservationStatus.Cancelled);
        }
    }
}
