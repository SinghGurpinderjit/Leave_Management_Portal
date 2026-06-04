using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs.Leaves;
using Shared.Common.Utilities;
using System.Net;
using Shared.Common.Constants;
using LeaveManagementService.Services.Contracts;
using Shared.Common.DTOs;
using Shared.Common.Authorization.Attributes;
namespace LeaveManagementService.Controllers;

[Route("api/v{version:apiVersion}/leave-balance")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILogger<LeaveBalancesController> _logger;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly string _instanceId = Environment.MachineName;

    public LeaveBalancesController(
        ILogger<LeaveBalancesController> logger,
        ILeaveBalanceService leaveBalanceService)
    {
        _logger = logger;
        _leaveBalanceService = leaveBalanceService;
    }

    // Single endpoint handles both employee (self) and manager (team member) access.
    [HttpGet("")]
    [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ValidateUserClaim]
    public async Task<IActionResult> GetMyLeaveBalances()
    {
        var loggedInUserId = Helpers.GetLoggedInUserId(User);

        var response = await _leaveBalanceService.GetByEmployeeIdAndLeaveTypeIdAsync(loggedInUserId, null);

        return ToActionResult(response);
    }

    [HttpGet("leavetype/{leaveTypeId:int}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ValidateUserClaim]
    public async Task<IActionResult> GetMyLeaveBalances(int leaveTypeId)
    {
        var loggedInUserId = Helpers.GetLoggedInUserId(User);

        var response = await _leaveBalanceService.GetByEmployeeIdAndLeaveTypeIdAsync(loggedInUserId, leaveTypeId);

        return ToActionResult(response);
    }

    [Authorize(Policy = AppConstants.CustomJwtPolicy.ManagerOnly)]
    [HttpGet("employees/{employeeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ValidateUserClaim]
    public async Task<IActionResult> GetEmployeeLeaveBalances(Guid employeeId)
    {
        var loggedInUserId = Helpers.GetLoggedInUserId(User);

        var response = await _leaveBalanceService.GetOtherEmployeeLeaveBalancesByLeaveTypeIdAsync(loggedInUserId, employeeId, null);

        return ToActionResult(response);
    }

    [Authorize(Policy = AppConstants.CustomJwtPolicy.ManagerOnly)]
    [HttpGet("employees/{employeeId:guid}/leavetype/{leaveTypeId:int}")]
    [ProducesResponseType(typeof(IEnumerable<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ValidateUserClaim]
    public async Task<IActionResult> GetEmployeeLeaveBalances(Guid employeeId, int leaveTypeId)
    {
        var loggedInUserId = Helpers.GetLoggedInUserId(User);

        var response = await _leaveBalanceService.GetOtherEmployeeLeaveBalancesByLeaveTypeIdAsync(loggedInUserId, employeeId, leaveTypeId);

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
