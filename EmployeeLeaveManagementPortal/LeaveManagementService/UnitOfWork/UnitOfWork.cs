using LeaveManagementService.Data;
using LeaveManagementService.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeaveManagementService.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LeavesMgmtDbContext _context;
        private IDbContextTransaction? _transaction;

        public ILeaveRequestRepository LeaveRequests { get; }
        public ILeaveBalanceRepository LeaveBalances { get; }

        public UnitOfWork(
            LeavesMgmtDbContext context,
            ILeaveRequestRepository leaveRequests,
            ILeaveBalanceRepository leaveBalances)
        {
            _context = context;
            LeaveRequests = leaveRequests;
            LeaveBalances = leaveBalances;
        }

        public async Task BeginTransactionAsync()
            => _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
            await _transaction!.CommitAsync();
        }

        public async Task RollbackAsync()
            => await _transaction!.RollbackAsync();

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public async ValueTask DisposeAsync()
        {
            if (_transaction is not null)
                await _transaction.DisposeAsync();

            await _context.DisposeAsync();
        }
    }
}
