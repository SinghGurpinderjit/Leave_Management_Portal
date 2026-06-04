namespace LeaveManagementService.Extensions;

public static class EmployeeHelperExtensions
{
    public static bool IsTeamMember(this Guid employeeId, List<Guid> teamMembers)
    {
        return teamMembers.Contains(employeeId);
    }
}
