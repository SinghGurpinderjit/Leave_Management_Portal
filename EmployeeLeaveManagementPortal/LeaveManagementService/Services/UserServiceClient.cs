namespace LeaveManagementService.Services
{
    using LeaveManagementService.Models;
    using LeaveManagementService.Services.Contracts;
    using Microsoft.Extensions.Options;
    using Shared.Common.DTOs;
    using Shared.Common.DTOs.Employee;
    using System.Net.Http.Headers;

    public class UserServiceClient : IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserServiceClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ServiceDiscoverySettings _settings;
        private const string XCorrelationID = "X-Correlation-ID";

        public UserServiceClient(
            HttpClient httpClient,
            ILogger<UserServiceClient> logger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<ServiceDiscoverySettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _settings = settings.Value;

            if (_settings.UseEureka)
            {
                _httpClient.BaseAddress = new Uri($"https://{_settings.UserService.ServiceName}");
                _logger.LogInformation("UserServiceClient configured with Eureka service name: {ServiceName}",
                    _settings.UserService.ServiceName);
            }
            else
            {
                _httpClient.BaseAddress = new Uri($"{_settings.UserService.DirectUrl}");
                _logger.LogInformation("UserServiceClient configured with direct URL: {Url}",
                    _settings.UserService.DirectUrl); //"https://localhost:7092");
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            try
            {
                SetAuthorizationHeader();
                SetCorrelationIdHeader();

                _logger.LogInformation("Calling User Service to get employee: {UserId}", userId);

                var response = await _httpClient.GetAsync($"/api/users/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
                    _logger.LogInformation("Successfully retrieved user {UserId}", userId);

                    if (apiResponse == null)
                        return null;

                    if (!apiResponse.IsSuccess)
                    {
                        return null;
                        //throw new Exception(apiResponse.Message);
                    }
                    return apiResponse.Data;
                }

                _logger.LogInformation("Failed to get user {UserId}, Status: {StatusCode}",
                    userId, response.StatusCode);

                // 5xx = server error — worth retrying via Polly
                if ((int)response.StatusCode >= 500)
                    throw new HttpRequestException($"UserService returned {response.StatusCode}");

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Error calling User Service for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateUserAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            return user != null;
        }

        public async Task<TeamMembersDto?> GetTeamMembersAsync(Guid userId)
        {
            try
            {
                SetAuthorizationHeader();
                SetCorrelationIdHeader();

                _logger.LogInformation("Calling User Service to get team members for user: {UserId}", userId);

                var response = await _httpClient.GetAsync($"/api/users/{userId}/team-members");

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TeamMembersDto>>();

                    _logger.LogInformation("Successfully retrieved team members for user {UserId}", userId);

                    if (apiResponse == null)
                        return null;

                    if (!apiResponse.IsSuccess)
                    {
                        throw new Exception(string.Join(',', apiResponse.Errors));
                    }
                    return apiResponse.Data;
                }

                _logger.LogInformation("Failed to get team members for user {UserId}, Status: {StatusCode}",
                    userId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Error calling User Service for geting team members for user: {UserId}", userId);
                throw;
            }

        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
            }
        }

        private void SetCorrelationIdHeader()
        {
            var correlationId = _httpContextAccessor.HttpContext?.Request.Headers[XCorrelationID].ToString();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(XCorrelationID, correlationId);
            }
        }
    }
}
