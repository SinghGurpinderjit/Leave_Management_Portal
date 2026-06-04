using Microsoft.EntityFrameworkCore;
using Shared.Common.Constants;
using Shared.Common.Models.EmployeeService;

namespace UserService.Data
{
    public class EmployeeDbContext : DbContext
    {
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        //public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureRole(modelBuilder);

            SeedData(modelBuilder);
        }


        private void SeedData(ModelBuilder modelBuilder)
        {
            var employeeRoleId = new Guid("2c99e6d8-f765-4ee3-97f9-1fd0b80dec38");
            var managerRoleId = new Guid("a7d6caa3-22a7-4b1a-a70d-0866d73c4a7a");

            var seniorManagerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var managerId =       Guid.Parse("22222222-2222-2222-2222-222222222222");

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = employeeRoleId, Name = AppConstants.UserRole.Employee },
                new Role { Id = managerRoleId, Name = AppConstants.UserRole.Manager }
                //new Role { Id = new Guid("a7d6caa3-22a1-4b1a-a70d-0866d73c4a7a"), Name = AppConstants.UserRole.Admin }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = seniorManagerId,
                    FirstName = "Senior",
                    LastName = "Manager",
                    Email = "senior.manager@company.com",
                    Password = "Test@123!",
                    ManagerId = null,
                    RoleId = managerRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = managerId,
                    FirstName = "manager",
                    LastName = "1",
                    Email = "manager.1@company.com",
                    Password = "Test@123!",
                    ManagerId = seniorManagerId,
                    RoleId = managerRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    FirstName = "Employee",
                    LastName = "1",
                    Email = "employee.1@company.com",
                    Password = "Test@123!",
                    ManagerId = managerId,
                    RoleId = employeeRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    FirstName = "Employee",
                    LastName = "2",
                    Email = "employee.2@company.com",
                    Password = "Test@123!",
                    ManagerId = managerId,
                    RoleId = employeeRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    FirstName = "Employee",
                    LastName = "3",
                    Email = "employee.3@company.com",
                    Password = "Test@123!",
                    ManagerId = managerId,
                    RoleId = employeeRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    FirstName = "Employee",
                    LastName = "4",
                    Email = "employee.4@company.com",
                    Password = "Test@123!",
                    ManagerId = seniorManagerId,
                    RoleId = employeeRoleId,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    FirstName = "Employee",
                    LastName = "5",
                    Email = "employee.5@company.com",
                    Password = "Test@123!",
                    ManagerId = seniorManagerId,
                    RoleId = employeeRoleId,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired();

                // User -> Role (many-to-one)
                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Self-referencing: User -> Manager
                entity.HasOne(e => e.Manager)
                    .WithMany()
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });
        }

        private void ConfigureRole(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Name).IsUnique();
                entity.Property(x => x.Name).IsRequired().HasMaxLength(50);
            });
        }
    }
}