using Shared.Common.Models.EmployeeService;
using UserService.Repositories.Contracts;
using UserService.Services.Contracts;

namespace UserService.Services;

public class EmployeeHierarchyService : IEmployeeHierarchyService
{
    private readonly ILogger<EmployeeHierarchyService> _logger;
    private readonly IUserRepository _userRepository;

    public EmployeeHierarchyService(ILogger<EmployeeHierarchyService> logger, IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task<bool> IsManagerOf(Guid managerId, Guid employeeId)
    {
        var teamMemberIds = await GetAllTeamMemberIdsAsync(managerId);
        return teamMemberIds.Contains(employeeId);
    }

    public async Task<IEnumerable<User>> GetAllTeamMembersAsync(Guid managerId)
    {
        var result = new Dictionary<Guid, User>();

        await LoadTeamMembersRecursivelyAsync(managerId, result);

        return result.Select(x => x.Value).ToList();
    }


    public async Task<IEnumerable<Guid>> GetAllTeamMemberIdsAsync(Guid managerId)
    {
        var result = new Dictionary<Guid, User>();

        await LoadTeamMembersRecursivelyAsync(managerId, result);

        return result.Keys.ToList();
    }

    private async Task LoadTeamMembersRecursivelyAsync(Guid managerId, Dictionary<Guid, User> result)
    {
        var teamMembers = await _userRepository.GetByManagerIdAsync(managerId);

        foreach (var teamMember in teamMembers)
        {
            // Prevent duplicates/infinite loops
            if (!result.ContainsKey(teamMember.Id))
            {
                result.Add(teamMember.Id, teamMember);
                await LoadTeamMembersRecursivelyAsync(teamMember.Id, result);
            }
        }
    }
}
