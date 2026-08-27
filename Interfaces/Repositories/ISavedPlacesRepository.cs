using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface ISavedPlacesRepository : IGenericRepository<SavedPlace>
    {
        Task<List<SavedPlace>> GetUserSavedPlacesOrderedAsync(Guid userId);
    }
}
