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
    public class SavedPlacesRepository : GenericRepository<SavedPlace>, ISavedPlacesRepository
    {
        public SavedPlacesRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<SavedPlace>> GetUserSavedPlacesOrderedAsync(Guid userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.PlaceType == PlaceType.Home ? 0 : s.PlaceType == PlaceType.Work ? 1 : 2)
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}
