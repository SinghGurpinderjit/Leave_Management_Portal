namespace LeaveManagementService.Services.Contracts
{
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Leaves;

    public interface ILeaveBalanceService
    {
        Task<ApiResponse<IEnumerable<LeaveBalanceDto>>> AddDefaultLeaveBalanceForUser(Guid userId);

        Task<ApiResponse<IEnumerable<LeaveBalanceDto>>> GetByEmployeeIdAndLeaveTypeIdAsync(
            Guid loggedInUserId, int? leaveTypeId);

        Task<ApiResponse<IEnumerable<LeaveBalanceDto>>> GetOtherEmployeeLeaveBalancesByLeaveTypeIdAsync(
            Guid loggedInUserId, Guid requestedEmployeeId, int? leaveTypeId);
    }
}
