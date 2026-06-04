using LeaveManagementService.Repositories;

namespace LeaveManagementService.UnitOfWork
{
    public interface IUnitOfWork
    {
        ILeaveRequestRepository LeaveRequests { get; }
        ILeaveBalanceRepository LeaveBalances { get; }
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task SaveChangesAsync();
    }
}
