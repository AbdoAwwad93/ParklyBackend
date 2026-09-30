using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class UserFcmTokensRepository : GenericRepository<UserFcmToken>, IUserFcmTokensRepository
    {
        public UserFcmTokensRepository(AppDbContext context) : base(context) { }

        public async Task<List<string>> GetTokensByUserIdAsync(Guid userId)
        {
            return await _dbSet.AsNoTracking()
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .ToListAsync();
        }

        public async Task UpsertAsync(Guid userId, string token, string? deviceId, string? platform)
        {
            var existing = await _dbSet.FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token);
            if (existing != null)
            {
                existing.DeviceId = deviceId;
                existing.Platform = platform;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _dbSet.AddAsync(new UserFcmToken
                {
                    UserId = userId,
                    Token = token,
                    DeviceId = deviceId,
                    Platform = platform
                });
            }
        }

        public async Task DeleteByTokenAsync(Guid userId, string token)
        {
            await _dbSet.Where(t => t.UserId == userId && t.Token == token)
                .ExecuteDeleteAsync();
        }

        public async Task DeleteAllForUserAsync(Guid userId)
        {
            await _dbSet.Where(t => t.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}
