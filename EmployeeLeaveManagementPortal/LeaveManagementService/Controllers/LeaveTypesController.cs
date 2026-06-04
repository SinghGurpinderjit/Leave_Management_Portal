using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs.Leaves;
using System.Net;
using Shared.Common.DTOs;
using LeaveManagementService.Services.Contracts;
using Shared.Common.Authorization.Attributes;
namespace LeaveManagementService.Controllers;

[Route("api/v{version:apiVersion}/leave-types")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class LeaveTypesController : ControllerBase
{
    private readonly ILogger<LeaveBalancesController> _logger;
    private readonly ILeaveTypeService _leaveTypeService;
    private readonly string _instanceId = Environment.MachineName;

    public LeaveTypesController(
        ILogger<LeaveBalancesController> logger,
        ILeaveTypeService leaveTypeService)
    {
        _logger = logger;
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet()]
    [ProducesResponseType(typeof(IEnumerable<LeaveTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ValidateUserClaim]
    public async Task<IActionResult> GetAll()
    {
        var response = await _leaveTypeService.GetAll();

        return ToActionResult(response);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ValidateUserClaim]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _leaveTypeService.GetById(id);

        return ToActionResult(response);
    }
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
                HttpStatusCode.UnprocessableEntity => StatusCode(422, response),
                _ => StatusCode(500, response)
            };
}
