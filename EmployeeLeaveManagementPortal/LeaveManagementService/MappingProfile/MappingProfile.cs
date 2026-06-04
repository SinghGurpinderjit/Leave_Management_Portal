using AutoMapper;
using Shared.Common.DTOs.Leaves;
using Shared.Common.Models.LeaveManagementService;

namespace LeaveManagementService.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<LeaveType, LeaveTypeDto>();

            CreateMap<LeaveBalance, LeaveBalanceDto>()
                .ForMember(
                    dest => dest.LeaveType,
                    opt => opt.MapFrom(src =>
                        src.LeaveType != null ? src.LeaveType!.Name : string.Empty)
                )
                .ForMember(dest => dest.TotalAllocated,
                    opt => opt.MapFrom(src =>
                        src.LeaveType != null ? src.LeaveType!.DefaultAllocation : default)
                )
                .ForMember(dest => dest.UsedLeaves,
                    opt => opt.MapFrom(src =>
                        src.LeaveType != null ? src.UsedLeaves : default)
                )
                .ForMember(dest => dest.RemainingLeaves,
                    opt => opt.MapFrom(src =>
                        src.LeaveType != null ? (src.LeaveType!.DefaultAllocation - src.UsedLeaves) : default)
                );
        }
    }
}
