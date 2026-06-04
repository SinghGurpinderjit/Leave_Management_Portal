using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Authorization.Attributes;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Employee;
using Shared.Common.Utilities;
using System.Security.Claims;
using UserService.Services.Contracts;

namespace UserService.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;
    private readonly IUserServiceManager _userServiceManager;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<CreateUserDto> _createUserDtoValidator;
    private readonly string _instanceId = Environment.MachineName;

    public UsersController(
        ILogger<UsersController> logger,
        IMapper mapper,
        IValidator<LoginDto> loginValidator,
        IValidator<CreateUserDto> createUserDtoValidator,
        IUserServiceManager userServiceManager)
    {
        _logger = logger;
        _mapper = mapper;
        _userServiceManager = userServiceManager;
        _loginValidator = loginValidator;
        _createUserDtoValidator = createUserDtoValidator;
    }



    /// <summary>
    /// Authenticate and receive a JWT token.
    /// POST /api/users/login
    /// Returns 200 with token + user info on success.
    /// Returns 401 on invalid credentials — never reveals which field is wrong.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var validation = await _loginValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        _logger.LogInformation(
            "[Instance: {InstanceId}] Login attempt for email: {Email}",
            _instanceId, dto.Email);

        var result = await _userServiceManager.LoginAsync(dto);

        return ToActionResult(result);
    }

    // ── User read endpoints ───────────────────────────────────────────────────

    /// <summary>
    /// Get all users. Manager or admin only.
    /// GET /api/users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("[Instance: {InstanceId}] Fetching all users", _instanceId);

        var result = await _userServiceManager.GetAllUsersInfoAsync();

        return ToActionResult(result);
    }

    /// <summary>
    /// Get user by ID.
    /// GET /api/users/{id}
    /// Employees can only fetch themselves; managers can fetch any user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ValidateUserClaim]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] Fetching user by ID: {UserId}", _instanceId, id);

        var result = await _userServiceManager.GetUserInfoByIdAsync(id);

        return ToActionResult(result);
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(CreateUserDto createUserDto)
    {
        var validation = await _createUserDtoValidator.ValidateAsync(createUserDto);
        if (!validation.IsValid)
            return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _userServiceManager.CreateUserAsync(createUserDto);

        return ToActionResult(result);
    }

    // ── Team hierarchy endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Get the logged-in user's team members (recursive hierarchy).
    /// GET /api/users/team-members
    /// </summary>
    [HttpGet("team-members")]
    [ValidateUserClaim]
    [ProducesResponseType(typeof(ApiResponse<TeamMembersDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTeamMembers()
    {
        var loggedInUserId = Helpers.GetLoggedInUserId(User);
        if (Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid userId))

            _logger.LogInformation(
                "[Instance: {InstanceId}] Fetching team members for user: {UserId}, {userId}",
                _instanceId, loggedInUserId, userId);

        var result = await _userServiceManager.GetTeamMembersByUserIdAsync(loggedInUserId);

        return ToActionResult(result);
    }

    /// <summary>
    /// Get team members for another user (requires manager access to that user).
    /// GET /api/users/{userId}/team-members
    /// Returns 403 if the logged-in user is not in the hierarchy above {userId}.
    /// </summary>
    [HttpGet("{userId:guid}/team-members")]
    [ValidateUserClaim]
    [ProducesResponseType(typeof(ApiResponse<TeamMembersDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTeamMembersForUser(Guid userId)
    {
        var loggedInUserId = Helpers.GetLoggedInUserId(User);

        _logger.LogInformation(
            "[Instance: {InstanceId}] User {LoggedInUser} fetching team members for user {TargetUser}",
            _instanceId, loggedInUserId, userId);

        var result = await _userServiceManager.GetTeamMembersOfOtherUserAsync(loggedInUserId, userId);

        return ToActionResult(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps ApiResponse to IActionResult — always returns the full envelope,
    /// never a naked object, so clients always get the same JSON shape.
    /// </summary>
    private IActionResult ToActionResult<T>(ApiResponse<T> response) =>
        response.HttpStatusCode switch
        {
            System.Net.HttpStatusCode.OK => Ok(response),
            System.Net.HttpStatusCode.Created => StatusCode(201, response),
            System.Net.HttpStatusCode.NoContent => NoContent(),
            System.Net.HttpStatusCode.BadRequest => BadRequest(response),
            System.Net.HttpStatusCode.NotFound => NotFound(response),
            System.Net.HttpStatusCode.Conflict => Conflict(response),
            System.Net.HttpStatusCode.Forbidden => StatusCode(403, response),
            System.Net.HttpStatusCode.Unauthorized => StatusCode(401, response),
            System.Net.HttpStatusCode.UnprocessableEntity => StatusCode(422, response),
            _ => StatusCode(500, response)
        };
}