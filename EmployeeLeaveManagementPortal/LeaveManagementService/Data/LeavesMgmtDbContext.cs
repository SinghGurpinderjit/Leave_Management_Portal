using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Common.Constants;
using Shared.Common.Models.LeaveManagementService;

namespace LeaveManagementService.Data;

public class LeavesMgmtDbContext : DbContext
{
    public LeavesMgmtDbContext(DbContextOptions<LeavesMgmtDbContext> options) : base(options)
    {
    }

    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureLeaveType(modelBuilder);
        ConfigureLeaveBalance(modelBuilder);
        ConfigureLeaveRequest(modelBuilder);

        SeedDataForLeaveTypes(modelBuilder);
    }

    private void ConfigureLeaveType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DefaultAllocation).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.Description).HasMaxLength(250);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_LeaveType_DefaultAllocation",
                "\"DefaultAllocation\" >= 0"));
        });
    }

    private void ConfigureLeaveBalance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId }).IsUnique();
            entity.Property(e => e.EmployeeId).IsRequired();
            entity.Property(e => e.LeaveTypeId).IsRequired();
            entity.Property(e => e.UsedLeaves).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasOne<LeaveType>(x => x.LeaveType)
                 .WithMany()
                 .HasForeignKey(e => e.LeaveTypeId)
                 .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                    "CK_LeaveBalance_UsedLeaves",
                    "\"UsedLeaves\" >= 0")
            );
        });
    }

    private void ConfigureLeaveRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.Property(e => e.EmployeeId).IsRequired();
            entity.Property(e => e.LeaveTypeId).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.NumberOfDays).IsRequired();
            entity.Property(e => e.ApplicationReason).HasMaxLength(500);
            entity.Property(e => e.ReviewReason).HasMaxLength(500);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(LeaveStatus.Pending);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);

            entity.HasOne<LeaveType>(x => x.LeaveType)
                  .WithMany()
                  .HasForeignKey(e => e.LeaveTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                    "CK_LeaveRequest_StartDate_EndDate",
                    "\"StartDate\" <= \"EndDate\"")
            );
            entity.ToTable(t => t.HasCheckConstraint(
                    "CK_LeaveRequest_NumberOfDays",
                    "\"NumberOfDays\" > 0")
            );
        });
    }

    private void SeedDataForLeaveTypes(ModelBuilder modelBuilder)
    {
        Log.Information("Seed LeaveTypes data starting...");

        modelBuilder.Entity<LeaveType>().HasData(
            new LeaveType
            {
                Id = 1,
                Name = AppConstants.LeaveTypes.SickLeave,
                DefaultAllocation = AppConstants.LeavesDefaultAllocation.TryGetValue(
                                        AppConstants.LeaveTypes.SickLeave, out int value) ? value : 10
            },
            new LeaveType
            {
                Id = 2,
                Name = AppConstants.LeaveTypes.CasualLeave,
                DefaultAllocation = AppConstants.LeavesDefaultAllocation.TryGetValue(
                                        AppConstants.LeaveTypes.CasualLeave, out int value2) ? value2 : 12
            },
            new LeaveType
            {
                Id = 3,
                Name = AppConstants.LeaveTypes.PrivilegeLeave,
                DefaultAllocation = AppConstants.LeavesDefaultAllocation.TryGetValue(
                                        AppConstants.LeaveTypes.PrivilegeLeave, out int value3) ? value3 : 15
            }
        );

        Log.Information("Seed LeaveTypes data completed");
    }
}