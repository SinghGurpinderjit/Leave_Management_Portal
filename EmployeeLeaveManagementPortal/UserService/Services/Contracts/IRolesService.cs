using Shared.Common.DTOs;
using Shared.Common.DTOs.Employee;

namespace UserService.Services.Contracts;

public interface IRolesService
{
    Task<ApiResponse<IEnumerable<RoleDto>>> GetAllAsync();

    Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id);

    Task<ApiResponse<RoleDto>> CreateAsync(CreateRoleDto dto);

    Task<ApiResponse<IEnumerable<RoleDto>>> CreateRangeAsync(List<CreateRoleDto> dtos);

    Task<ApiResponse<RoleDto>> UpdateAsync(Guid id, UpdateRoleDto dto);

    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}