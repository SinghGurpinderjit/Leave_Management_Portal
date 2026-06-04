using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Employee;
using System.Net;
using UserService.Services.Contracts;

namespace UserService.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly ILogger<RolesController> _logger;
    private readonly IRolesService _rolesService;
    private readonly IValidator<CreateRoleDto> _createRoleValidator;
    private readonly IValidator<UpdateRoleDto> _updateRoleValidator;
    private readonly string _instanceId = Environment.MachineName;

    public RolesController(
        ILogger<RolesController> logger,
        IRolesService rolesService,
        IValidator<CreateRoleDto> createRoleValidator,
        IValidator<UpdateRoleDto> updateRoleValidator)
    {
        _rolesService = rolesService;
        _logger = logger;
        _createRoleValidator = createRoleValidator;
        _updateRoleValidator = updateRoleValidator;
    }

    // GET api/roles
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("[Instance: {InstanceId}] GetAll roles request", _instanceId);

        var response = await _rolesService.GetAllAsync();
        return ToActionResult(response);
    }

    // GET api/roles/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation(
            "[Instance: {InstanceId}] GetById role request: {RoleId}",
            _instanceId,
            id);

        var response = await _rolesService.GetByIdAsync(id);
        return ToActionResult(response);
    }
    
    /// <summary>
    /// Maps ApiResponse to IActionResult — always returns the full envelope,
    /// never a naked object, so clients always get the same JSON shape.
    /// </summary>
    private IActionResult ToActionResult<T>(ApiResponse<T> response) =>
        response.HttpStatusCode switch
        {
            HttpStatusCode.OK => Ok(response),
            HttpStatusCode.Created => StatusCode(201, response),
            HttpStatusCode.NoContent => NoContent(),
            HttpStatusCode.BadRequest => BadRequest(response),
            HttpStatusCode.NotFound => NotFound(response),
            HttpStatusCode.Conflict => Conflict(response),
            HttpStatusCode.Forbidden => StatusCode(403, response),
            HttpStatusCode.Unauthorized => StatusCode(401, response),
            HttpStatusCode.UnprocessableEntity => StatusCode(422, response),
            _ => StatusCode(500, response)
        };
}