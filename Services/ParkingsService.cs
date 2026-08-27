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
            var parkings = await _unitOfWork.Parkings.GetParkingsWithSpacesAsync();
            var filtered = parkings.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim().ToLower();
                filtered = filtered.Where(p =>
                    p.Name.ToLower().Contains(keyword)
                    || p.Address.ToLower().Contains(keyword));
            }

            if (query.VehicleSize.HasValue)
            {
                filtered = filtered.Where(p =>
                    p.ParkingSpaces.Any(s => s.IsActive && s.VehicleSize == query.VehicleSize));
            }

            if (query.MaxRate.HasValue)
            {
                filtered = filtered.Where(p =>
                    p.ParkingSpaces.Any(s => s.IsActive && s.BaseHourlyRate <= query.MaxRate));
            }

            if (query.Latitude.HasValue && query.Longitude.HasValue && query.RadiusKm.HasValue)
            {
                var lat = query.Latitude.Value;
                var lng = query.Longitude.Value;
                var radius = query.RadiusKm.Value;
                filtered = filtered.Where(p => GeoHelper.DistanceKm(p.Latitude, p.Longitude, lat, lng) <= radius);
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
                        ? GeoHelper.DistanceKm(parking.Latitude, parking.Longitude, query.Latitude.Value, query.Longitude.Value)
                        : null,
                    AvailableSpaces = availableSpaces.Count,
                    MinHourlyRate = activeSpaces.Count > 0 ? activeSpaces.Min(s => s.BaseHourlyRate) : null
                });
            }

            if (query.Latitude.HasValue && query.Longitude.HasValue)
            {
                results = results.OrderBy(r => r.DistanceKm ?? double.MaxValue).ToList();
            }

            return ApiResponse<List<SearchParkingDTO>>.Success("Search completed successfully.", results);
        }

        public async Task<ApiResponse<List<NearbyParkingDTO>>> GetNearbyAsync(NearbyParkingQuery query)
        {
            if (query.Latitude < -90 || query.Latitude > 90)
            {
                return ApiResponse<List<NearbyParkingDTO>>.Failure("Latitude must be between -90 and 90.");
            }
            if (query.Longitude < -180 || query.Longitude > 180)
            {
                return ApiResponse<List<NearbyParkingDTO>>.Failure("Longitude must be between -180 and 180.");
            }

            var arrival = query.Arrival ?? DateTime.UtcNow;
            var departure = query.Departure ?? arrival.AddHours(1);

            if (arrival >= departure)
            {
                return ApiResponse<List<NearbyParkingDTO>>.Failure("Departure time must be after arrival time.");
            }

            var radius = query.RadiusKm > 0 ? query.RadiusKm : 5.0;
            var (minLat, maxLat, minLng, maxLng) = GeoHelper.GetBoundingBox(query.Latitude, query.Longitude, radius);

            var candidateParkings = await _unitOfWork.Parkings.GetCandidateParkingsInBoundingBoxAsync(
            minLat,maxLat,minLng,maxLng,query.VehicleSize?.ToString(), query.MaxRate);

            var inRangeParkings = new List<(Parking Parking, double Distance, bool IsOpenNow)>();
            foreach (var parking in candidateParkings)
            {
                var distance = GeoHelper.DistanceKm(parking.Latitude, parking.Longitude, query.Latitude, query.Longitude);
                if (distance > radius)
                {
                    continue;
                }

                var isWindowOpen = GeoHelper.IsWindowWithinOperatingHours(parking.OperatingHours, arrival, departure);
                if (!query.IncludeClosed && !isWindowOpen)
                {
                    continue;
                }

                var isOpenNow = GeoHelper.IsOpenAt(parking.OperatingHours, DateTime.UtcNow);
                inRangeParkings.Add((parking, distance, isOpenNow));
            }

            if (inRangeParkings.Count == 0)
            {
                return ApiResponse<List<NearbyParkingDTO>>.Success("Nearby parkings retrieved successfully.", new List<NearbyParkingDTO>());
            }

            // Single batch query for available spaces across all in-range facilities (eliminates N+1)
            var parkingIds = inRangeParkings.Select(x => x.Parking.ParkingId).ToList();
            var availableSpacesByParking = await _availabilityService.GetAvailableSpacesForParkingsAsync(parkingIds, arrival, departure);

            var results = new List<NearbyParkingDTO>();

            foreach (var (parking, distance, isOpenNow) in inRangeParkings)
            {
                var availableSpaces = availableSpacesByParking.GetValueOrDefault(parking.ParkingId) ?? new List<ParkingSpace>();
                var activeSpaces = parking.ParkingSpaces.Where(s => s.IsActive).ToList();

                if (query.VehicleSize.HasValue)
                {
                    availableSpaces = availableSpaces.Where(s => s.VehicleSize == query.VehicleSize.Value).ToList();
                }

                if (query.MaxRate.HasValue)
                {
                    availableSpaces = availableSpaces.Where(s => s.BaseHourlyRate <= query.MaxRate.Value).ToList();
                }

                if (query.OnlyAvailable && availableSpaces.Count == 0)
                {
                    continue;
                }

                decimal? minRate = null;
                if (availableSpaces.Count > 0)
                {
                    minRate = availableSpaces.Min(s => s.BaseHourlyRate);
                }
                else if (activeSpaces.Count > 0)
                {
                    minRate = activeSpaces.Min(s => s.BaseHourlyRate);
                }

                results.Add(new NearbyParkingDTO
                {
                    ParkingId = parking.ParkingId,
                    OwnerId = parking.OwnerId,
                    Name = parking.Name,
                    Address = parking.Address,
                    Latitude = parking.Latitude,
                    Longitude = parking.Longitude,
                    OperatingHours = parking.OperatingHours,
                    IsOpenNow = isOpenNow,
                    DistanceKm = distance,
                    AvailableSpaces = availableSpaces.Count,
                    TotalSpaces = activeSpaces.Count,
                    MinHourlyRate = minRate
                });
            }

            IEnumerable<NearbyParkingDTO> sorted = query.SortBy == NearbySortBy.Price
                ? results.OrderBy(p => p.MinHourlyRate ?? decimal.MaxValue).ThenBy(p => p.DistanceKm)
                : results.OrderBy(p => p.DistanceKm).ThenBy(p => p.MinHourlyRate ?? decimal.MaxValue);

            var page = query.Page > 0 ? query.Page : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;
            var pagedResults = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return ApiResponse<List<NearbyParkingDTO>>.Success("Nearby parkings retrieved successfully.", pagedResults);
        }
    }
}