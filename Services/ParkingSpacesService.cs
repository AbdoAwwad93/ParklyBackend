using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        private readonly INotificationService _notificationService;

        public ParkingSpacesService(IUnitOfWork unitOfWork, IAvailabilityService availabilityService, IMapper mapper, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _availabilityService = availabilityService;
            _mapper = mapper;
            _notificationService = notificationService;
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
            space.Level = string.IsNullOrWhiteSpace(space.Level) ? null : space.Level.Trim();
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

            var wasActive = space.IsActive;
            _mapper.Map(dto, space);
            space.Level = string.IsNullOrWhiteSpace(space.Level) ? null : space.Level.Trim();

            if (wasActive && !space.IsActive)
            {
                await _notificationService.CreateAsync(ownerId, NotificationType.Alert,
                    $"Maintenance Alert — Space {space.SpotNumber}",
                    $"Space {space.SpotNumber} at {space.Parking.Name} has been marked unavailable.",
                    space.ParkingId, null, space.SpaceId);
            }
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
                query.VehicleSize?.ToString(), query.MaxRate,
                query.SpaceType?.ToString(), query.Level, query.Status?.ToString());
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
                    SpaceType = space.SpaceType,
                    Level = space.Level,
                    Status = space.Status,
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

        public async Task<ApiResponse<OwnerSpacesPageDTO>> GetOwnerSpacesAsync(
            Guid ownerId, Guid? parkingId, string? status, SpaceType? type, string? search, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var spaces = await GetOwnerSpacesQuery(ownerId).ToListAsync();
            var locations = BuildOwnerLocations(spaces);
            var summary = BuildSummary(spaces, locations.Count);

            IEnumerable<ParkingSpace> filtered = spaces;
            if (parkingId.HasValue)
            {
                filtered = filtered.Where(space => space.ParkingId == parkingId.Value);
            }

            if (type.HasValue)
            {
                filtered = filtered.Where(space => space.SpaceType == type.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                filtered = ApplyDisplayStatusFilter(filtered, status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(space =>
                    space.SpotNumber.ToLowerInvariant().Contains(normalizedSearch) ||
                    (space.Level != null && space.Level.ToLowerInvariant().Contains(normalizedSearch)) ||
                    space.Parking.Name.ToLowerInvariant().Contains(normalizedSearch));
            }

            var filteredList = filtered
                .OrderBy(space => space.Parking.Name)
                .ThenBy(space => NormalizeLevel(space.Level))
                .ThenBy(space => space.SpotNumber)
                .ToList();

            var totalCount = filteredList.Count;
            var items = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToOwnerSpaceListItem)
                .ToList();

            return ApiResponse<OwnerSpacesPageDTO>.Success("Owner parking spaces retrieved successfully.", new OwnerSpacesPageDTO
            {
                Summary = summary,
                Locations = locations,
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        public async Task<ApiResponse<OwnerAvailabilityDTO>> GetOwnerAvailabilityAsync(Guid ownerId, Guid? parkingId)
        {
            var spaces = await GetOwnerSpacesQuery(ownerId).ToListAsync();
            var locations = BuildOwnerLocations(spaces);
            var selectedParkingId = parkingId ?? locations.FirstOrDefault()?.ParkingId;
            var selectedSpaces = selectedParkingId.HasValue
                ? spaces.Where(space => space.ParkingId == selectedParkingId.Value).ToList()
                : new List<ParkingSpace>();

            var selectedName = selectedSpaces.FirstOrDefault()?.Parking.Name
                ?? locations.FirstOrDefault(location => location.ParkingId == selectedParkingId)?.Name
                ?? string.Empty;

            var levels = selectedSpaces
                .GroupBy(space => NormalizeLevel(space.Level))
                .OrderBy(group => group.Key)
                .Select(group => new AvailabilityLevelGroupDTO
                {
                    Level = group.Key,
                    Spaces = group.OrderBy(space => space.SpotNumber).Select(ToOwnerSpaceListItem).ToList()
                })
                .ToList();

            return ApiResponse<OwnerAvailabilityDTO>.Success("Owner space availability retrieved successfully.", new OwnerAvailabilityDTO
            {
                Locations = locations,
                SelectedParkingId = selectedParkingId,
                SelectedParkingName = selectedName,
                Summary = BuildSummary(selectedSpaces, selectedParkingId.HasValue ? 1 : 0),
                Levels = levels
            });
        }

        public async Task<ApiResponse<OwnerSpaceListItemDTO>> UpdateAvailabilityAsync(Guid ownerId, Guid spaceId, UpdateSpaceAvailabilityDTO dto)
        {
            var space = await GetOwnerSpaceAsync(ownerId, spaceId);
            if (space == null)
            {
                return ApiResponse<OwnerSpaceListItemDTO>.Failure("Parking space not found or you do not have permission.");
            }

            var wasActive = space.IsActive;
            space.IsActive = dto.IsActive;
            if (!space.IsActive)
            {
                space.Status = SpaceStatus.Available;
            }
            else
            {
                await RefreshSpaceStatusAsync(space.SpaceId);
            }

            if (wasActive && !space.IsActive)
            {
                await _notificationService.CreateAsync(ownerId, NotificationType.Alert,
                    $"Maintenance Alert - Space {space.SpotNumber}",
                    $"Space {space.SpotNumber} at {space.Parking.Name} has been marked unavailable.",
                    space.ParkingId, null, space.SpaceId);
            }

            await _unitOfWork.SaveChangesAsync();
            var refreshed = await _unitOfWork.ParkingSpaces.GetByIdWithParkingAsync(spaceId);
            return ApiResponse<OwnerSpaceListItemDTO>.Success("Space availability updated successfully.", ToOwnerSpaceListItem(refreshed!));
        }

        private IQueryable<ParkingSpace> GetOwnerSpacesQuery(Guid ownerId)
        {
            return _unitOfWork.ParkingSpaces.Query()
                .Include(space => space.Parking)
                .Where(space => space.Parking.OwnerId == ownerId);
        }

        private static List<OwnerSpaceLocationDTO> BuildOwnerLocations(List<ParkingSpace> spaces)
        {
            return spaces
                .GroupBy(space => new { space.ParkingId, space.Parking.Name })
                .OrderBy(group => group.Key.Name)
                .Select(group => new OwnerSpaceLocationDTO
                {
                    ParkingId = group.Key.ParkingId,
                    Name = group.Key.Name
                })
                .ToList();
        }

        private static OwnerSpaceSummaryDTO BuildSummary(List<ParkingSpace> spaces, int locationCount)
        {
            var available = spaces.Count(space => space.IsActive && space.Status == SpaceStatus.Available);
            var occupied = spaces.Count(space => space.IsActive && space.Status == SpaceStatus.Occupied);
            var reserved = spaces.Count(space => space.IsActive && space.Status == SpaceStatus.Reserved);
            var maintenance = spaces.Count(space => !space.IsActive);
            var total = spaces.Count;
            var used = occupied + reserved;

            return new OwnerSpaceSummaryDTO
            {
                TotalSpaces = total,
                Locations = locationCount,
                Available = available,
                Occupied = occupied,
                Reserved = reserved,
                Maintenance = maintenance,
                OccupancyPercentage = total == 0 ? 0 : (int)Math.Round(used * 100.0 / total)
            };
        }

        private static IEnumerable<ParkingSpace> ApplyDisplayStatusFilter(IEnumerable<ParkingSpace> spaces, string status)
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            return normalizedStatus switch
            {
                "available" => spaces.Where(space => space.IsActive && space.Status == SpaceStatus.Available),
                "occupied" => spaces.Where(space => space.IsActive && space.Status == SpaceStatus.Occupied),
                "reserved" => spaces.Where(space => space.IsActive && space.Status == SpaceStatus.Reserved),
                "maintenance" or "inactive" => spaces.Where(space => !space.IsActive),
                _ => spaces
            };
        }

        private static OwnerSpaceListItemDTO ToOwnerSpaceListItem(ParkingSpace space) => new()
        {
            SpaceId = space.SpaceId,
            ParkingId = space.ParkingId,
            ParkingName = space.Parking?.Name ?? string.Empty,
            SpotNumber = space.SpotNumber,
            Level = NormalizeLevel(space.Level),
            SpaceType = space.SpaceType,
            DisplayType = FormatSpaceType(space.SpaceType),
            BaseHourlyRate = space.BaseHourlyRate,
            Status = space.Status,
            DisplayStatus = space.IsActive ? space.Status.ToString() : "Maintenance",
            IsActive = space.IsActive
        };

        private static string NormalizeLevel(string? level)
            => string.IsNullOrWhiteSpace(level) ? "Unassigned" : level.Trim();

        private static string FormatSpaceType(SpaceType type) => type switch
        {
            SpaceType.EVCharging => "EV Charging",
            _ => type.ToString()
        };

        /// <summary>
        /// Recomputes Space.Status from live reservations.
        /// CheckedIn => Occupied, Confirmed overlapping now => Reserved, else Available.
        /// Must be called inside an active transaction; caller commits.
        /// </summary>
        public async Task RefreshSpaceStatusAsync(Guid spaceId)
        {
            var now = DateTime.UtcNow;

            var hasCheckedIn = await _unitOfWork.Reservations.AnyAsync(r =>
                r.SpaceId == spaceId && r.Status == ReservationStatus.CheckedIn);

            SpaceStatus nextStatus;
            if (hasCheckedIn)
            {
                nextStatus = SpaceStatus.Occupied;
            }
            else
            {
                var hasConfirmedNow = await _unitOfWork.Reservations.AnyAsync(r =>
                    r.SpaceId == spaceId
                    && r.Status == ReservationStatus.Confirmed
                    && r.ArrivalTime <= now
                    && r.DepartureTime > now);
                nextStatus = hasConfirmedNow ? SpaceStatus.Reserved : SpaceStatus.Available;
            }

            var space = await _unitOfWork.ParkingSpaces.GetByIdAsync(spaceId);
            if (space != null && space.Status != nextStatus)
            {
                space.Status = nextStatus;
            }
        }
    }
}
