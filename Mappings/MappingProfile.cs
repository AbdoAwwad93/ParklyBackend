using AutoMapper;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;

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
                .ForMember(dest => dest.SpotNumber, opt => opt.MapFrom(src => src.ParkingSpace.SpotNumber));
            CreateMap<Parking, ParkingDTO>().ReverseMap();
            CreateMap<Parking, ParkingResponseDTO>();
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