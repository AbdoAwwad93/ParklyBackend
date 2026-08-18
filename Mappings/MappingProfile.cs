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

            CreateMap<Reservation, ReservationResponseDTO>()
                .ForMember(dest => dest.ParkingId, opt => opt.MapFrom(src => src.ParkingSpace.ParkingId))
                .ForMember(dest => dest.SpotNumber, opt => opt.MapFrom(src => src.ParkingSpace.SpotNumber));
            CreateMap<Parking, ParkingDTO>().ReverseMap();
            CreateMap<Review, ReviewDTO>().ReverseMap();
            CreateMap<AppUser, ProfileDTO>().ReverseMap();
            CreateMap<AppUser, LoginResponseDTO>();
        }
    }
}