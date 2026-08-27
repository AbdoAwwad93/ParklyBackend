using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class EmailVerificationOtpsRepository : GenericRepository<EmailVerificationOtp>, IEmailVerificationOtpsRepository
    {
        public EmailVerificationOtpsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<EmailVerificationOtp>> GetActiveOtpsAsync(Guid userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<EmailVerificationOtp?> GetLatestValidOtpAsync(Guid userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
