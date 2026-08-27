using Microsoft.EntityFrameworkCore.Storage;
using Parkly_Backend.Interfaces.Repositories;

namespace Parkly_Backend.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;



        public IParkingsRepository Parkings { get; private set; }
        public IParkingSpacesRepository ParkingSpaces { get; private set; }
        public IReservationsRepository Reservations { get; private set; }
        public ISavedPlacesRepository SavedPlaces { get; private set; }
        public IReviewsRepository Reviews { get; private set; }
        public IParkingOwnersRepository ParkingOwners { get; private set; }
        public IRefreshTokensRepository RefreshTokens { get; private set; }
        public IEmailVerificationOtpsRepository EmailVerificationOtps { get; private set; }
        public IPasswordResetOtpsRepository PasswordResetOtps { get; private set; }
        public IAccessLogsRepository AccessLogs { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Parkings = new ParkingsRepository(_context);
            Reservations = new ReservationsRepository(_context);
            SavedPlaces = new SavedPlacesRepository(_context);
            Reviews = new ReviewsRepository(_context);
            ParkingSpaces = new ParkingSpacesRepository(_context);
            ParkingOwners = new ParkingOwnersRepository(_context);
            RefreshTokens = new RefreshTokensRepository(_context);
            EmailVerificationOtps = new EmailVerificationOtpsRepository(_context);
            PasswordResetOtps = new PasswordResetOtpsRepository(_context);
            AccessLogs = new AccessLogsRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                return _transaction;
            }

            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                return;
            }

            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                return;
            }

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}