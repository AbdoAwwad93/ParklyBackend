using System;
using System.Collections.Generic;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Models.DTOs
{
    public class OwnerSpaceLocationDTO
    {
        public Guid ParkingId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class OwnerSpaceSummaryDTO
    {
        public int TotalSpaces { get; set; }
        public int Locations { get; set; }
        public int Available { get; set; }
        public int Occupied { get; set; }
        public int Reserved { get; set; }
        public int Maintenance { get; set; }
        public int OccupancyPercentage { get; set; }
    }

    public class OwnerSpaceListItemDTO
    {
        public Guid SpaceId { get; set; }
        public Guid ParkingId { get; set; }
        public string ParkingName { get; set; } = string.Empty;
        public string SpotNumber { get; set; } = string.Empty;
        public string? Level { get; set; }
        public SpaceType SpaceType { get; set; }
        public string DisplayType { get; set; } = string.Empty;
        public decimal BaseHourlyRate { get; set; }
        public SpaceStatus Status { get; set; }
        public string DisplayStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class OwnerSpacesPageDTO
    {
        public OwnerSpaceSummaryDTO Summary { get; set; } = new();
        public List<OwnerSpaceLocationDTO> Locations { get; set; } = [];
        public List<OwnerSpaceListItemDTO> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class OwnerAvailabilityDTO
    {
        public List<OwnerSpaceLocationDTO> Locations { get; set; } = [];
        public Guid? SelectedParkingId { get; set; }
        public string SelectedParkingName { get; set; } = string.Empty;
        public OwnerSpaceSummaryDTO Summary { get; set; } = new();
        public List<AvailabilityLevelGroupDTO> Levels { get; set; } = [];
    }

    public class AvailabilityLevelGroupDTO
    {
        public string Level { get; set; } = string.Empty;
        public List<OwnerSpaceListItemDTO> Spaces { get; set; } = [];
    }

    public class UpdateSpaceAvailabilityDTO
    {
        public bool IsActive { get; set; }
    }
}
