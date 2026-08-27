using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class AccessLogsRepository : GenericRepository<AccessLog>, IAccessLogsRepository
    {
        public AccessLogsRepository(AppDbContext context) : base(context)
        {
        }
    }
}
