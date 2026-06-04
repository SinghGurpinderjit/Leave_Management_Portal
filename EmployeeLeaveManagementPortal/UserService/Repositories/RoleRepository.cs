namespace UserService.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Common.Models.EmployeeService;
using UserService.Data;
using UserService.Repositories.Contracts;


public class RoleRepository : IRoleRepository
{

    private readonly EmployeeDbContext _context;

    public RoleRepository(EmployeeDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<Role> CreateAsync(Role role)
    {
        role.Id = Guid.NewGuid();

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return role;
    }

    public async Task<IEnumerable<Role>> CreateRangeAsync(List<Role> roles)
    {
        roles.ForEach(role => role.Id = Guid.NewGuid());

        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync();

        return roles;
    }

    public async Task<Role?> UpdateAsync(Guid id, Role role)
    {
        var existingRole = await _context.Roles.FindAsync(id);
        if (existingRole == null)
            return null;

        existingRole.Name = role.Name;

        await _context.SaveChangesAsync();
        return existingRole;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
            return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }
}
