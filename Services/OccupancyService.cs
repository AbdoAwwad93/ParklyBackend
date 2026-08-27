using Microsoft.AspNetCore.SignalR;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Hubs;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Services
{
    public class OccupancyService : IOccupancyService
    {
        private readonly IHubContext<OccupancyHub> _hubContext;
        private readonly IUnitOfWork _unitOfWork;

        public OccupancyService(IHubContext<OccupancyHub> hubContext, IUnitOfWork unitOfWork)
        {
            _hubContext = hubContext;
            _unitOfWork = unitOfWork;
        }

        public async Task BroadcastOccupancyUpdateAsync(Guid parkingId)
        {
            // Calculate current occupancy: count of reservations with CheckedIn status for this parking
            var currentOccupancy = await _unitOfWork.Reservations.GetCheckedInCountForParkingAsync(parkingId);

            await _hubContext.Clients.Group($"Parking_{parkingId}").SendAsync("ReceiveOccupancyUpdate", parkingId, currentOccupancy);
        }
    }
}
