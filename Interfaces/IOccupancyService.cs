namespace Parkly_Backend.Interfaces
{
    public interface IOccupancyService
    {
        Task BroadcastOccupancyUpdateAsync(Guid parkingId);
    }
}
