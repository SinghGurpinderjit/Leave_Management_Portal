namespace UserService.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using Shared.Common.Models.EmployeeService;
    using UserService.Data;
    using UserService.Repositories.Contracts;

    public class EmployeeRepository : IUserRepository
    {
        private readonly EmployeeDbContext _context;

        public EmployeeRepository(EmployeeDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                                 .Include(x => x.Manager)
                                 .Include(x => x.Role)
                                 //.ThenInclude(x => x.Role)
                                 .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                                  .Include(x => x.Manager)
                                  .Include(x => x.Role)
                                  //.ThenInclude(x => x.Role)
                                  .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                                 .Include(x => x.Role)
                                 .Include(x => x.Manager)
                                     .ThenInclude(m => m!.Role)
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetByManagerIdAsync(Guid managerId)
        {
            return await _context.Users
                                .Include(x => x.Role)
                                .AsNoTracking()
                                .Where(x => x.ManagerId == managerId)
                                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _context.Users
                           .Include(x => x.Role)
                           .Include(x => x.Manager)
                           .ThenInclude(x => x.Role)
                           .AsNoTracking()
                           .FirstOrDefault(x => user.Id == x.Id)!;
        }

        public async Task<User?> UpdateAsync(Guid id, User user)
        {
            var existingUser = await _context.Users
                                             .Include(x => x.Role)
                                             .FirstOrDefaultAsync(x => x.Id == id);
            if (existingUser == null)
                return null;

            existingUser.Email = user.Email;
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(user.Password))
            {
                existingUser.Password = user.Password;
            }

            if (user.ManagerId != null)
            {
                existingUser.ManagerId = user.ManagerId;
            }

            await _context.SaveChangesAsync();
            return existingUser;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
