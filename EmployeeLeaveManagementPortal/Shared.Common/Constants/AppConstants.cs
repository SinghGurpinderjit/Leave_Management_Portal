using OpenTelemetry.Trace;

namespace Shared.Common.Constants;

public struct AppConstants
{
    public struct UserRole
    {
        public const string Employee = "Employee";
        public const string Manager = "Manager";
    }

    public struct CustomJwtPolicy
    {
        public const string EmployeeOnly = "EmployeeOnly";
        public const string ManagerOnly = "ManagerOnly";
    }

    public const string jwtCustomUserRoleClaimName = "user_role";

    public struct LeaveTypes
    {
        public const string SickLeave = "Sick Leave";
        public const string CasualLeave = "Casual Leave";
        public const string PrivilegeLeave = "Privilege Leave";
    }

    public static IDictionary<string, int> LeavesDefaultAllocation = new Dictionary<string, int>()
    {
        {LeaveTypes.CasualLeave, 12},
        {LeaveTypes.SickLeave, 10},
        {LeaveTypes.PrivilegeLeave, 15}
    };

    public struct ExceptionMessages
    {
        public const string ExceptionErrorMessage = "Exception Message: {Message}";
    }
}
