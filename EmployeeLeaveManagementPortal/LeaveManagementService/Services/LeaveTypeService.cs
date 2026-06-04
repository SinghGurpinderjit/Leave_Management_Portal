namespace LeaveManagementService.Services
{
    using System.Net;
    using Shared.Common.DTOs;
    using LeaveManagementService.Repositories;
    using Shared.Common.Models.LeaveManagementService;
    using Shared.Common.DTOs.Leaves;
    using AutoMapper;
    using LeaveManagementService.Services.Contracts;

    public class LeaveTypeService : ILeaveTypeService
    {
        private readonly ILogger<LeaveBalanceService> _logger;
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IMapper _mapper;
        private readonly string _instanceId = Environment.MachineName;

        public LeaveTypeService(
            ILogger<LeaveBalanceService> logger,
            ILeaveTypeRepository leaveTypeRepository,
            IMapper mapper)
        {
            _logger = logger;
            _leaveTypeRepository = leaveTypeRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<LeaveTypeDto>> GetById(int leaveTypeId)
        {
            //var response = new ApiResponse<LeaveTypeDto>();

            try
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] Fetching leave type by ID: {LeaveTypeId}",
                    _instanceId, leaveTypeId);

                var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveTypeId);
                if (leaveType is null)
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] No leave type found for ID: {LeaveTypeId}",
                        _instanceId, leaveTypeId);

                    return ApiResponse<LeaveTypeDto>.NotFound("No leave type found.");
                }

                return ApiResponse<LeaveTypeDto>.Ok(MapToDto(leaveType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Instance: {InstanceId}] Some error occured while fetching leave type ID: {LeaveTypeId}", _instanceId, leaveTypeId);
                return ApiResponse<LeaveTypeDto>.InternalError($"Some error occured");
            }
        }

        public async Task<ApiResponse<IEnumerable<LeaveTypeDto>>> GetAll()
        {
            try
            {
                _logger.LogInformation(
                    "[Instance: {InstanceId}] Fetching leave types",
                    _instanceId);

                var leaveTypes = await _leaveTypeRepository.GetAllAsync();
                if (!leaveTypes.Any())
                {
                    _logger.LogWarning(
                        "[Instance: {InstanceId}] No leave types found",
                        _instanceId);

                    return ApiResponse<IEnumerable<LeaveTypeDto>>.NotFound("No leave types found.");
                }

                return ApiResponse<IEnumerable<LeaveTypeDto>>.Ok(MapToDto(leaveTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Instance: {InstanceId}] Some error occured while fetching leave types", _instanceId);
                return ApiResponse<IEnumerable<LeaveTypeDto>>.InternalError($"Some error occured");
            }
        }

        private LeaveTypeDto MapToDto(LeaveType leaveType) => _mapper.Map<LeaveTypeDto>(leaveType);

        private IEnumerable<LeaveTypeDto> MapToDto(IEnumerable<LeaveType> leaveTypes)
        {
            return leaveTypes.Select(leaveType => _mapper.Map<LeaveTypeDto>(leaveType));
        }

    }
}