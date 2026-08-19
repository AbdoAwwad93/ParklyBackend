using AutoMapper;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class ParkingsService : IParkingsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ParkingsService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ParkingResponseDTO>>> GetAllAsync()
        {
            var parkings = await _unitOfWork.Repository<Parking>().GetAllAsync();
            var response = _mapper.Map<List<ParkingResponseDTO>>(parkings);
            return ApiResponse<List<ParkingResponseDTO>>.Success("Parkings retrieved successfully.", response);
        }

        public async Task<ApiResponse<ParkingResponseDTO>> GetByIdAsync(Guid id)
        {
            var parking = await _unitOfWork.Repository<Parking>().GetByIdAsync(id);
            if (parking == null)
            {
                return ApiResponse<ParkingResponseDTO>.Failure("Parking not found.");
            }

            var response = _mapper.Map<ParkingResponseDTO>(parking);
            return ApiResponse<ParkingResponseDTO>.Success("Parking retrieved successfully.", response);
        }

        public async Task<ApiResponse<ParkingResponseDTO>> CreateAsync(Guid ownerId, CreateParkingDTO dto)
        {
            var parkingOwner = await _unitOfWork.Repository<ParkingOwner>().FirstOrDefaultAsync(o => o.OwnerId == ownerId);
            if (parkingOwner == null)
            {
                return ApiResponse<ParkingResponseDTO>.Failure("Parking owner record not found.");
            }

            var parking = _mapper.Map<Parking>(dto);
            parking.OwnerId = ownerId;

            await _unitOfWork.Repository<Parking>().AddAsync(parking);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<ParkingResponseDTO>(parking);
            return ApiResponse<ParkingResponseDTO>.Success("Parking created successfully.", response);
        }

        public async Task<ApiResponse<ParkingResponseDTO>> UpdateAsync(Guid ownerId, Guid id, UpdateParkingDTO dto)
        {
            var parking = await _unitOfWork.Repository<Parking>().GetByIdAsync(id);
            if (parking == null)
            {
                return ApiResponse<ParkingResponseDTO>.Failure("Parking not found.");
            }
            if (parking.OwnerId != ownerId)
            {
                return ApiResponse<ParkingResponseDTO>.Failure("You do not have permission to modify this parking.");
            }

            _mapper.Map(dto, parking);

            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<ParkingResponseDTO>(parking);
            return ApiResponse<ParkingResponseDTO>.Success("Parking updated successfully.", response);
        }

        public async Task<ApiResponse> DeleteAsync(Guid ownerId, Guid id)
        {
            var parking = await _unitOfWork.Repository<Parking>().GetByIdAsync(id);
            if (parking == null)
            {
                return ApiResponse.Failure("Parking not found.");
            }
            if (parking.OwnerId != ownerId)
            {
                return ApiResponse.Failure("You do not have permission to delete this parking.");
            }

            _unitOfWork.Repository<Parking>().Delete(parking);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Parking deleted successfully.");
        }
    }
}