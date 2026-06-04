namespace LeaveManagementService.Repositories
{
    using Shared.Common.Models.LeaveManagementService;

    public interface ILeaveBalanceRepository
    {
        Task<LeaveBalance?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaveBalance>> GetByEmployeeIdAsync(Guid employeeId);
        Task<LeaveBalance?> GetByLeaveTypeIdAsync(int leaveTypeId);
        Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(Guid employeeId, int leaveTypeId);
        Task<IEnumerable<LeaveBalance>> GetAllAsync();
        Task<LeaveBalance> CreateAsync(LeaveBalance leaveBalance);
        Task<IEnumerable<LeaveBalance>> CreateRangeAsync(List<LeaveBalance> leaveTypes);

        Task<LeaveBalance?> UpdateAsync(Guid id, LeaveBalance user);
    }
}