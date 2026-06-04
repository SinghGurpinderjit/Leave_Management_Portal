namespace LeaveManagementService.Services
{
    using System.Net;
    using Shared.Common.DTOs;
    using LeaveManagementService.Repositories;
    using Shared.Common.Models.LeaveManagementService;
    using Shared.Common.DTOs.Leaves;
    using AutoMapper;
    using LeaveManagementService.Services.Contracts;

    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly ILogger<LeaveBalanceService> _logger;
        private readonly ILeaveBalanceRepository _leaveBalanceRepository;
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;
        private readonly string _instanceId = Environment.MachineName;

        public LeaveBalanceService(
            ILogger<LeaveBalanceService> logger,
            ILeaveBalanceRepository leaveBalanceRepository,
            ILeaveTypeRepository leaveTypeRepository,
            IUserServiceClient userServiceClient,
            IMapper mapper)
        {
            _logger = logger;
            _leaveBalanceRepository = leaveBalanceRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _userServiceClient = userServiceClient;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<LeaveBalanceDto>>> GetByEmployeeIdAndLeaveTypeIdAsync(
            Guid loggedInUserId, int? leaveTypeId)
        {
            //var response = new ApiResponse<IEnumerable<LeaveBalanceDto>>();
            try
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] Fetching details of user : {UserID}",
                    _instanceId,
                    loggedInUserId);

                var userExists = await _userServiceClient.ValidateUserAsync(loggedInUserId);
                if (!userExists)
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] Requested user not found : {UserID}",
                        _instanceId, loggedInUserId);

                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.BadRequest("Requested user not found.");
                }

                _logger.LogInformation(
                    "[Instance: {InstanceId}] Fetching leave balances for user: {UserId}",
                    _instanceId, loggedInUserId);

                var leaveBalances = await _leaveBalanceRepository.GetByEmployeeIdAsync(loggedInUserId);
                if (leaveBalances is null || !leaveBalances.Any())
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] No leave balances found for user: {UserId}",
                        _instanceId, loggedInUserId);

                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.NotFound("Leave balances not found.");
                }

                if (leaveTypeId != null && leaveTypeId != default)
                {
                    var filteredLeaveBalances = leaveBalances.Where(x => x.LeaveType!.Id == leaveTypeId)
                                                         .Select(MapToDto)
                                                         .ToList();
                    if (!filteredLeaveBalances.Any())
                    {
                        string errorMessage = "Leave balances not found" + (leaveTypeId == null ? string.Empty : $" for leave type ID: {leaveTypeId}");
                        _logger.LogWarning(
                            "[Instance: {InstanceId}] No leave balances found for user: {UserId} for leave type: {LeaveTypeId}",
                            _instanceId, loggedInUserId, leaveTypeId);

                        return ApiResponse<IEnumerable<LeaveBalanceDto>>.NotFound(errorMessage);
                    }
                    else
                    {
                        return ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(filteredLeaveBalances);
                    }
                }
                else
                {
                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(leaveBalances.Select(MapToDto).ToList());
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Instance: {InstanceId}] Error fetching leave balance for Employee: {EmployeeId}",
                    _instanceId, loggedInUserId);

                return ApiResponse<IEnumerable<LeaveBalanceDto>>.InternalError("An error occurred while fetching leave balance .");
            }
        }


        public async Task<ApiResponse<IEnumerable<LeaveBalanceDto>>> GetOtherEmployeeLeaveBalancesByLeaveTypeIdAsync(
            Guid loggedInUserId, Guid requestedEmployeeId, int? leaveTypeId)
        {
            //var response = new ApiResponse<IEnumerable<LeaveBalanceDto>>();
            try
            {
                // Validate User
                var requestedEmployeeExists = await _userServiceClient.ValidateUserAsync(requestedEmployeeId);
                if (!requestedEmployeeExists)
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] Requested user not found : {UserID}",
                        _instanceId, requestedEmployeeId);

                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.NotFound($"Requested user not found: {requestedEmployeeId}");
                }

                _logger.LogInformation("[Instance: {InstanceId}] Fetching team members of Manager: {ManagerId}", _instanceId, loggedInUserId);

                var isSelfAccess = loggedInUserId == requestedEmployeeId;
                if (!isSelfAccess)
                {
                    var teamMembersInfo = await _userServiceClient.GetTeamMembersAsync(loggedInUserId);
                    if (teamMembersInfo == null)
                    {
                        _logger.LogWarning("[Instance: {InstanceId}] Logged in user details not found : {UserID}", _instanceId, loggedInUserId);

                        return ApiResponse<IEnumerable<LeaveBalanceDto>>.BadRequest($"Logged in user details not found: {loggedInUserId}.");
                    }

                    var isManagerOfEmployee = teamMembersInfo?.TeamMembers.Select(x => x.Id).Contains(requestedEmployeeId) ?? false;
                    if (!isManagerOfEmployee)
                    {
                        _logger.LogWarning(
                            "[Instance: {InstanceId}] Unauthorized access attempt — LoggedInUser: {LoggedInUserId} tried to access user: {UserId}",
                            _instanceId, loggedInUserId, requestedEmployeeId);

                        return ApiResponse<IEnumerable<LeaveBalanceDto>>.Forbidden(
                            $"You are not authorized to access this user: {requestedEmployeeId}.");
                    }
                }

                _logger.LogInformation(
                    "[Instance: {InstanceId}] Fetching leave balances for requested employee: {EmployeeId}",
                    _instanceId, requestedEmployeeId);

                var leaveBalances = await _leaveBalanceRepository.GetByEmployeeIdAsync(requestedEmployeeId);
                if (leaveBalances is null || !leaveBalances.Any())
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] No leave balances found for requested user: {EmployeeId}",
                        _instanceId, requestedEmployeeId);

                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.NotFound($"No Leave balances found for user: {requestedEmployeeId}");
                }

                if (leaveTypeId != null && leaveTypeId != default)
                {
                    var filteredLeaveBalances = leaveBalances.Where(x => x.LeaveType!.Id == leaveTypeId)
                                                         .Select(MapToDto)
                                                         .ToList();
                    if (!filteredLeaveBalances.Any())
                    {
                        string errorMessage = $"Leave balances not found for user {requestedEmployeeId}" + (leaveTypeId == null ? string.Empty : $" for leave type ID: {leaveTypeId}");
                        _logger.LogWarning(
                            "[Instance: {InstanceId}] No leave balances found for user: {UserId} for leave type: {LeaveTypeId}",
                            _instanceId, loggedInUserId, leaveTypeId);

                        return ApiResponse<IEnumerable<LeaveBalanceDto>>.BadRequest(errorMessage);
                    }
                    else
                    {
                        return ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(filteredLeaveBalances);
                    }
                }
                else
                {
                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(leaveBalances.Select(MapToDto).ToList());
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Instance: {InstanceId}] Error fetching leave balance for Employee: {EmployeeId}",
                    _instanceId, requestedEmployeeId);

                return ApiResponse<IEnumerable<LeaveBalanceDto>>.InternalError("An error occurred while fetching leave balance .");
            }
        }

        public async Task<ApiResponse<IEnumerable<LeaveBalanceDto>>> AddDefaultLeaveBalanceForUser(Guid userId)
        {
            try
            {
                // Validate Leave Type
                var leaveTypes = await _leaveTypeRepository.GetAllAsync();
                if (!leaveTypes.Any())
                {
                    _logger.LogWarning("No leave types data found. Skipping adding default leave allocation for user: {UserId}", userId);
                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.BadRequest("No leave types data found");
                }

                _logger.LogInformation("Adding default leave balance for user: {UserId}", userId);

                if ((await _leaveBalanceRepository.GetByEmployeeIdAsync(userId)).Any())
                {
                    _logger.LogInformation("Already added default leave balance for user: {UserId}. Skipping", userId);
                    return ApiResponse<IEnumerable<LeaveBalanceDto>>.Conflict("Already added default leave balance.");
                }

                var defaultLeaveBalances = leaveTypes.Select(leaveType => new LeaveBalance
                {
                    EmployeeId = userId,
                    LeaveTypeId = leaveType.Id,
                    UsedLeaves = 0,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                var leavBalanceCreated = await _leaveBalanceRepository.CreateRangeAsync(defaultLeaveBalances);

                _logger.LogInformation("Added default leave balance successfully for user: {UserId}", userId);

                return ApiResponse<IEnumerable<LeaveBalanceDto>>.Ok(MapToDto(leavBalanceCreated));
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Some error occured while adding default leave balance for user: {UserId}", userId);
                return ApiResponse<IEnumerable<LeaveBalanceDto>>.InternalError("Some error occurred");
            }

        }


        #region Private methods
        private LeaveBalanceDto MapToDto(LeaveBalance leaveBalance) =>
            _mapper.Map<LeaveBalanceDto>(leaveBalance);

        private IEnumerable<LeaveBalanceDto> MapToDto(IEnumerable<LeaveBalance> leaveBalances) =>
            leaveBalances.Select(leaveBalance => _mapper.Map<LeaveBalanceDto>(leaveBalance));
        #endregion
    }
}