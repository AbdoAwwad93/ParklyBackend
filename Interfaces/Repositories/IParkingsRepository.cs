using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IParkingsRepository : IGenericRepository<Parking>
    {
        Task<List<Parking>> GetParkingsWithSpacesAsync();
        Task<List<Parking>> GetCandidateParkingsInBoundingBoxAsync(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng, string? vehicleSize = null, decimal? maxRate = null);
    }
}
