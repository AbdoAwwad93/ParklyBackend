using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IUserFcmTokensRepository : IGenericRepository<UserFcmToken>
    {
        /// <summary>Returns all FCM tokens registered for the given user.</summary>
        Task<List<string>> GetTokensByUserIdAsync(Guid userId);

        /// <summary>Inserts a new token or updates the timestamp if the same user+token already exists.</summary>
        Task UpsertAsync(Guid userId, string token, string? deviceId, string? platform);

        /// <summary>Removes a specific token (e.g. on logout).</summary>
        Task DeleteByTokenAsync(Guid userId, string token);

        /// <summary>Removes all tokens for a user (e.g. on account deletion).</summary>
        Task DeleteAllForUserAsync(Guid userId);
    }
}
