using AutoMapper;
using Parkly_Backend.Common.Helpers;
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
        private readonly IAvailabilityService _availabilityService;
        private readonly IMapper _mapper;

        public ParkingSpacesService(IUnitOfWork unitOfWork, IAvailabilityService availabilityService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _availabilityService = availabilityService;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ParkingSpaceResponseDTO>>> GetAllAsync()
        {
            var spaces = await _unitOfWork.ParkingSpaces.GetAllWithParkingAsync();

            var response = _mapper.Map<List<ParkingSpaceResponseDTO>>(spaces);
            return ApiResponse<List<ParkingSpaceResponseDTO>>.Success("Parking spaces retrieved successfully.", response);
        }

        public async Task<ApiResponse<List<ParkingSpaceResponseDTO>>> GetByParkingIdAsync(Guid parkingId)
        {
            var parking = await _unitOfWork.Parkings.GetByIdAsync(parkingId);
            if (parking == null)
            {
                return ApiResponse<List<ParkingSpaceResponseDTO>>.Failure("Parking not found.");
            }

            var spaces = await _unitOfWork.ParkingSpaces.GetByParkingIdWithParkingAsync(parkingId);

            var response = _mapper.Map<List<ParkingSpaceResponseDTO>>(spaces);
            return ApiResponse<List<ParkingSpaceResponseDTO>>.Success("Parking spaces retrieved successfully.", response);
        }

        public async Task<ApiResponse<ParkingSpaceResponseDTO>> GetByIdAsync(Guid spaceId)
        {
            var space = await _unitOfWork.ParkingSpaces.GetByIdWithParkingAsync(spaceId);

            if (space == null)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("Parking space not found.");
            }

            var response = _mapper.Map<ParkingSpaceResponseDTO>(space);
            return ApiResponse<ParkingSpaceResponseDTO>.Success("Parking space retrieved successfully.", response);
        }

        public async Task<ApiResponse<ParkingSpaceResponseDTO>> CreateAsync(Guid ownerId, CreateParkingSpaceDTO dto)
        {
            var parking = await _unitOfWork.Parkings.GetByIdAsync(dto.ParkingId);
            if (parking == null)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("Parking not found.");
            }
            if (parking.OwnerId != ownerId)
            {
                return ApiResponse<ParkingSpaceResponseDTO>.Failure("You do not have permission to add spaces to this parking.");
            }

            var space = _mapper.Map<ParkingSpace>(dto);
            await _unitOfWork.ParkingSpaces.AddAsync(space);
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

            var hasActiveReservations = await _unitOfWork.ParkingSpaces.HasActiveReservationsAsync(spaceId);
            if (hasActiveReservations)
            {
                return ApiResponse.Failure("Cannot delete this space because it has active reservations.");
            }

            _unitOfWork.ParkingSpaces.Delete(space);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Parking space deleted successfully.");
        }

        private async Task<ParkingSpace?> GetOwnerSpaceAsync(Guid ownerId, Guid spaceId)
        {
            return await _unitOfWork.ParkingSpaces.GetOwnerSpaceAsync(ownerId, spaceId);
        }

        private async Task<ParkingSpaceResponseDTO> BuildResponseAsync(Guid spaceId)
        {
            var space = await _unitOfWork.ParkingSpaces.GetByIdWithParkingAsync(spaceId);

            return _mapper.Map<ParkingSpaceResponseDTO>(space);
        }

        public async Task<ApiResponse<List<NearbyParkingSpaceDTO>>> GetNearbySpacesAsync(NearbyParkingQuery query)
        {
            if (query.Latitude < -90 || query.Latitude > 90)
            {
                return ApiResponse<List<NearbyParkingSpaceDTO>>.Failure("Latitude must be between -90 and 90.");
            }
            if (query.Longitude < -180 || query.Longitude > 180)
            {
                return ApiResponse<List<NearbyParkingSpaceDTO>>.Failure("Longitude must be between -180 and 180.");
            }

            var arrival = query.Arrival ?? DateTime.UtcNow;
            var departure = query.Departure ?? arrival.AddHours(1);

            if (arrival >= departure)
            {
                return ApiResponse<List<NearbyParkingSpaceDTO>>.Failure("Departure time must be after arrival time.");
            }

            var radius = query.RadiusKm > 0 ? query.RadiusKm : 5.0;
            var (minLat, maxLat, minLng, maxLng) = GeoHelper.GetBoundingBox(query.Latitude, query.Longitude, radius);

            var candidateSpaces = await _unitOfWork.ParkingSpaces.GetCandidateSpacesInBoundingBoxAsync(
                minLat,maxLat,minLng,maxLng, 
                query.VehicleSize?.ToString(), query.MaxRate);
            if (candidateSpaces.Count == 0)
            {
                return ApiResponse<List<NearbyParkingSpaceDTO>>.Success("Nearby parking spaces retrieved successfully.", new List<NearbyParkingSpaceDTO>());
            }
            var inRangeSpaces = new List<ParkingSpace>();
            var spaceDistances = new Dictionary<Guid, double>();

            foreach (var space in candidateSpaces)
            {
                var distance = GeoHelper.DistanceKm(space.Parking.Latitude, space.Parking.Longitude, query.Latitude, query.Longitude);
                if (distance > radius)
                {
                    continue;
                }

                var isWindowOpen = GeoHelper.IsWindowWithinOperatingHours(space.Parking.OperatingHours, arrival, departure);
                if (!query.IncludeClosed && !isWindowOpen)
                {
                    continue;
                }

                inRangeSpaces.Add(space);
                spaceDistances[space.SpaceId] = distance;
            }

            if (inRangeSpaces.Count == 0)
            {
                return ApiResponse<List<NearbyParkingSpaceDTO>>.Success("Nearby parking spaces retrieved successfully.", new List<NearbyParkingSpaceDTO>());
            }
            var inRangeParkingIds = inRangeSpaces.Select(s => s.ParkingId).Distinct().ToList();
            var availableSpacesByParking = await _availabilityService.GetAvailableSpacesForParkingsAsync(inRangeParkingIds, arrival, departure);
            var availableSpaceIds = availableSpacesByParking.Values
                .SelectMany(spaces => spaces)
                .Select(s => s.SpaceId)
                .ToHashSet();

            var results = new List<NearbyParkingSpaceDTO>();

            foreach (var space in inRangeSpaces)
            {
                var isAvailable = availableSpaceIds.Contains(space.SpaceId);

                if (query.OnlyAvailable && !isAvailable)
                {
                    continue;
                }

                results.Add(new NearbyParkingSpaceDTO
                {
                    SpaceId = space.SpaceId,
                    SpotNumber = space.SpotNumber,
                    VehicleSize = space.VehicleSize,
                    BaseHourlyRate = space.BaseHourlyRate,
                    ParkingId = space.ParkingId,
                    ParkingName = space.Parking.Name,
                    ParkingAddress = space.Parking.Address,
                    Latitude = space.Parking.Latitude,
                    Longitude = space.Parking.Longitude,
                    DistanceKm = spaceDistances[space.SpaceId],
                    IsAvailable = isAvailable
                });
            }

            IEnumerable<NearbyParkingSpaceDTO> sorted = query.SortBy == NearbySortBy.Price
                ? results.OrderBy(s => s.BaseHourlyRate).ThenBy(s => s.DistanceKm)
                : results.OrderBy(s => s.DistanceKm).ThenBy(s => s.BaseHourlyRate);

            var page = query.Page > 0 ? query.Page : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;
            var pagedResults = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return ApiResponse<List<NearbyParkingSpaceDTO>>.Success("Nearby parking spaces retrieved successfully.", pagedResults);
        }
    }
}