namespace LeaveManagementService.Repositories
{
    using LeaveManagementService.Data;
    using Microsoft.EntityFrameworkCore;
    using Shared.Common.Models.LeaveManagementService;

    public class LeaveRequestRepository : ILeaveRequestRepository
    {
        private readonly LeavesMgmtDbContext _context;

        public LeaveRequestRepository(LeavesMgmtDbContext context)
        {
            _context = context;
        }

        public async Task<LeaveRequest?> GetByIdAsync(Guid id)
        {
            return await _context.LeaveRequests
                                 .Include(x => x.LeaveType)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.LeaveRequests
                                 .Include(x => x.LeaveType)
                                 .AsNoTracking()
                                 .Where(x => x.EmployeeId == employeeId)
                                 .OrderByDescending(x => x.CreatedAt)
                                 .ToListAsync();
        }

        // Used by manager to fetch all requests belonging to a set of employee IDs (their team)
        public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdsAsync(IEnumerable<Guid> employeeIds)
        {
            return await _context.LeaveRequests
                                 .Include(x => x.LeaveType)
                                 .AsNoTracking()
                                 .Where(x => employeeIds.Contains(x.EmployeeId))
                                 .OrderByDescending(x => x.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<bool> HasOverlappingRequestAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
        {
            return await _context.LeaveRequests
                .AsNoTracking()
                .AnyAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.Id != excludeRequestId && // exclude self on updates
                    x.Status != LeaveStatus.Rejected &&
                    x.Status != LeaveStatus.Cancelled &&
                    startDate <= x.EndDate &&
                    endDate >= x.StartDate);
        }

        public async Task<LeaveRequest> AddAsync(LeaveRequest leaveRequest)
        {
            leaveRequest.Id = Guid.NewGuid();
            leaveRequest.Status = LeaveStatus.Pending;
            leaveRequest.ApplicationReason = leaveRequest.ApplicationReason;
            leaveRequest.CreatedAt = DateTime.UtcNow;

            await _context.LeaveRequests.AddAsync(leaveRequest);
            await SaveChangesAsync();

            // Re-fetch with includes so the caller gets the full entity
            return (await _context.LeaveRequests
                .Include(x => x.LeaveType)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == leaveRequest.Id))!;
        }

        private async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Updates only the Status and Reason fields; all other fields are immutable after creation
        public async Task<LeaveRequest?> UpdateStatusAsync(Guid id, LeaveStatus status, string? applicationReason, string? reviewReason, Guid? updatedBy = null)
        {
            var existing = await _context.LeaveRequests
                                                     .Include(x => x.LeaveType)
                                                     .FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
                return null;

            existing.Status = status;
            existing.ApplicationReason = applicationReason;
            existing.ReviewReason = reviewReason;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = updatedBy;

            await SaveChangesAsync();

            return existing;
        }
    }
}
