using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IReviewsRepository : IGenericRepository<Review>
    {
        Task<Review?> GetReviewWithIncludesAsync(Guid reviewId);
        Task<List<Review>> GetReviewsForParkingAsync(Guid parkingId, int skip, int take);
        Task<int> GetTotalReviewsForParkingAsync(Guid parkingId);
        Task<double> GetAverageRatingForParkingAsync(Guid parkingId);
        Task<List<Review>> GetUserReviewsAsync(Guid userId);
        Task<Dictionary<Guid, (double AverageRating, int TotalReviews)>> GetReviewStatsForParkingsAsync(IEnumerable<Guid> parkingIds);
    }
}
