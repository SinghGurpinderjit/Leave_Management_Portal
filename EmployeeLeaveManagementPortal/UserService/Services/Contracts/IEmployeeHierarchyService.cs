using Shared.Common.Models.EmployeeService;

namespace UserService.Services.Contracts
{
    public interface IEmployeeHierarchyService
    {
        Task<bool> IsManagerOf(Guid managerId, Guid employeeId);
        Task<IEnumerable<Guid>> GetAllTeamMemberIdsAsync(Guid managerId);
        Task<IEnumerable<User>> GetAllTeamMembersAsync(Guid managerId);
    }
}