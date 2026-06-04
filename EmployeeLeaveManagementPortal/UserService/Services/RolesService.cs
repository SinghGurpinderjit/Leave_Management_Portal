using System.Net;
using AutoMapper;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Employee;
using Shared.Common.Models.EmployeeService;
using UserService.Repositories.Contracts;
using UserService.Services.Contracts;

namespace UserService.Services;

public class RolesService : IRolesService
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RolesService> _logger;
    private readonly IMapper _mapper;
    private readonly string _instanceId = Environment.MachineName;

    public RolesService(IRoleRepository roleRepository, ILogger<RolesService> logger, IMapper mapper)
    {
        _roleRepository = roleRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<RoleDto>>> GetAllAsync()
    {
        _logger.LogInformation("[Instance: {InstanceId}] Fetching all roles", _instanceId);

        var response = new ApiResponse<IEnumerable<RoleDto>>();
        var roles = await _roleRepository.GetAllAsync();

        _logger.LogInformation(
            "[Instance: {InstanceId}] Fetched {Count} roles",
            _instanceId,
            roles.Count());

        return response.Success(roles.Select(ToDto), HttpStatusCode.OK);
    }

    public async Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] Fetching role: {RoleId}",
            _instanceId,
            id);

        //var response = new ApiResponse<RoleDto>();

        var role = await _roleRepository.GetByIdAsync(id);
        if (role is null)
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Role not found: {RoleId}",
                _instanceId,
                id);

            return ApiResponse<RoleDto>.NotFound($"Role with ID {id} does not exist");
        }

        _logger.LogInformation(
            "[Instance: {InstanceId}] Role fetched successfully: {RoleId}",
            _instanceId,
            id);

        return ApiResponse<RoleDto>.Ok(ToDto(role));
    }

    public async Task<ApiResponse<RoleDto>> CreateAsync(CreateRoleDto dto)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] Creating role with name: {RoleName}",
            _instanceId,
            dto.Name);

        //var response = new ApiResponse<RoleDto>();

        var existing = await _roleRepository.GetByNameAsync(dto.Name);
        if (existing is not null)
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Role already exists with name: {RoleName}",
                _instanceId,
                dto.Name);

            return ApiResponse<RoleDto>.Conflict($"Role '{dto.Name}' already exists");
        }

        var created = await _roleRepository.CreateAsync(new Role { Name = dto.Name });

        _logger.LogInformation(
            "[Instance: {InstanceId}] Role created successfully: {RoleId} - {RoleName}",
            _instanceId,
            created.Id,
            created.Name);

        return ApiResponse<RoleDto>.Created(ToDto(created));
    }

    public async Task<ApiResponse<IEnumerable<RoleDto>>> CreateRangeAsync(List<CreateRoleDto> dtos)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] Bulk creating {Count} roles",
            _instanceId,
            dtos.Count);

        //var response = new ApiResponse<IEnumerable<RoleDto>>();

        // Check for duplicates within the request itself
        var duplicatesInRequest = dtos
            .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatesInRequest.Any())
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Duplicate role names in bulk request: {Names}",
                _instanceId,
                string.Join(", ", duplicatesInRequest));

            return ApiResponse<IEnumerable<RoleDto>>.BadRequest(
                $"Duplicate role names in request: {string.Join(", ", duplicatesInRequest)}");
        }

        // Check for conflicts against existing roles in DB
        var allExisting = await _roleRepository.GetAllAsync();
        var existingNames = allExisting
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var conflicting = dtos
            .Where(d => existingNames.Contains(d.Name))
            .Select(d => d.Name)
            .ToList();

        if (conflicting.Any())
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Roles already exist: {Names}",
                _instanceId,
                string.Join(", ", conflicting));

            return ApiResponse<IEnumerable<RoleDto>>.Conflict(
                $"Roles already exist: {string.Join(", ", conflicting)}");
        }

        var roles = dtos.Select(d => new Role { Name = d.Name }).ToList();
        var created = await _roleRepository.CreateRangeAsync(roles);

        _logger.LogInformation(
            "[Instance: {InstanceId}] Bulk created {Count} roles successfully",
            _instanceId,
            created.Count());

        return ApiResponse<IEnumerable<RoleDto>>.Created(created.Select(ToDto));
    }

    public async Task<ApiResponse<RoleDto>> UpdateAsync(Guid id, UpdateRoleDto dto)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] Updating role: {RoleId}",
            _instanceId,
            id);

        //var response = new ApiResponse<RoleDto>();

        // Check the target role exists
        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Update failed — role not found: {RoleId}",
                _instanceId,
                id);

            return ApiResponse<RoleDto>.NotFound($"Role with ID {id} does not exist");
        }

        // Check the new name isn't already taken by a different role
        var nameConflict = await _roleRepository.GetByNameAsync(dto.Name);
        if (nameConflict is not null && nameConflict.Id != id)
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Update failed — role name already taken: {RoleName}",
                _instanceId,
                dto.Name);

            return ApiResponse<RoleDto>.Conflict($"Role '{dto.Name}' already exists");
        }

        var updated = await _roleRepository.UpdateAsync(id, new Role { Name = dto.Name });

        _logger.LogInformation(
            "[Instance: {InstanceId}] Role updated successfully: {RoleId}",
            _instanceId,
            id);

        return ApiResponse<RoleDto>.Ok(ToDto(updated!));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] Deleting role: {RoleId}",
            _instanceId,
            id);

        //var response = new ApiResponse<bool>();

        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning(
                "[Instance: {InstanceId}] Delete failed — role not found: {RoleId}",
                _instanceId,
                id);

            return ApiResponse<bool>.NotFound($"Role with ID {id} does not exist");
        }

        await _roleRepository.DeleteAsync(id);

        _logger.LogInformation(
            "[Instance: {InstanceId}] Role deleted successfully: {RoleId}",
            _instanceId,
            id);

        return ApiResponse<bool>.NoContent();
    }

    private RoleDto ToDto(Role role) => _mapper.Map<RoleDto>(role);
}