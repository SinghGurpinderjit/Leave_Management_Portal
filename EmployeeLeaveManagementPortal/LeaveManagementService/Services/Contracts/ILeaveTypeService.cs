namespace LeaveManagementService.Services.Contracts
{
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Leaves;

    public interface ILeaveTypeService
    {
        Task<ApiResponse<IEnumerable<LeaveTypeDto>>> GetAll();

        Task<ApiResponse<LeaveTypeDto>> GetById(int leaveTypeId);
    }
}
