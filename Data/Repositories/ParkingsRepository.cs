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
    public class ParkingsRepository : GenericRepository<Parking>, IParkingsRepository
    {
        public ParkingsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Parking>> GetParkingsWithSpacesAsync()
        {
            return await _dbSet
                .Include(p => p.ParkingSpaces)
                .ToListAsync();
        }

        public async Task<List<Parking>> GetCandidateParkingsInBoundingBoxAsync(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng, string? vehicleSize = null, decimal? maxRate = null)
        {
            var query = _dbSet
                .Include(p => p.ParkingSpaces)
                .Where(p => p.Latitude >= minLat && p.Latitude <= maxLat &&
                            p.Longitude >= minLng && p.Longitude <= maxLng);

            if (!string.IsNullOrEmpty(vehicleSize))
            {
                query = query.Where(p => p.ParkingSpaces.Any(s => s.IsActive && s.VehicleSize.ToString() == vehicleSize));
            }

            if (maxRate.HasValue)
            {
                query = query.Where(p => p.ParkingSpaces.Any(s => s.IsActive && s.BaseHourlyRate <= maxRate.Value));
            }

            return await query.ToListAsync();
        }
    }
}
