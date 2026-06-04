namespace LeaveManagementService.Services.Contracts
{
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Leaves;

    public interface ILeaveRequestService
    {
        //Task<ApiResponse<PaginatedResult<LeaveRequestDto>>> GetMyLeaveRequestHistoryAsync(
        //    Guid loggedinUserId, Guid? leaveRequestId, LeaveRequestHistoryQuery query);

        ////Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetByEmployeeIdAsync(Guid userId);
        ////Task<ApiResponse<LeaveRequestDto?>> GetLeaveRequestByIdAsync(Guid userId);
        ////Task<ApiResponse<LeaveRequestDto?>> GetLeaveRequestByIdAndEmployeeIdAsync(Guid id, Guid userId);
        //Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetTeamLeaveRequestsAsync(Guid managerId, Guid? leaveRequestId = null);

        ////Task<ApiResponse<LeaveRequestDto>> GetTeamLeaveRequestByIdAsync(Guid id, Guid managerId);
        //Task<ApiResponse<LeaveRequestDto>> ApplyLeaveAsync(Guid loggedInUserId, CreateLeaveRequestDto request);
        //Task<ApiResponse<LeaveRequestDto>> CancelLeaveRequestAsync(Guid leaveRequestId, Guid loggedInUserId, string? reason);
        //Task<ApiResponse<LeaveRequestDto>> SetLeaveRequestStatusAsync(Guid id, Guid managerId, LeaveStatus status, string? reason);

        // ============ Employee: Only =================

        // Employee: apply for leave 
        Task<ApiResponse<LeaveRequestDto>> ApplyLeaveAsync(Guid loggedInUserId, CreateLeaveRequestDto request);

        // Employee: view history 
        Task<ApiResponse<PaginatedResult<LeaveRequestDto>>> GetMyLeaveRequestHistoryAsync(Guid loggedInUserId, LeaveRequestHistoryQuery query);

        // Employee: get single request
        Task<ApiResponse<LeaveRequestDto>> GetLeaveRequestByIdAsync(Guid leaveRequestId, Guid loggedInUserId);

        // Employee: cancel request 
        Task<ApiResponse<LeaveRequestDto>> CancelLeaveRequestAsync(Guid leaveRequestId, Guid loggedInUserId, string? reason);


        // ============ Manager Only ====================

        // Manager: approve or reject 
        Task<ApiResponse<TeamLeaveRequestSummaryDto>> SetLeaveRequestStatusAsync(Guid leaveRequestId, Guid managerId, string status, string? reason);

        // Manager: get team requests 
        Task<ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>> GetTeamLeaveRequestsAsync(Guid managerId, TeamLeaveRequestQuery query);

        // Manager: get single team request 
        Task<ApiResponse<TeamLeaveRequestSummaryDto>> GetTeamLeaveRequestByIdAsync(Guid leaveRequestId, Guid managerId);
    }
}