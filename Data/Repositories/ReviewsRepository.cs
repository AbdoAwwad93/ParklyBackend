using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class ReviewsRepository : GenericRepository<Review>, IReviewsRepository
    {
        public ReviewsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Review?> GetReviewWithIncludesAsync(Guid reviewId)
        {
            return await _dbSet
                .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
                .Include(r => r.Reservation.ParkingSpace.Parking)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);
        }

        public async Task<List<Review>> GetReviewsForParkingAsync(Guid parkingId, int skip, int take)
        {
            return await _dbSet
                .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
                .Include(r => r.Reservation.ParkingSpace.Parking)
                .Where(r => r.Reservation.ParkingSpace.ParkingId == parkingId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetTotalReviewsForParkingAsync(Guid parkingId)
        {
            return await _dbSet
                .Where(r => r.Reservation.ParkingSpace.ParkingId == parkingId)
                .CountAsync();
        }

        public async Task<double> GetAverageRatingForParkingAsync(Guid parkingId)
        {
            var count = await GetTotalReviewsForParkingAsync(parkingId);
            if (count == 0) return 0;
            
            return await _dbSet
                .Where(r => r.Reservation.ParkingSpace.ParkingId == parkingId)
                .AverageAsync(r => r.Rating);
        }

        public async Task<List<Review>> GetUserReviewsAsync(Guid userId)
        {
            return await _dbSet
                .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
                .Include(r => r.Reservation.ParkingSpace.Parking)
                .Where(r => r.Reservation.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
