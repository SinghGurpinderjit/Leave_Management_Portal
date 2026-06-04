using Shared.Common.Models.LeaveManagementService;
namespace LeaveManagementService.Repositories
{
    public interface ILeaveTypeRepository
    {
        Task<LeaveType?> GetByIdAsync(int id);
        Task<IEnumerable<LeaveType>> GetAllAsync();
        Task<LeaveType> CreateAsync(LeaveType leaveType);
        Task<LeaveType?> UpdateAsync(int id, LeaveType leaveType);
    }
}