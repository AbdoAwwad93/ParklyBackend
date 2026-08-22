using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        private readonly IAvailabilityService _availabilityService;
        private readonly IMapper _mapper;

        public ParkingsService(IUnitOfWork unitOfWork, IAvailabilityService availabilityService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _availabilityService = availabilityService;
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

        public async Task<ApiResponse<List<SearchParkingDTO>>> SearchAsync(SearchParkingQuery query)
        {
            var queryable = _unitOfWork.Repository<Parking>().Query()
                .Include(p => p.ParkingSpaces)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim().ToLower();
                queryable = queryable.Where(p =>
                    p.Name.ToLower().Contains(keyword)
                    || p.Address.ToLower().Contains(keyword));
            }

            if (query.VehicleSize.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.ParkingSpaces.Any(s => s.IsActive && s.VehicleSize == query.VehicleSize));
            }

            if (query.MaxRate.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.ParkingSpaces.Any(s => s.IsActive && s.BaseHourlyRate <= query.MaxRate));
            }

            var parkings = await queryable.ToListAsync();
            IEnumerable<Parking> filtered = parkings;

            if (query.Latitude.HasValue && query.Longitude.HasValue && query.RadiusKm.HasValue)
            {
                var lat = query.Latitude.Value;
                var lng = query.Longitude.Value;
                var radius = query.RadiusKm.Value;
                filtered = filtered.Where(p => DistanceKm(p.Latitude, p.Longitude, lat, lng) <= radius);
            }

            var arrival = query.Arrival ?? DateTime.UtcNow;
            var departure = query.Departure ?? arrival.AddHours(1);

            var results = new List<SearchParkingDTO>();
            foreach (var parking in filtered)
            {
                var availableSpaces = await _availabilityService.GetAvailableSpacesAsync(parking.ParkingId, arrival, departure);
                var activeSpaces = parking.ParkingSpaces.Where(s => s.IsActive).ToList();

                results.Add(new SearchParkingDTO
                {
                    ParkingId = parking.ParkingId,
                    OwnerId = parking.OwnerId,
                    Name = parking.Name,
                    Address = parking.Address,
                    Latitude = parking.Latitude,
                    Longitude = parking.Longitude,
                    OperatingHours = parking.OperatingHours,
                    DistanceKm = query.Latitude.HasValue && query.Longitude.HasValue
                        ? DistanceKm(parking.Latitude, parking.Longitude, query.Latitude.Value, query.Longitude.Value)
                        : null,
                    AvailableSpaces = availableSpaces.Count,
                    MinHourlyRate = activeSpaces.Count > 0 ? activeSpaces.Min(s => s.BaseHourlyRate) : null
                });
            }

            return ApiResponse<List<SearchParkingDTO>>.Success("Search completed successfully.", results);
        }

        private static double DistanceKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
            var dLng = (double)(lng2 - lng1) * Math.PI / 180.0;
            var sinLat = Math.Sin(dLat / 2) * Math.Sin(dLat / 2);
            var sinLng = Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var cosLat1 = Math.Cos((double)lat1 * Math.PI / 180.0);
            var cosLat2 = Math.Cos((double)lat2 * Math.PI / 180.0);

            var a = sinLat + cosLat1 * cosLat2 * sinLng;
            return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}