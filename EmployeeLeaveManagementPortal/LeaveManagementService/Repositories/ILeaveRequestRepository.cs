namespace LeaveManagementService.Repositories
{
    using Shared.Common.Models.LeaveManagementService;

    public interface ILeaveRequestRepository
    {
        Task<LeaveRequest?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId);
        Task<IEnumerable<LeaveRequest>> GetByEmployeeIdsAsync(IEnumerable<Guid> employeeIds);
        Task<bool> HasOverlappingRequestAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null);
        Task<LeaveRequest> AddAsync(LeaveRequest leaveRequest);
        Task<LeaveRequest?> UpdateStatusAsync(Guid id, LeaveStatus status, string? applicationReason, string? reviewReason, Guid? updatedBy = null);
    }
}