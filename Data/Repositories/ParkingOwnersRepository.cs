using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class ParkingOwnersRepository : GenericRepository<ParkingOwner>, IParkingOwnersRepository
    {
        public ParkingOwnersRepository(AppDbContext context) : base(context)
        {
        }
    }
}
