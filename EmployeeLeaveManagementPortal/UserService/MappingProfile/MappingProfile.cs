namespace Shared.Common.MappingProfile
{
    using AutoMapper;
    using Shared.Common.DTOs.Employee;
    using Shared.Common.Messages;
    using Shared.Common.Models.EmployeeService;

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserRolesInfoDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name));

            CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.Manager, opt => opt.MapFrom(src => src.Manager != null ?
                new UserSummaryDto
                {
                    Id = src.Manager.Id,
                    FirstName = src.Manager.FirstName,
                    LastName = src.Manager.LastName,
                    Email = src.Manager.Email,
                    Role = src.Manager.Role != null ? src.Manager.Role.Name : string.Empty
                } : null)
            );

            CreateMap<User, UserSummaryDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Name));

            CreateMap<CreateUserDto, User>();

            CreateMap<UpsertEmployeeDto, User>();

            CreateMap<User, UserCreatedMessage>()
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ManagerId, opt => opt.MapFrom(src => src.ManagerId));

            CreateMap<Role, RoleDto>();
            CreateMap<CreateRoleDto, Role>();

        }
    }
}
