namespace LeaveManagementService.Controllers
{
    using Asp.Versioning;
    using FluentValidation;
    using LeaveManagementService.Services.Contracts;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Shared.Common.Authorization.Attributes;
    using Shared.Common.Constants;
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Leaves;
    using Shared.Common.Utilities;
    using System;
    using System.Net;

    [Route("api/v{version:apiVersion}/leaves")]
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class LeaveRequestsController : ControllerBase
    {

        private readonly ILogger<LeaveRequestsController> _logger;
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly IValidator<CreateLeaveRequestDto> _createValidator;
        private readonly IValidator<UpdateLeaveRequestStatusDto> _statusValidator;
        private readonly IValidator<CancelLeaveRequestDto> _cancelValidator;
        private readonly IValidator<LeaveRequestHistoryQuery> _historyQueryValidator;
        private readonly IValidator<TeamLeaveRequestQuery> _teamQueryValidator;
        private readonly string _instanceId = Environment.MachineName;

        public LeaveRequestsController(
            ILogger<LeaveRequestsController> logger,
            ILeaveRequestService leaveRequestService,
            IValidator<CreateLeaveRequestDto> createValidator,
            IValidator<UpdateLeaveRequestStatusDto> statusValidator,
            IValidator<CancelLeaveRequestDto> cancelValidator,
            IValidator<LeaveRequestHistoryQuery> historyQueryValidator,
            IValidator<TeamLeaveRequestQuery> teamQueryValidator)
        {
            _logger = logger;
            _leaveRequestService = leaveRequestService;
            _createValidator = createValidator;
            _statusValidator = statusValidator;
            _cancelValidator = cancelValidator;
            _historyQueryValidator = historyQueryValidator;
            _teamQueryValidator = teamQueryValidator;
        }

        /// <summary>
        /// Employee: view own leave request history with pagination and filters.
        /// GET /api/v1/leave-requests/history?status=Approved&fromDate=2025-01-01&page=1&pageSize=10
        /// </summary>
        [HttpGet("history")]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<LeaveRequestDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyLeaveHistory([FromQuery] LeaveRequestHistoryQuery query)
        {
            var validation = await _historyQueryValidator.ValidateAsync(query);
            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var loggedInUserId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Employee {UserId} fetching leave history. Status={Status} Page={Page}",
                _instanceId, loggedInUserId, query.Status, query.Page);

            var result = await _leaveRequestService.GetMyLeaveRequestHistoryAsync(loggedInUserId, query);

            return ToActionResult(result);
        }

        /// <summary>
        /// Employee: get a single leave request by ID (must belong to the logged-in employee).
        /// GET /api/v1/leave-requests/{leaveRequestId}
        /// </summary>
        [HttpGet("{leaveRequestId:guid}")]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyLeaveRequestById(Guid leaveRequestId)
        {
            var loggedInUserId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Employee {UserId} fetching leave request {LeaveRequestId}",
                _instanceId, loggedInUserId, leaveRequestId);

            var result = await _leaveRequestService.GetLeaveRequestByIdAsync(leaveRequestId, loggedInUserId);

