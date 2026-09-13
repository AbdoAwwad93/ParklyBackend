using AutoMapper;
using Parkly_Backend.Common.Helpers;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterDTO, AppUser>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ParkingOwner, opt => opt.Ignore())
                .ForMember(dest => dest.Reservations, opt => opt.Ignore())
                .ForMember(dest => dest.Disputes, opt => opt.Ignore());

            CreateMap<OwnerRegisterDTO, AppUser>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ParkingOwner, opt => opt.Ignore())
                .ForMember(dest => dest.Reservations, opt => opt.Ignore())
                .ForMember(dest => dest.Disputes, opt => opt.Ignore());

            CreateMap<Reservation, ReservationResponseDTO>()
                .ForMember(dest => dest.ParkingId, opt => opt.MapFrom(src => src.ParkingSpace.ParkingId))
                .ForMember(dest => dest.SpotNumber, opt => opt.MapFrom(src => src.ParkingSpace.SpotNumber))
                .ForMember(dest => dest.ParkingName, opt => opt.MapFrom(src => src.ParkingSpace.Parking.Name))
                .ForMember(dest => dest.ParkingAddress, opt => opt.MapFrom(src => src.ParkingSpace.Parking.Address))
                .ForMember(dest => dest.HourlyRate, opt => opt.MapFrom(src => src.ParkingSpace.BaseHourlyRate))
                .ForMember(dest => dest.CheckInTime, opt => opt.MapFrom(src => src.AccessLogs
                    .Where(l => l.ScanType == ScanType.Entry)
                    .OrderByDescending(l => l.ScanTimestamp)
                    .Select(l => (DateTime?)l.ScanTimestamp)
                    .FirstOrDefault()));
            CreateMap<Parking, ParkingDTO>().ReverseMap();
            CreateMap<Parking, ParkingResponseDTO>()
                .ForMember(dest => dest.Features, opt => opt.MapFrom(src => src.Features.Select(f => f.ToString()).ToList()))
                .ForMember(dest => dest.IsOpenNow, opt => opt.MapFrom(src => GeoHelper.IsOpenAt(src.OperatingHours, DateTime.UtcNow)))
                .ForMember(dest => dest.TotalSpaces, opt => opt.MapFrom(src => src.ParkingSpaces != null ? src.ParkingSpaces.Count(s => s.IsActive) : 0))
                .ForMember(dest => dest.MinHourlyRate, opt => opt.MapFrom(src => src.ParkingSpaces != null && src.ParkingSpaces.Any(s => s.IsActive) ? src.ParkingSpaces.Where(s => s.IsActive).Min(s => (decimal?)s.BaseHourlyRate) : null))
                .ForMember(dest => dest.AvailableSpaces, opt => opt.Ignore())
                .ForMember(dest => dest.DistanceKm, opt => opt.Ignore())
                .ForMember(dest => dest.RecommendationReason, opt => opt.Ignore());
            CreateMap<CreateParkingDTO, Parking>();
            CreateMap<UpdateParkingDTO, Parking>();
            CreateMap<CreateParkingSpaceDTO, ParkingSpace>();
            CreateMap<UpdateParkingSpaceDTO, ParkingSpace>();
            CreateMap<ParkingSpace, ParkingSpaceResponseDTO>()
                .ForMember(dest => dest.ParkingName, opt => opt.MapFrom(src => src.Parking.Name));
            CreateMap<Review, ReviewResponseDTO>();
            CreateMap<AppUser, ProfileDTO>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FullName.Contains(" ") ? src.FullName.Substring(0, src.FullName.IndexOf(" ")) : src.FullName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.FullName.Contains(" ") ? src.FullName.Substring(src.FullName.IndexOf(" ") + 1) : string.Empty))
                .ReverseMap();
            CreateMap<AppUser, LoginResponseDTO>();
            CreateMap<SavedPlace, SavedPlaceResponseDTO>();
            CreateMap<CreateSavedPlaceDTO, SavedPlace>();
            CreateMap<UpdateSavedPlaceDTO, SavedPlace>();
        }
    }
}