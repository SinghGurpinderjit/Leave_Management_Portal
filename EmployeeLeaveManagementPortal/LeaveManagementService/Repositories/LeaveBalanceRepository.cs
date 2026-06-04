namespace LeaveManagementService.Repositories
{
    using LeaveManagementService.Data;
    using Microsoft.EntityFrameworkCore;
    using Shared.Common.Models.LeaveManagementService;

    public class LeaveBalanceRepository : ILeaveBalanceRepository
    {
        private readonly LeavesMgmtDbContext _context;

        public LeaveBalanceRepository(LeavesMgmtDbContext context)
        {
            _context = context;
        }

        public async Task<LeaveBalance?> GetByIdAsync(Guid id)
        {
            return await _context.LeaveBalances
                                 .Include(x => x.LeaveType)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<LeaveBalance>> GetByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.LeaveBalances
                                 .Include(x => x.LeaveType)
                                 .AsNoTracking()
                                 .Where(u => u.EmployeeId == employeeId)
                                 .ToListAsync();
        }

        public async Task<LeaveBalance?> GetByLeaveTypeIdAsync(int leaveTypeId)
        {
            return await _context.LeaveBalances
                                 .Include(x => x.LeaveType)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(u => u.LeaveTypeId == leaveTypeId);
        }

        public async Task<IEnumerable<LeaveBalance>> GetAllAsync()
        {
            return await _context.LeaveBalances.Include(x => x.LeaveType).AsNoTracking().ToListAsync();
        }

        public async Task<LeaveBalance> CreateAsync(LeaveBalance leaveBalance)
        {
            leaveBalance.Id = Guid.NewGuid();
            leaveBalance.CreatedAt = DateTime.UtcNow;

            _context.LeaveBalances.Add(leaveBalance);
            await _context.SaveChangesAsync();

            return leaveBalance;
        }
        public async Task<IEnumerable<LeaveBalance>> CreateRangeAsync(List<LeaveBalance> leaveBalances)
        {
            leaveBalances.ForEach(leaveBalance =>
            {
                leaveBalance.Id = Guid.NewGuid();
                leaveBalance.CreatedAt = DateTime.UtcNow;
            });

            await _context.LeaveBalances.AddRangeAsync(leaveBalances);
            await _context.SaveChangesAsync();

            return leaveBalances;
        }

        public async Task<LeaveBalance?> UpdateAsync(Guid id, LeaveBalance leaveBalance)
        {
            var existingLeaveBalance = await _context.LeaveBalances
                                                     .Include(x => x.LeaveType)
                                                     .FirstOrDefaultAsync(x => x.Id == id);
            if (existingLeaveBalance == null)
                return null;

            existingLeaveBalance.UsedLeaves = leaveBalance.UsedLeaves;
            existingLeaveBalance.UpdatedAt = DateTime.UtcNow;

            _context.LeaveBalances.Update(existingLeaveBalance);
            await _context.SaveChangesAsync();

            return existingLeaveBalance;
        }

        public async Task<LeaveBalance?> GetByEmployeeAndLeaveTypeAsync(Guid employeeId, int leaveTypeId)
        {
            return await _context.LeaveBalances
                                 .Include(l => l.LeaveType)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(l => l.EmployeeId == employeeId && l.LeaveTypeId == leaveTypeId);
        }
    }
}
