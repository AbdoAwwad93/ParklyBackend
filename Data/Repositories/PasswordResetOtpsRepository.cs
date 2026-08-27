using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class PasswordResetOtpsRepository : GenericRepository<PasswordResetOtp>, IPasswordResetOtpsRepository
    {
        public PasswordResetOtpsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<PasswordResetOtp>> GetActiveOtpsAsync(Guid userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<PasswordResetOtp?> GetLatestValidOtpAsync(Guid userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
