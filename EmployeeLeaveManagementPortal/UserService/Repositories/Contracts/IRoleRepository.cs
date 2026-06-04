namespace UserService.Repositories.Contracts
{
    using Shared.Common.Models.EmployeeService;

    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name);
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role> CreateAsync(Role role);
        Task<IEnumerable<Role>> CreateRangeAsync(List<Role> role);
        Task<Role?> UpdateAsync(Guid id, Role role);
        Task<bool> DeleteAsync(Guid id);
    }
}
