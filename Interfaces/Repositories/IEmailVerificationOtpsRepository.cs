using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IEmailVerificationOtpsRepository : IGenericRepository<EmailVerificationOtp>
    {
        Task<List<EmailVerificationOtp>> GetActiveOtpsAsync(Guid userId);
        Task<EmailVerificationOtp?> GetLatestValidOtpAsync(Guid userId);
    }
}
