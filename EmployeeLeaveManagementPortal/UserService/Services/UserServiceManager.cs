using AutoMapper;
using Microsoft.Extensions.Options;
using Shared.Common.Configuration;
using Shared.Common.DTOs;
using Shared.Common.Messaging;
using System.Net;
using Shared.Common.DTOs.Employee;
using UserService.UnitOfWork;
using UserService.Services.Contracts;
using UserService.Repositories.Contracts;
using Shared.Common.Utilities;
using Shared.Common.Models.EmployeeService;
using Shared.Common.Messages;
using UserService.Repositories;
using Shared.Common.Constants;

namespace EmployeeService.Services;

public class UserServiceManager : IUserServiceManager
{
    private readonly ILogger<UserServiceManager> _logger;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IEmployeeHierarchyService _employeeHierarchyService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RabbitMQSettings _settings;
    private readonly string _instanceId = Environment.MachineName;
    private readonly JwtTokenGenerator _tokenGenerator;

    public UserServiceManager(
        ILogger<UserServiceManager> logger,
        IMapper mapper,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEmployeeHierarchyService employeeHierarchyService,
        IMessagePublisher messagePublisher,
        IOptions<RabbitMQSettings> settings,
        JwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _mapper = mapper;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _employeeHierarchyService = employeeHierarchyService;
        _messagePublisher = messagePublisher;
        _tokenGenerator = tokenGenerator;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginDto loginDto)
    {

        _logger.LogInformation("[Instance: {InstanceId}] Login attempt for email: {Email}", _instanceId, loginDto.Email);

        var user = await _userRepository.GetByEmailAsync(loginDto.Email);

        if (user == null || user.Password != loginDto.Password)
        {
            _logger.LogInformation("[Instance: {InstanceId}] Failed login attempt for email: {Email}", _instanceId, loginDto.Email);

            return ApiResponse<LoginResponseDto>.BadRequest("Invalid credentials");
        }

        var token = _tokenGenerator.GenerateToken(user.Id, user.Email, user.Role.Name);

        _logger.LogInformation("[Instance: {InstanceId}] Successful login attempt for email: {Email}", _instanceId, loginDto.Email);

        var userDto = _mapper.Map<UserSummaryDto>(user);
        var response = new LoginResponseDto()
        {
            Token = token,
            User = userDto
        };

        return ApiResponse<LoginResponseDto>.Ok(response);
    }
    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersInfoAsync()
    {
        //var response = new ApiResponse<IEnumerable<UserDto>>();

        try
        {
            _logger.LogInformation("[Instance: {InstanceId}] Fetching users", _instanceId);

            var users = await _userRepository.GetAllAsync();
            if (!users.Any())
            {
                _logger.LogWarning("[Instance: {InstanceId}] Users not found", _instanceId);
                return ApiResponse<IEnumerable<UserDto>>.NotFound($"Users not found");
            }

            var usersDto = users.Select(employee => _mapper.Map<UserDto>(employee));

            _logger.LogInformation("[Instance: {InstanceId}] Users fetched successfuly", _instanceId);

            return ApiResponse<IEnumerable<UserDto>>.Ok(usersDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Instance: {InstanceId}] Some error occured while fetching users", _instanceId);
            return ApiResponse<IEnumerable<UserDto>>.InternalError($"Some error occured");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserInfoByIdAsync(Guid userId)
    {
        //var response = new ApiResponse<UserDto>();

        try
        {
            _logger.LogInformation("[Instance: {InstanceId}] Fetching user by ID: {UserId}", _instanceId, userId);

            //bool hasAccess = await HasAccessToFetchOtherEmployee(employeeId, requestedEmployeeId);
            //if (!hasAccess)
            //{
            //    _logger.LogWarning(
            //        "[Instance: {InstanceId}] Unauthorized team access attempt by LoggedIn Employee: {LoggedInEmployee}, Requested Employee: {EmployeeId}",
            //        _instanceId,
            //        employeeId,
            //        requestedEmployeeId);
            //    return response.Fail(HttpStatusCode.Forbidden, "You are not allowed to access this resource.");
            //}

            var employee = await _userRepository.GetByIdAsync(userId);
            if (employee == null)
            {
                _logger.LogWarning("[Instance: {InstanceId}] User not found: {UserId}", _instanceId, userId);
                return ApiResponse<UserDto>.NotFound($"User not found: {userId}");
            }

            return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(employee));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Instance: {InstanceId}] Some error occured while fetching user : {UserId}", _instanceId, userId);
            return ApiResponse<UserDto>.InternalError($"Some error occured");
        }
    }

