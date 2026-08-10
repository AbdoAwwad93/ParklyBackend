using Microsoft.EntityFrameworkCore;

namespace Parkly_Backend.Data.Repositories
{
    public class GenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            
        }
        public async Task<List<T>> GetAllAsync()
        {
            var data = await _context.Set<T>().ToListAsync();
            return data;
        }

    }
}
