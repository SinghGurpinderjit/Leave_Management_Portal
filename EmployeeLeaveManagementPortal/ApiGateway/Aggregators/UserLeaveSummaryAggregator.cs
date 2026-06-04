using Ocelot.Middleware;
using Ocelot.Multiplexer;
using System.Net;
using System.Text.Json;
using System.Text;

namespace ApiGateway.Aggregators
{
    /// <summary>
    /// Custom Ocelot aggregator that combines responses from UserService and LeaveManagementService
    /// into a single unified response. This allows clients to fetch user details and their
    /// leave requests with current leave balance within a single API call instead of making three separate requests.
    /// 
    /// Usage: Configure in ocelot.json with "Aggregates" section pointing to multiple routes
    /// </summary>
    public class UserLeaveSummaryAggregator : IDefinedAggregator
    {

        private readonly ILogger<UserLeaveSummaryAggregator> _logger;

        public UserLeaveSummaryAggregator(ILogger<UserLeaveSummaryAggregator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Aggregates multiple downstream service responses into a single response.
        /// Expected to receive exactly 3 responses: one from user-service and two from leave-service.
        /// </summary>
        /// <param name="responses">List of HttpContext objects containing downstream responses</param>
        /// <returns>A single DownstreamResponse combining both services' data</returns>
        public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
        {
            _logger.LogInformation(">>> UserLeaveSummaryAggregator HIT");

            // Validate that we received exactly 2 responses as expected
            // If aggregator is misconfigured in ocelot.json, this will fail
            if (responses.Count != 3)
            {
                return new DownstreamResponse(
                    new StringContent(
                        JsonSerializer.Serialize(new { error = "Invalid number of responses" }),
                        Encoding.UTF8,
                        "application/json"),
                    HttpStatusCode.InternalServerError,
                    new List<Header>(),
                    "Error");
            }

            // Extract downstream responses from HttpContext items
            // responses[0] = user-service response
            // responses[1] = leave-service response
            // responses[2] = leave-service response
            var userResponse = responses[0].Items.DownstreamResponse();
            var leaveBalanceResponse = responses[1].Items.DownstreamResponse();
            var leaveRequestsResponse = responses[2].Items.DownstreamResponse();

            // Validate both services returned successful responses (2xx status codes)
            // This prevents returning partial/invalid data to the client

            var userSuccess = (int)userResponse.StatusCode >= 200 && (int)userResponse.StatusCode < 300;
            var leaveRequestsSuccess = (int)leaveRequestsResponse.StatusCode >= 200 && (int)leaveRequestsResponse.StatusCode < 300;
            var leaveBalanceSuccess = (int)leaveBalanceResponse.StatusCode >= 200 && (int)leaveBalanceResponse.StatusCode < 300;

            //if (!userSuccess || !leaveRequestsSuccess || !leaveBalanceSuccess)
            //{
            //    // Return 502 Bad Gateway if either downstream service failed
            //    var failedService = (!leaveBalanceSuccess || !leaveRequestsSuccess) ? "leave-service" : "user-service";
            //    return new DownstreamResponse(
            //        new StringContent(
            //            JsonSerializer.Serialize(new { error = $"Failed to retrieve data from {failedService}" }),
            //            Encoding.UTF8,
            //            "application/json"),
            //        HttpStatusCode.BadGateway,
            //        new List<Header>(),
            //        "Error");
            //}

            // Read JSON content from both downstream services
            var userContent = await userResponse.Content.ReadAsStringAsync();
            var leaveRequestsContent = await leaveRequestsResponse.Content.ReadAsStringAsync();
            var leaveBalanceContent = await leaveBalanceResponse.Content.ReadAsStringAsync();

            // Parse and validate JSON documents
            JsonDocument? userDoc = null;
            JsonDocument? leaveRequestsDoc = null;
            JsonDocument? leaveBalanceDoc = null;

            try
            {
                userDoc = JsonDocument.Parse(userContent);
                leaveRequestsDoc = JsonDocument.Parse(leaveRequestsContent);
                leaveBalanceDoc = JsonDocument.Parse(leaveBalanceContent);

                // Create aggregated response with both user and leave request and leave balance data
                // Structure: { "user": {...}, "leaveRequests": {...} , "leaveBalance": {...} }
                // Structure:
                //{
                //    "isSuccess": true,
                //    "statusCode": 200,
                //    "data": {
                //    },
                //    "errors": []
                //}
                var aggregatedResponse = new
                {
                    user = JsonSerializer.Deserialize<object>(userContent),
                    leaveBalance = JsonSerializer.Deserialize<object>(leaveBalanceContent),
                    leaveRequests = JsonSerializer.Deserialize<object>(leaveRequestsContent)
                };

                // Serialize the combined response
                var jsonContent = JsonSerializer.Serialize(aggregatedResponse);
                var stringContent = new StringContent(
                    jsonContent,
                    Encoding.UTF8,
                    "application/json");

                // Return successful aggregated response
                return new DownstreamResponse(
                    stringContent,
                    HttpStatusCode.OK,
                    new List<Header>
                    {
                    new Header("Content-Type", new[] { "application/json" })
                    },
                    "OK");
            }
            catch (Exception ex)
            {
                // Handle JSON parsing errors or serialization failures
                return new DownstreamResponse(
                    new StringContent(
                        JsonSerializer.Serialize(new { error = "Failed to aggregate responses", details = ex.Message }),
                        Encoding.UTF8,
                        "application/json"),
                    HttpStatusCode.InternalServerError,
                    new List<Header>(),
                    "Error");
            }
            finally
            {
                // Dispose JsonDocument objects to free memory
                userDoc?.Dispose();
                leaveRequestsDoc?.Dispose();
                leaveBalanceDoc?.Dispose();
            }
        }
    }
}
