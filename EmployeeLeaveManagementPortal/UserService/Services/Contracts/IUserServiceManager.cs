namespace UserService.Services.Contracts
{
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Employee;

    public interface IUserServiceManager
    {
        /// <summary>
        /// Validates credentials and returns a signed JWT + user info on success.
        /// Returns 401 Unauthorized on invalid email or password.
        /// Never reveals which field is wrong.
        /// </summary>
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Returns all users in the system.
        /// Intended for Manager / Admin use only — enforce via controller policy.
        /// </summary>
        Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersInfoAsync();

        /// <summary>
        /// Returns a single user by ID.
        /// Returns 404 NotFound when no user exists with that ID.
        /// </summary>
        Task<ApiResponse<UserDto>> GetUserInfoByIdAsync(Guid userId);

        /// <summary>
        /// Returns all direct and indirect team members under the given user (recursive).
        /// Used by the logged-in user to see their own team.
        /// Returns 404 when the user does not exist.
        /// </summary>
        Task<ApiResponse<TeamMembersDto>> GetTeamMembersByUserIdAsync(Guid managerId);

        /// <summary>
        /// Returns team members for another user, enforcing hierarchy access rules:
        ///   - A user can always view their own team.
        ///   - A user can view another user's team only if that user is within
        ///     their own hierarchy (i.e. they are a manager of that user).
        /// Returns 403 Forbidden when access is denied.
        /// Returns 404 when either user does not exist.
        /// </summary>
        Task<ApiResponse<TeamMembersDto>> GetTeamMembersOfOtherUserAsync(Guid loggedInUserId, Guid targetUserId);

        /// <summary>
        /// Creates a new user and publishes a UserCreated event so LeaveManagementService
        /// can initialize default leave balances (Casual: 12, Sick: 10, Privilege: 15).
        /// Returns 409 Conflict when the email is already taken.
        /// Returns 400 BadRequest when the ManagerId or RoleIds are invalid.
        /// </summary>
        Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);

    }
}