using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class ParkingSpacesService : IParkingSpacesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ParkingSpacesService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ParkingSpaceResponseDTO>>> GetAllAsync()
        {
            var spaces = await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                .ToListAsync();

            var response = _mapper.Map<List<ParkingSpaceResponseDTO>>(spaces);
            return ApiResponse<List<ParkingSpaceResponseDTO>>.Success("Parking spaces retrieved successfully.", response);
        }

        public async Task<ApiResponse<List<ParkingSpaceResponseDTO>>> GetByParkingIdAsync(Guid parkingId)
        {
            var parking = await _unitOfWork.Repository<Parking>().GetByIdAsync(parkingId);
            if (parking == null)
            {
                return ApiResponse<List<ParkingSpaceResponseDTO>>.Failure("Parking not found.");
            }

            var spaces = await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                .Where(s => s.ParkingId == parkingId)
                .ToListAsync();

            var response = _mapper.Map<List<ParkingSpaceResponseDTO>>(spaces);
            return ApiResponse<List<ParkingSpaceResponseDTO>>.Success("Parking spaces retrieved successfully.", response);
        }

        public async Task<ApiResponse<ParkingSpaceResponseDTO>> GetByIdAsync(Guid spaceId)
        {
            var space = await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId);

            if (space == null)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("Parking space not found.");
            }

            var response = _mapper.Map<ParkingSpaceResponseDTO>(space);
            return ApiResponse<ParkingSpaceResponseDTO>.Success("Parking space retrieved successfully.", response);
        }

        public async Task<ApiResponse<ParkingSpaceResponseDTO>> CreateAsync(Guid ownerId, CreateParkingSpaceDTO dto)
        {
            var parking = await _unitOfWork.Repository<Parking>().GetByIdAsync(dto.ParkingId);
            if (parking == null)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("Parking not found.");
            }
            if (parking.OwnerId != ownerId)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("You do not have permission to add spaces to this parking.");
            }

            var space = _mapper.Map<ParkingSpace>(dto);
            await _unitOfWork.Repository<ParkingSpace>().AddAsync(space);
            await _unitOfWork.SaveChangesAsync();

            var response = await BuildResponseAsync(space.SpaceId);
            return ApiResponse<ParkingSpaceResponseDTO>.Success("Parking space created successfully.", response);
        }

        public async Task<ApiResponse<ParkingSpaceResponseDTO>> UpdateAsync(Guid ownerId, Guid spaceId, UpdateParkingSpaceDTO dto)
        {
            var space = await GetOwnerSpaceAsync(ownerId, spaceId);
            if (space == null)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("Parking space not found or you do not have permission.");
            }

            _mapper.Map(dto, space);
            await _unitOfWork.SaveChangesAsync();

            var response = await BuildResponseAsync(space.SpaceId);
            return ApiResponse<ParkingSpaceResponseDTO>.Success("Parking space updated successfully.", response);
        }

        public async Task<ApiResponse> DeleteAsync(Guid ownerId, Guid spaceId)
        {
            var space = await GetOwnerSpaceAsync(ownerId, spaceId);
            if (space == null)
            {
                return ApiResponse.Failure("Parking space not found or you do not have permission.");
            }

            var hasActiveReservations = await _unitOfWork.Repository<Reservation>()
                .AnyAsync(r => r.SpaceId == spaceId && r.Status != ReservationStatus.Cancelled);
            if (hasActiveReservations)
            {
                return ApiResponse.Failure("Cannot delete this space because it has active reservations.");
            }

            _unitOfWork.Repository<ParkingSpace>().Delete(space);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Parking space deleted successfully.");
        }

        private async Task<ParkingSpace?> GetOwnerSpaceAsync(Guid ownerId, Guid spaceId)
        {
            return await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                .FirstOrDefaultAsync(s => s.SpaceId == spaceId && s.Parking.OwnerId == ownerId);
        }

        private async Task<ParkingSpaceResponseDTO> BuildResponseAsync(Guid spaceId)
        {
            var space = await _unitOfWork.Repository<ParkingSpace>().Query()
                .Include(s => s.Parking)
                .FirstAsync(s => s.SpaceId == spaceId);

            return _mapper.Map<ParkingSpaceResponseDTO>(space);
        }
    }
}