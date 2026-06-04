namespace LeaveManagementService.Services.Contracts
{
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Employee;

    public interface IUserServiceClient
    {
        //Task<LoginResponseDto> GenerateToken(LoginDto loginDto);

        Task<UserDto?> GetUserByIdAsync(Guid userId);

        Task<bool> ValidateUserAsync(Guid userId);

        Task<TeamMembersDto?> GetTeamMembersAsync(Guid userId);
    }
}