            return ToActionResult(result);
        }

        /// <summary>
        /// Employee: submit a new leave application.
        /// POST /api/v1/leave-requests
        /// Returns 201 Created with the new leave request on success.
        /// Returns 400 for insufficient balance, past dates, mismatched days.
        /// Returns 409 for overlapping leave request.
        /// </summary>
        [HttpPost]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
        {
            var validation = await _createValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var loggedInUserId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Employee {UserId} applying for leave. Type={LeaveTypeId} Dates={Start}-{End}",
                _instanceId, loggedInUserId, dto.LeaveTypeId, dto.StartDate, dto.EndDate);

            var result = await _leaveRequestService.ApplyLeaveAsync(loggedInUserId, dto);

            if (result.HttpStatusCode == System.Net.HttpStatusCode.Created)
            {
                return CreatedAtAction(
                    nameof(GetMyLeaveRequestById),
                    new { leaveRequestId = result.Data!.LeaveRequestId },
                    result);
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Employee: cancel a pending leave request.
        /// PATCH /api/v1/leave-requests/{leaveRequestId}/cancel
        /// Only the owner of the request can cancel it.
        /// Only Pending requests can be cancelled.
        /// </summary>
        [HttpPatch("{leaveRequestId:guid}/cancel")]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel(Guid leaveRequestId, [FromBody] CancelLeaveRequestDto dto)
        {
            var validation = await _cancelValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var loggedInUserId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Employee {UserId} cancelling leave request {LeaveRequestId}",
                _instanceId, loggedInUserId, leaveRequestId);

            var result = await _leaveRequestService.CancelLeaveRequestAsync(leaveRequestId, loggedInUserId, dto.Reason);

            return ToActionResult(result);
        }

        // ── Manager endpoints ─────────────────────────────────────────────────────

        /// <summary>
        /// Manager: view all leave requests for team members with filters and pagination.
        /// GET /api/v1/leave-requests/team?status=Pending&employeeId=...&fromDate=...&page=1
        /// </summary>
        [HttpGet("team")]
        [Authorize(Policy = AppConstants.CustomJwtPolicy.ManagerOnly)]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTeamLeaveRequests([FromQuery] TeamLeaveRequestQuery query)
        {
            var validation = await _teamQueryValidator.ValidateAsync(query);
            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var managerId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Manager {ManagerId} fetching team leave requests. Status={Status} Employee={EmployeeId}",
                _instanceId, managerId, query.Status, query.EmployeeId);

            var result = await _leaveRequestService.GetTeamLeaveRequestsAsync(managerId, query);

            return ToActionResult(result);
        }

        /// <summary>
        /// Manager: get a single team member's leave request by ID.
        /// GET /api/v1/leave-requests/{leaveRequestId}/team
        /// Returns 403 if the request does not belong to the manager's team.
        /// </summary>
        [HttpGet("{leaveRequestId:guid}/team")]
        [Authorize(Policy = AppConstants.CustomJwtPolicy.ManagerOnly)]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTeamLeaveRequestById(Guid leaveRequestId)
        {
            var managerId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Manager {ManagerId} fetching team leave request {LeaveRequestId}",
                _instanceId, managerId, leaveRequestId);

            var result = await _leaveRequestService.GetTeamLeaveRequestByIdAsync(leaveRequestId, managerId);

            return ToActionResult(result);
        }

        /// <summary>
        /// Manager: approve or reject a pending leave request.
        /// PATCH /api/v1/leave-requests/{leaveRequestId}/status
        /// 
        /// Approve: { "status": "Approved" }
        /// Reject:  { "status": "Rejected", "reason": "Insufficient staffing." }
        /// 
        /// On approval  → deducts leave balance + publishes leave.approved notification.
        /// On rejection → stores reason + publishes leave.rejected notification.
        /// Returns 403 if the request does not belong to the manager's team.
        /// Returns 400 if the request is not in Pending status.
        /// </summary>
        [HttpPatch("{leaveRequestId:guid}/status")]
        [Authorize(Policy = AppConstants.CustomJwtPolicy.ManagerOnly)]
        [ValidateUserClaim]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SetStatus(Guid leaveRequestId, [FromBody] UpdateLeaveRequestStatusDto dto)
        {
            var validation = await _statusValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse<object>.UnprocessableEntity(
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var managerId = Helpers.GetLoggedInUserId(User);

            _logger.LogInformation(
                "[Instance: {InstanceId}] Manager {ManagerId} setting status={Status} on leave request {LeaveRequestId}",
                _instanceId, managerId, dto.Status, leaveRequestId);

            var result = await _leaveRequestService.SetLeaveRequestStatusAsync(
                leaveRequestId, managerId, dto.Status, dto.Reason);

            return ToActionResult(result);
        }

        /// <summary>
        /// Maps the service-layer ApiResponse to the correct IActionResult.
        /// All responses use the unified ApiResponse envelope — no naked objects.
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
                HttpStatusCode.UnprocessableEntity => StatusCode(422, response),
                _ => StatusCode(500, response)
            };
    }
}