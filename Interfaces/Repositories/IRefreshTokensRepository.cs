using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IRefreshTokensRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
    }
}
