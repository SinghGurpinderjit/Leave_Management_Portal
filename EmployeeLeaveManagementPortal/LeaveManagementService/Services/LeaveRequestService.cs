namespace LeaveManagementService.Services
{
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Leaves;
    using Shared.Common.Models.LeaveManagementService;
    using LeaveManagementService.Repositories;
    using LeaveManagementService.UnitOfWork;
    using Microsoft.Extensions.Logging;
    using Microsoft.EntityFrameworkCore;
    using LeaveManagementService.Services.Contracts;
    using Shared.Common.Messaging;
    using Microsoft.Extensions.Options;
    using Shared.Common.Configuration;
    using Shared.Common.Messages;
    using Shared.Common.Extensions;

    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly ILogger<LeaveRequestService> _logger;
        private readonly ILeaveRequestRepository _leaveRequestRepository;
        private readonly ILeaveBalanceRepository _leaveBalanceRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessagePublisher _messagePublisher;
        private readonly RabbitMQSettings _leaveStatusPublishSettings;

        private readonly string _instanceId = Environment.MachineName;

        public LeaveRequestService(
            ILogger<LeaveRequestService> logger,
            ILeaveRequestRepository leaveRequestRepository,
            ILeaveBalanceRepository leaveBalanceRepository,
            IUserServiceClient userServiceClient,
            IUnitOfWork unitOfWork,
            IMessagePublisher messagePublisher,
            IOptions<RabbitMQSettings> settings)
        {
            _logger = logger;
            _leaveRequestRepository = leaveRequestRepository;
            _leaveBalanceRepository = leaveBalanceRepository;
            _userServiceClient = userServiceClient;
            _unitOfWork = unitOfWork;
            _messagePublisher = messagePublisher;

            _leaveStatusPublishSettings = settings.Value;
        }

        public async Task<ApiResponse<LeaveRequestDto>> ApplyLeaveAsync(
            Guid loggedInUserId, CreateLeaveRequestDto request)
        {
            try
            {
                // 1. Validate the logged-in user exists
                var userInfo = await _userServiceClient.GetUserByIdAsync(loggedInUserId);
                if (userInfo == null)
                    return ApiResponse<LeaveRequestDto>.BadRequest($"User not found: {loggedInUserId}");

                // 2. Validate reporting manager matches the user's actual manager
                if (userInfo.Manager != null)
                {
                    if (!request.ReportingManagerId.HasValue)
                        return ApiResponse<LeaveRequestDto>.BadRequest("Reporting manager ID is required.");

                    if (request.ReportingManagerId.Value != userInfo.Manager.Id)
                        return ApiResponse<LeaveRequestDto>.BadRequest(
                            "Reporting manager ID does not matches or manager not assigned.");
                }

                // 3. Check for overlapping leave requests
                var hasOverlap = await _leaveRequestRepository.HasOverlappingRequestAsync(
                    loggedInUserId,
                    request.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    request.EndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

                if (hasOverlap)
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] Overlapping leave request exists for Employee: {EmployeeId}",
                        _instanceId, loggedInUserId);

                    return ApiResponse<LeaveRequestDto>.Conflict(
                        "An overlapping leave request already exists for the selected dates.");
                }

                // 4. Check leave balance
                var leaveBalance = await _leaveBalanceRepository
                    .GetByEmployeeAndLeaveTypeAsync(loggedInUserId, request.LeaveTypeId);

                if (leaveBalance is null)
                    return ApiResponse<LeaveRequestDto>.BadRequest(
                        $"No leave balance found for leave type ID: {request.LeaveTypeId}.");

                var availableDays = leaveBalance.LeaveType!.DefaultAllocation - leaveBalance.UsedLeaves;
                if (availableDays < request.NumberOfDays)
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] Insufficient balance for Employee: {EmployeeId}. " +
                        "Available: {Available}, Requested: {Requested}",
                        _instanceId, loggedInUserId, availableDays, request.NumberOfDays);

                    return ApiResponse<LeaveRequestDto>.BadRequest(
                        $"Insufficient leave balance. Available: {availableDays} day(s), Requested: {request.NumberOfDays} day(s).");
                }

                // 5. Create the leave request (status defaults to Pending)
                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = loggedInUserId,
                    LeaveTypeId = request.LeaveTypeId,
                    StartDate = request.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    EndDate = request.EndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    NumberOfDays = request.NumberOfDays,
                    ApplicationReason = request.Reason,
                    ReportingManagerId = request.ReportingManagerId,
                    Status = LeaveStatus.Pending
                };

                var created = await _leaveRequestRepository.AddAsync(leaveRequest);

                _logger.LogInformation(
                    "[Instance: {InstanceId}] Leave request {LeaveRequestId} created for Employee: {EmployeeId}",
                    _instanceId, created.Id, loggedInUserId);

                // 6. Publish notification: leave applied
                await PublishLeaveAppliedNotificationAsync(created, userInfo.Manager?.Id, userInfo.Email, userInfo?.Manager?.Email);

                return ApiResponse<LeaveRequestDto>.Created(MapToDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error creating leave request for Employee: {EmployeeId}",
                    _instanceId, loggedInUserId);

                return ApiResponse<LeaveRequestDto>.InternalError(
                    "An error occurred while submitting your leave request.");
            }
        }

        public async Task<ApiResponse<TeamLeaveRequestSummaryDto>> SetLeaveRequestStatusAsync(Guid leaveRequestId, Guid managerId, string status, string? reviewReason)
        {
            try
            {
                // Parse status string to enum
                if (!Enum.TryParse<LeaveStatus>(status, ignoreCase: true, out var leaveStatus)
                    || (leaveStatus != LeaveStatus.Approved && leaveStatus != LeaveStatus.Rejected))
                {
                    return ApiResponse<TeamLeaveRequestSummaryDto>.BadRequest(
                        "Managers can only approve or reject leave requests.");
                }

                var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId);
                if (leaveRequest is null)
                    return ApiResponse<TeamLeaveRequestSummaryDto>.NotFound(
                        $"Leave request not found: {leaveRequestId}");

                // Verify the employee belongs to this manager's team
                var teamInfo = await _userServiceClient.GetTeamMembersAsync(managerId);
                if (teamInfo is null)
                    return ApiResponse<TeamLeaveRequestSummaryDto>.Forbidden("Manager not found.");

                if (!teamInfo.TeamMembers.Select(x => x.Id).Contains(leaveRequest.EmployeeId))
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] Manager {ManagerId} attempted to update request " +
                        "{LeaveRequestId} outside their team",
                        _instanceId, managerId, leaveRequestId);

                    return ApiResponse<TeamLeaveRequestSummaryDto>.Forbidden(
                        "You are not authorized to update this leave request.");
                }

                // Guard: only Pending requests can be approved/rejected
                if (leaveRequest.Status != LeaveStatus.Pending)
                    return ApiResponse<TeamLeaveRequestSummaryDto>.BadRequest(
                        $"Only pending requests can be approved or rejected. " +
                        $"Current status: {leaveRequest.Status}.");

                var leaveBalance = await _leaveBalanceRepository
                    .GetByEmployeeAndLeaveTypeAsync(leaveRequest.EmployeeId, leaveRequest.LeaveType.Id);

                if (leaveBalance is null)
                    return ApiResponse<TeamLeaveRequestSummaryDto>.NotFound("Leave balance not found.");

                if (string.Equals(status, LeaveStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var availableDays = leaveBalance.LeaveType!.DefaultAllocation - leaveBalance.UsedLeaves;
                    if (availableDays < leaveRequest.NumberOfDays)
                    {
                        _logger.LogWarning(
                            "[Instance: {InstanceId}] Due to insufficient balance , request can not be approved for Employee: {EmployeeId}. " +
                            "| Leave balance available: {Available}, Requested: {Requested}",
                            _instanceId, leaveRequest.EmployeeId, availableDays, leaveRequest.NumberOfDays);

                        return ApiResponse<TeamLeaveRequestSummaryDto>.BadRequest(
                            $"Insufficient leave balance for Employee: {leaveRequest.EmployeeId}. Available: {availableDays} day(s), Requested: {leaveRequest.NumberOfDays} day(s).");
                    }
                }

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var updated = await _leaveRequestRepository
                        .UpdateStatusAsync(leaveRequestId, leaveStatus, leaveRequest.ApplicationReason, reviewReason, managerId);

                    if (leaveStatus == LeaveStatus.Approved)
                    {
                        leaveBalance.UsedLeaves += leaveRequest.NumberOfDays;
                        await _leaveBalanceRepository.UpdateAsync(leaveBalance.Id, leaveBalance);
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "[Instance: {InstanceId}] Leave request {LeaveRequestId} {Status} by Manager {ManagerId}",
                        _instanceId, leaveRequestId, leaveStatus, managerId);

                    // ── Publish notification after successful commit ──

                    if (leaveStatus == LeaveStatus.Approved)
                        await PublishLeaveApprovedNotificationAsync(updated!, managerId, updated.ReviewReason);
                    else if (leaveStatus == LeaveStatus.Rejected)
                        await PublishLeaveRejectedNotificationAsync(updated!, managerId, updated.ReviewReason);

                    // Build name lookup from team members
                    var nameLookup = teamInfo.TeamMembers
                        .ToDictionary(m => m.Id, m => (Email: m.Email, FullName: m.FullName));

                    return ApiResponse<TeamLeaveRequestSummaryDto>.Ok(MapToTeamSummaryDto(updated!, nameLookup.ToDictionary()));
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync();

                    _logger.LogError(ex,
                        "[Instance: {InstanceId}] Transaction failed updating leave request {LeaveRequestId}",
                        _instanceId, leaveRequestId);

                    return ApiResponse<TeamLeaveRequestSummaryDto>.InternalError(
                        "Failed to update the leave request. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error setting status on leave request {LeaveRequestId}",
                    _instanceId, leaveRequestId);

                return ApiResponse<TeamLeaveRequestSummaryDto>.InternalError(
                    "An error occurred while updating the leave request.");
            }
        }

        public async Task<ApiResponse<PaginatedResult<LeaveRequestDto>>> GetMyLeaveRequestHistoryAsync(
            Guid loggedInUserId, LeaveRequestHistoryQuery query)
        {
            try
            {
                var leaveRequests = await _leaveRequestRepository.GetByEmployeeIdAsync(loggedInUserId);

                // Apply status filter
                var filtered = leaveRequests.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query.Status) &&
                    !string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(x =>
                        string.Equals(x.Status.ToString(), query.Status, StringComparison.OrdinalIgnoreCase));
                }

                // Apply date range filter
                if (query.FromDate.HasValue)
                    filtered = filtered.Where(x =>
                        DateOnly.FromDateTime(x.StartDate) >= query.FromDate.Value);

                if (query.ToDate.HasValue)
                    filtered = filtered.Where(x =>
                        DateOnly.FromDateTime(x.EndDate) <= query.ToDate.Value);

                var totalCount = filtered.Count();

                var items = filtered
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(MapToDto)
                    .ToList();


                return ApiResponse<PaginatedResult<LeaveRequestDto>>.Ok(
                    PaginatedResult<LeaveRequestDto>.Create(items, totalCount, query.Page, query.PageSize));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error fetching leave history for Employee: {EmployeeId}",
                    _instanceId, loggedInUserId);

                return ApiResponse<PaginatedResult<LeaveRequestDto>>.InternalError(
                    "An error occurred while fetching your leave history.");
            }
        }

        // ── Employee: get single request ──────────────────────────────────────────

        public async Task<ApiResponse<LeaveRequestDto>> GetLeaveRequestByIdAsync(
            Guid leaveRequestId, Guid loggedInUserId)
        {
            try
            {
                var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId);
                if (leaveRequest is null)
                    return ApiResponse<LeaveRequestDto>.NotFound(
                        $"Leave request not found: {leaveRequestId}");

                // Employees can only see their own requests
                if (leaveRequest.EmployeeId != loggedInUserId)
                    return ApiResponse<LeaveRequestDto>.Forbidden("Only person who has raised request can view. You are not authorized to view this leave request.");

                return ApiResponse<LeaveRequestDto>.Ok(MapToDto(leaveRequest));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error fetching leave request {LeaveRequestId}",
                    _instanceId, leaveRequestId);

                return ApiResponse<LeaveRequestDto>.InternalError(
                    "An error occurred while fetching the leave request.");
            }
        }

        // ── Manager: get team requests ────────────────────────────────────────────

        public async Task<ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>> GetTeamLeaveRequestsAsync(
            Guid managerId, TeamLeaveRequestQuery query)
        {
            try
            {
                var teamInfo = await _userServiceClient.GetTeamMembersAsync(managerId);
                if (teamInfo is null)
                    return ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>.NotFound(
                        "Manager not found.");

                if (!teamInfo.TeamMembers.Any())
                    return ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>.Ok(
                        PaginatedResult<TeamLeaveRequestSummaryDto>.Empty(query.Page, query.PageSize));

                var teamMemberIds = teamInfo.TeamMembers.Select(x => x.Id).ToList();

                // Optional: filter to a specific employee
                if (query.EmployeeId.HasValue)
                {
                    if (!teamMemberIds.Contains(query.EmployeeId.Value))
                        return ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>.Forbidden(
                            "The specified employee is not a member of your team.");

                    teamMemberIds = [query.EmployeeId.Value];
                }

                var requests = await _leaveRequestRepository.GetByEmployeeIdsAsync(teamMemberIds);

                var filtered = requests.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query.Status) &&
                    !string.Equals(query.Status, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(x =>
                        string.Equals(x.Status.ToString(), query.Status, StringComparison.OrdinalIgnoreCase));
                }

                if (query.FromDate.HasValue)
                    filtered = filtered.Where(x =>
                        DateOnly.FromDateTime(x.StartDate) >= query.FromDate.Value);

                if (query.ToDate.HasValue)
                    filtered = filtered.Where(x =>
                        DateOnly.FromDateTime(x.EndDate) <= query.ToDate.Value);

                var totalCount = filtered.Count();

                // Build name lookup from team members
                var nameLookup = teamInfo.TeamMembers
                    .ToDictionary(m => m.Id, m => (Email: m.Email, FullName: m.FullName));

                var items = filtered
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(r => MapToTeamSummaryDto(r, nameLookup.ToDictionary()))
                    .ToList();

                return ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>.Ok(
                    PaginatedResult<TeamLeaveRequestSummaryDto>.Create(items, totalCount, query.Page, query.PageSize));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error fetching team leave requests for Manager: {ManagerId}",
                    _instanceId, managerId);

                return ApiResponse<PaginatedResult<TeamLeaveRequestSummaryDto>>.InternalError(
                    "An error occurred while fetching team leave requests.");
            }
        }

        // ── Manager: get single team request ─────────────────────────────────────

        public async Task<ApiResponse<TeamLeaveRequestSummaryDto>> GetTeamLeaveRequestByIdAsync(
            Guid leaveRequestId, Guid managerId)
        {
            try
            {
                var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId);
                if (leaveRequest is null)
                    return ApiResponse<TeamLeaveRequestSummaryDto>.NotFound(
                        $"Leave request not found: {leaveRequestId}");

                if (leaveRequest.EmployeeId == managerId)
                {
                    return ApiResponse<TeamLeaveRequestSummaryDto>.BadRequest($"Leave request ID: {leaveRequestId} does not belongs to your team members.");
                }

                var teamInfo = await _userServiceClient.GetTeamMembersAsync(managerId);
                if (teamInfo is null)
                    return ApiResponse<TeamLeaveRequestSummaryDto>.Forbidden("Manager not found.");

                if (!teamInfo.TeamMembers.Select(x => x.Id).Contains(leaveRequest.EmployeeId))
                    return ApiResponse<TeamLeaveRequestSummaryDto>.Forbidden(
                        "You are not authorized to view this leave request.");

                // Build name lookup from team members
                var nameLookup = teamInfo.TeamMembers.Where(x => x.Id == leaveRequest.EmployeeId)
                    .ToDictionary(m => m.Id, m => (Email: m.Email, FullName: m.FullName));

                return ApiResponse<TeamLeaveRequestSummaryDto>.Ok(MapToTeamSummaryDto(leaveRequest, nameLookup.ToDictionary()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error fetching team leave request {LeaveRequestId}",
                    _instanceId, leaveRequestId);

                return ApiResponse<TeamLeaveRequestSummaryDto>.InternalError(
                    "An error occurred while fetching the leave request.");
            }
        }

        // ── Employee: cancel request ──────────────────────────────────────────────

        public async Task<ApiResponse<LeaveRequestDto>> CancelLeaveRequestAsync(
            Guid leaveRequestId, Guid loggedInUserId, string? reason)
        {
            try
            {
                var leaveRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequestId);
                if (leaveRequest is null)
                    return ApiResponse<LeaveRequestDto>.NotFound(
                        $"Leave request not found: {leaveRequestId}");

                // Only the owner can cancel
                if (leaveRequest.EmployeeId != loggedInUserId)
                    return ApiResponse<LeaveRequestDto>.Forbidden(
                        "You are not authorized to cancel this leave request.");

                // Guard: only Pending requests can be cancelled
                if (leaveRequest.Status != LeaveStatus.Pending)
                    return ApiResponse<LeaveRequestDto>.BadRequest(
                        $"Only pending requests can be cancelled. Current status: {leaveRequest.Status}.");

                var updated = await _leaveRequestRepository
                    .UpdateStatusAsync(leaveRequestId, LeaveStatus.Cancelled, applicationReason: leaveRequest.ApplicationReason, reviewReason: reason, loggedInUserId);

                if (updated is null)
                    return ApiResponse<LeaveRequestDto>.InternalError(
                        "Failed to cancel the leave request.");

                _logger.LogInformation(
                    "[Instance: {InstanceId}] Leave request {LeaveRequestId} cancelled by Employee {EmployeeId}",
                    _instanceId, leaveRequestId, loggedInUserId);

                return ApiResponse<LeaveRequestDto>.Ok(MapToDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Error cancelling leave request {LeaveRequestId}",
                    _instanceId, leaveRequestId);

                return ApiResponse<LeaveRequestDto>.InternalError(
                    "An error occurred while cancelling the leave request.");
            }
        }

        // ── Notification helpers ──────────────────────────────────────────────────

        private async Task PublishLeaveAppliedNotificationAsync(LeaveRequest request, Guid? managerId, string employeeEmail, string? managerEmail)
        {
            try
            {
                var message = new LeaveNotificationMessage
                {
                    LeaveRequestId = request.Id,
                    EmployeeId = request.EmployeeId,
                    EmployeeEmail = employeeEmail,
                    ManagerEmail = managerEmail,
                    ManagerId = managerId,
                    LeaveType = request.LeaveType?.Name ?? string.Empty,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    NumberOfDays = request.NumberOfDays,
                    Status = LeaveStatus.Pending.ToString(),
                    NotificationType = LeaveNotificationType.LeaveApplied,
                    OccurredAt = DateTime.UtcNow
                };

                await _messagePublisher.PublishAsync(message, _leaveStatusPublishSettings.RoutingKey);

                _logger.LogInformation(
                    "[Instance: {InstanceId}] LeaveApplied notification published for request {LeaveRequestId}",
                    _instanceId, request.Id);
            }
            catch (Exception ex)
            {
                // Notification failure must NOT fail the main operation
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Failed to publish LeaveApplied notification for request {LeaveRequestId}",
                    _instanceId, request.Id);
            }
        }

        private async Task PublishLeaveApprovedNotificationAsync(LeaveRequest request, Guid managerId, string? reviewReason)
        {
            try
            {
                var message = new LeaveNotificationMessage
                {
                    LeaveRequestId = request.Id,
                    EmployeeId = request.EmployeeId,
                    ManagerId = managerId,
                    LeaveType = request.LeaveType?.Name ?? string.Empty,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    NumberOfDays = request.NumberOfDays,
                    Status = LeaveStatus.Approved.ToString(),
                    NotificationType = LeaveNotificationType.LeaveApproved,
                    OccurredAt = DateTime.UtcNow
                };

                await _messagePublisher.PublishAsync(message, _leaveStatusPublishSettings.RoutingKey);

                _logger.LogInformation(
                    "[Instance: {InstanceId}] LeaveApproved notification published for request {LeaveRequestId}",
                    _instanceId, request.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Failed to publish LeaveApproved notification for request {LeaveRequestId}",
                    _instanceId, request.Id);
            }
        }

        private async Task PublishLeaveRejectedNotificationAsync(
            LeaveRequest request, Guid managerId, string? reviewReason)
        {
            try
            {
                var message = new LeaveNotificationMessage
                {
                    LeaveRequestId = request.Id,
                    EmployeeId = request.EmployeeId,
                    ManagerId = managerId,
                    LeaveType = request.LeaveType?.Name ?? string.Empty,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    NumberOfDays = request.NumberOfDays,
                    Status = LeaveStatus.Rejected.ToString(),
                    ReviewReason = reviewReason,
                    NotificationType = LeaveNotificationType.LeaveRejected,
                    OccurredAt = DateTime.UtcNow
                };

                await _messagePublisher.PublishAsync(message, _leaveStatusPublishSettings.RoutingKey);

                _logger.LogInformation(
                    "[Instance: {InstanceId}] LeaveRejected notification published for request {LeaveRequestId}",
                    _instanceId, request.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Instance: {InstanceId}] Failed to publish LeaveRejected notification for request {LeaveRequestId}",
                    _instanceId, request.Id);
            }
        }

        // ── Mappers ───────────────────────────────────────────────────────────────

        private static LeaveRequestDto MapToDto(LeaveRequest r) => new()
        {
            LeaveRequestId = r.Id,
            EmployeeId = r.EmployeeId,
            LeaveType = r.LeaveType?.Name ?? string.Empty,
            StartDate = DateOnly.FromDateTime(r.StartDate),
            EndDate = DateOnly.FromDateTime(r.EndDate),
            NumberOfDays = r.NumberOfDays,
            Status = r.Status.ToString(),
            ApplicationReason = r.ApplicationReason ?? string.Empty,
            ReviewReason = r.ReviewReason ?? string.Empty,
            ReportingManagerId = r.ReportingManagerId,
            CreatedAt = r.CreatedAt.UtcToLocalDatetimeString(),
            UpdatedAt = r.UpdatedAt.HasValue ? r.UpdatedAt.Value.UtcToLocalDatetimeString() : null
        };

        private static TeamLeaveRequestSummaryDto MapToTeamSummaryDto(
            LeaveRequest r, Dictionary<Guid, (string Email, string FullName)> teamMembersLookup) => new()
            {
                LeaveRequestId = r.Id,
                EmployeeInfo = new()
                {
                    Id = r.EmployeeId,
                    FullName = teamMembersLookup.TryGetValue(r.EmployeeId, out var employee) ? employee.FullName : string.Empty,
                    Email = employee.Email ?? string.Empty,
                },
                LeaveType = r.LeaveType?.Name ?? string.Empty,
                StartDate = r.StartDate.UtcToLocalDateOnlyString(),
                EndDate = r.EndDate.UtcToLocalDateOnlyString(),
                NumberOfDays = r.NumberOfDays,
                Status = r.Status.ToString(),
                ReportingManagerId = r.ReportingManagerId,
                CreatedAt = r.CreatedAt.UtcToLocalDatetimeString(),//ToString("yyyy-MM-dd"),
                UpdatedAt = r.UpdatedAt.HasValue ? r.UpdatedAt.Value.UtcToLocalDatetimeString() : null,
                ApplicationReason = r.ApplicationReason,
                ReviewReason = r.ReviewReason,
            };
    }
}