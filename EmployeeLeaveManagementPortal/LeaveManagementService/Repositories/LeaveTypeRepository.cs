using LeaveManagementService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Models.LeaveManagementService;

namespace LeaveManagementService.Repositories
{
    public class LeaveTypeRepository : ILeaveTypeRepository
    {
        private readonly LeavesMgmtDbContext _context;

        public LeaveTypeRepository(LeavesMgmtDbContext context)
        {
            _context = context;
        }

        public async Task<LeaveType?> GetByIdAsync(int id)
        {
            return await _context.LeaveTypes
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<LeaveType>> GetAllAsync()
        {
            return await _context.LeaveTypes.AsNoTracking().ToListAsync();
        }

        public async Task<LeaveType> CreateAsync(LeaveType leaveType)
        {
            _context.LeaveTypes.Add(leaveType);
            await _context.SaveChangesAsync();

            return leaveType;
        }

        public async Task<LeaveType?> UpdateAsync(int id, LeaveType leaveType)
        {
            var existingLeaveType = await _context.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id);
            if (existingLeaveType == null)
                return null;

            existingLeaveType.Name = leaveType.Name;
            existingLeaveType.DefaultAllocation = leaveType.DefaultAllocation;
            existingLeaveType.Description = leaveType.Description;

            _context.LeaveTypes.Add(existingLeaveType);
            await _context.SaveChangesAsync();

            return existingLeaveType;
        }
    }
}