    public async Task<ApiResponse<TeamMembersDto>> GetTeamMembersByUserIdAsync(Guid managerId)
    {
        //var response = new ApiResponse<TeamMembersDto>();

        try
        {
            // Validate user exists
            var employee = await _userRepository.GetByIdAsync(managerId);
            if (employee == null)
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] User not found: {UserID}",
                    _instanceId,
                    managerId);

                return ApiResponse<TeamMembersDto>.NotFound($"User not found: {managerId}");
            }

            // Load team members
            var teamMembers = await _employeeHierarchyService.GetAllTeamMembersAsync(managerId);

            var teamMemberDto = new TeamMembersDto
            {
                Id = employee.Id,
                Email = employee.Email,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                TeamMembers = teamMembers
                    .Select(teamMember => _mapper.Map<UserSummaryDto>(teamMember))
                    //new UserSummaryDto
                    //{
                    //    Id = teamMember.Id,
                    //    FirstName = teamMember.FirstName,
                    //    LastName = teamMember.LastName,
                    //    Email = teamMember.Email,
                    //    Role = teamMember.Role.Name
                    //})
                    .ToList()
            };

            return ApiResponse<TeamMembersDto>.Ok(teamMemberDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Instance: {InstanceId}] Some error occured while fetching team members. Logged in UserId: {UserID}",
                _instanceId,
                managerId);
            return ApiResponse<TeamMembersDto>.InternalError("Some error occured while fetching team members");
        }
    }

    public async Task<ApiResponse<TeamMembersDto>> GetTeamMembersOfOtherUserAsync(Guid loggedInUserId, Guid targetUserId)
    {
        //var response = new ApiResponse<TeamMembersDto>();

        try
        {
            // Validate employee exists
            var user = await _userRepository.GetByIdAsync(loggedInUserId);
            var otherUser = await _userRepository.GetByIdAsync(targetUserId);
            if (user == null)
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] User not found: {UserId}",
                    _instanceId,
                    targetUserId);

                return ApiResponse<TeamMembersDto>.NotFound($"User not found: {user}");
            }

            if (otherUser == null)
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] Requested User not found: {UserId}",
                    _instanceId,
                    targetUserId);

                return ApiResponse<TeamMembersDto>.NotFound($"Requested User not found: {targetUserId}");
            }

            // Employee can access:
            // 1. Their own team
            // 2. Teams under their hierarchy

            var hasAccess = await HasAccessToFetchOtherEmployee(loggedInUserId, targetUserId);
            if (!hasAccess)
            {
                _logger.LogWarning(
                    "[Instance: {InstanceId}] Unauthorized team access attempt by Logged in User: {LoggedInUser} for User: {UserId}",
                    _instanceId,
                    loggedInUserId,
                    targetUserId);
                return ApiResponse<TeamMembersDto>.Forbidden("You are not allowed to access this resource.");
            }

            // Load team members
            var teamMembers = await _employeeHierarchyService.GetAllTeamMembersAsync(targetUserId);

            return ApiResponse<TeamMembersDto>.Ok(new TeamMembersDto
            {
                Id = otherUser.Id,
                Email = otherUser.Email,
                FirstName = otherUser.FirstName,
                LastName = otherUser.LastName,
                TeamMembers = teamMembers
                    .Select(teamMember => _mapper.Map<UserSummaryDto>(teamMember))
                    //new UserSummaryDto
                    //{
                    //    Id = teamMember.Id,
                    //    FullName =
                    //        $"{teamMember.FirstName} {teamMember.LastName}",
                    //    Email = teamMember.Email,
                    //    Role = teamMember.Role.Name
                    //})
                    .ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                        "[Instance: {InstanceId}] Some error occured while fetching team members. Logged in UserId: {UserID}, Requested UserId: {OtherUserID}",
                        _instanceId,
                        loggedInUserId,
                        targetUserId);
            return ApiResponse<TeamMembersDto>.InternalError("Some error occured while fetching team members");
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            _logger.LogInformation("[Instance: {InstanceId}] Creating user : {Email}", _instanceId, createUserDto.Email);

            var roleExists = await _roleRepository.GetByIdAsync(createUserDto.RoleId);
            if (roleExists == null)
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] Role Id: {RoleId} not found",
                    _instanceId, createUserDto.RoleId);

                return ApiResponse<UserDto>.NotFound($"Role Id: {createUserDto.RoleId} not found");
            }
            if (createUserDto.ManagerId.HasValue)
            {
                var managerExists = await _userRepository.GetByIdAsync(createUserDto.ManagerId.Value);
                if (managerExists == null)
                {
                    _logger.LogInformation(
                        "[Instance: {InstanceId}] Manager not found {ManagerId} ",
                        _instanceId, createUserDto.ManagerId);

                    return ApiResponse<UserDto>.NotFound($"Manager not found");
                }

                if(!string.Equals(managerExists.Role.Name, AppConstants.UserRole.Manager, StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<UserDto>.BadRequest($"Cannot assign manager, ID: {createUserDto.ManagerId} does not have manager role");
                }
            }

            var userEmailExists = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (userEmailExists != null)
            {
                _logger.LogInformation(
                        "[Instance: {InstanceId}] Email already exists {Email} ",
                        _instanceId, createUserDto.Email);

                return ApiResponse<UserDto>.BadRequest($"Email already exists");
            }
            var user = _mapper.Map<User>(createUserDto);

            await _unitOfWork.BeginTransactionAsync();

            var createdUser = await _userRepository.CreateAsync(user);
            var userDto = _mapper.Map<UserDto>(createdUser);

            try
            {
                await PublishUserCreatedEventAsync(createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Instance: {InstanceId}] Failed to publish leave balance initialization event for user: {Email}, Revoked user creation",
                _instanceId, createUserDto.Email);

                await _unitOfWork.RollbackAsync();

                return ApiResponse<UserDto>.InternalError("Some error occured while publising user created event");
            }

            _logger.LogInformation("[Instance: {InstanceId}] User created succssfully {Email}",
                _instanceId, createUserDto.Email);

            _logger.LogInformation("[Instance: {InstanceId}] Published default leave balance initialization event succssfully for user: {Email}",
                _instanceId, createUserDto.Email);

            await _unitOfWork.CommitAsync();

            return ApiResponse<UserDto>.Created(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                        "[Instance: {InstanceId}] Some error occured while creating user. {User}",
                        _instanceId,
                        createUserDto.Email);

            await _unitOfWork.RollbackAsync();

            return ApiResponse<UserDto>.InternalError("Some error occured while creating user");
        }
    }

    private async Task<bool> HasAccessToFetchOtherEmployee(Guid employeeId, Guid requestedEmployeeId)
    {
        bool isSelf = employeeId == requestedEmployeeId;
        if (!isSelf)
        {
            var isManager = await _employeeHierarchyService.IsManagerOf(employeeId, requestedEmployeeId);
            if (!isManager)
            {
                return false;
            }
        }
        return true;
    }

    private async Task PublishUserCreatedEventAsync(User user)
    {
        try
        {
            var message = new UserCreatedMessage
            {
                EmployeeId = user.Id,
                EmployeeEmail = user.Email,
                ManagerId = user.ManagerId
            };
            await _messagePublisher.PublishAsync(message, _settings.RoutingKey);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
