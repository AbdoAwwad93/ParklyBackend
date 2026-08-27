using Microsoft.EntityFrameworkCore.Storage;
using Parkly_Backend.Interfaces.Repositories;

namespace Parkly_Backend.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        private IParkingsRepository? _parkingsRepository;
        private IParkingSpacesRepository? _parkingSpacesRepository;
        private IReservationsRepository? _reservationsRepository;
        private ISavedPlacesRepository? _savedPlacesRepository;
        private IReviewsRepository? _reviewsRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IParkingsRepository Parkings => _parkingsRepository ??= new ParkingsRepository(_context);
        public IParkingSpacesRepository ParkingSpaces => _parkingSpacesRepository ??= new ParkingSpacesRepository(_context);
        public IReservationsRepository Reservations => _reservationsRepository ??= new ReservationsRepository(_context);
        public ISavedPlacesRepository SavedPlaces => _savedPlacesRepository ??= new SavedPlacesRepository(_context);
        public IReviewsRepository Reviews => _reviewsRepository ??= new ReviewsRepository(_context);

        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories.TryGetValue(typeof(T), out var repository))
            {
                return (IGenericRepository<T>)repository;
            }

            var newRepository = new GenericRepository<T>(_context);
            _repositories[typeof(T)] = newRepository;
            return newRepository;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
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