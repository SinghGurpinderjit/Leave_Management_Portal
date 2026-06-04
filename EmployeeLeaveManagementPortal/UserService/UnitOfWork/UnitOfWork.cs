using Microsoft.EntityFrameworkCore.Storage;
using UserService.Data;

namespace UserService.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EmployeeDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(EmployeeDbContext context)
        {
            _context = context;
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
