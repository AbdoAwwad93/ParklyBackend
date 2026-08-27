using Microsoft.EntityFrameworkCore.Storage;
using Parkly_Backend.Interfaces.Repositories;

namespace Parkly_Backend.Data.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IParkingsRepository Parkings { get; }
        IParkingSpacesRepository ParkingSpaces { get; }
        IReservationsRepository Reservations { get; }
        ISavedPlacesRepository SavedPlaces { get; }
        IReviewsRepository Reviews { get; }

        IParkingOwnersRepository ParkingOwners { get; }
        IRefreshTokensRepository RefreshTokens { get; }
        IEmailVerificationOtpsRepository EmailVerificationOtps { get; }
        IPasswordResetOtpsRepository PasswordResetOtps { get; }
        IAccessLogsRepository AccessLogs { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}