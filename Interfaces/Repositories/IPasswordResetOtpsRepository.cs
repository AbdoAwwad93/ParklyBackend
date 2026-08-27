using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IPasswordResetOtpsRepository : IGenericRepository<PasswordResetOtp>
    {
        Task<List<PasswordResetOtp>> GetActiveOtpsAsync(Guid userId);
        Task<PasswordResetOtp?> GetLatestValidOtpAsync(Guid userId);
    }
}
