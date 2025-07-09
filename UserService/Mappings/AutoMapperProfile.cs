using AutoMapper;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.DeletedAt, opt => opt.MapFrom(src => src.DeletedAt));
        
        CreateMap<UserDto, User>();
        CreateMap<UpdateUserDto, User>();
    }
} 