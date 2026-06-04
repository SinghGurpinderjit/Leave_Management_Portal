using Shared.Common.Models.EmployeeService;

namespace UserService.Repositories.Contracts
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<IEnumerable<User>> GetByManagerIdAsync(Guid managerId);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(Guid id, User user);
        Task<bool> DeleteAsync(Guid id);
    }
}
