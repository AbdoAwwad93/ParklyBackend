using Microsoft.AspNetCore.SignalR;

namespace Parkly_Backend.Hubs
{
    public class OccupancyHub : Hub
    {
        public async Task JoinParkingGroup(string parkingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Parking_{parkingId}");
        }

        public async Task LeaveParkingGroup(string parkingId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Parking_{parkingId}");
        }
    }
}
