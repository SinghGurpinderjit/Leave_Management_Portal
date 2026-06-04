using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace LeaveManagementService.Extensions;

/// <summary>
/// Extension methods for configuring Polly resilience policies (Retry + Circuit Breaker)
/// Supports both Simple and Advanced circuit breaker modes via configuration
/// </summary>
public static class ResiliencePolicyExtensions
{
    /// <summary>
    /// Adds Polly resilience policies to HttpClient (Retry with Jitter + Circuit Breaker)
    /// Reads configuration from appsettings.json under "Resilience" section
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(
        this IHttpClientBuilder httpClientBuilder,
        IConfiguration configuration)
    {
        // Read configuration
        var enablePolly = configuration.GetValue<bool>("Resilience:EnablePolly", true);

        if (!enablePolly)
        {
            Log.Information("Polly resilience policies: Disabled");
            return httpClientBuilder;
        }

        var retryCount = configuration.GetValue<int>("Resilience:RetryPolicy:RetryCount", 3);
        var backoffPower = configuration.GetValue<int>("Resilience:RetryPolicy:BackoffPower", 2);
        var useAdvancedCircuitBreaker = configuration.GetValue<bool>("Resilience:CircuitBreaker:UseAdvanced", true);

        Log.Information("Polly resilience policies: Enabled, Circuit Breaker Mode: {Mode}",
            useAdvancedCircuitBreaker ? "Advanced (rate-based)" : "Simple (consecutive)");

        // Create and apply policies
        var retryPolicy = CreateRetryPolicyWithJitter(retryCount, backoffPower);
        var circuitBreakerPolicy = useAdvancedCircuitBreaker
            ? CreateAdvancedCircuitBreakerPolicy(configuration)
            : CreateSimpleCircuitBreakerPolicy(configuration);

        // Apply policies in order: circuit breaker first, then retry
        // 
        // POLICY ORDER: Circuit Breaker (OUTER) → Retry (INNER) → HTTP Call
        // 
        // Behavior:
        // 1. Circuit OPEN → Immediate rejection (no retry attempts, fail fast)
        // 2. Circuit CLOSED → Retry policy handles transient failures
        // 3. Circuit HALF-OPEN → Test call goes through retry policy
        // 
        // This order is more efficient: when circuit is open, no wasted retry delays
        httpClientBuilder
            .AddPolicyHandler(circuitBreakerPolicy)
            .AddPolicyHandler(retryPolicy);

        Log.Information("✅ Polly policies applied: Circuit Breaker ({Mode}) → Retry with jitter",
            useAdvancedCircuitBreaker ? "Advanced" : "Simple");

        return httpClientBuilder;
    }

    /// <summary>
    /// Creates retry policy with exponential backoff and jitter
    /// Jitter prevents "thundering herd" when multiple clients retry simultaneously
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicyWithJitter(
        int retryCount,
        int backoffPower)
    {
        var jitterer = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError()  // Handles network errors, 5xx, 408 timeout
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    // Exponential backoff: 2^1=2s, 2^2=4s, 2^3=8s
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(backoffPower, retryAttempt));

                    // Add jitter: +0% to +50% of base delay
                    // Spreads retries randomly to avoid load spikes
                    var jitter = TimeSpan.FromMilliseconds(
                        baseDelay.TotalMilliseconds * jitterer.NextDouble() * 0.5);

                    return baseDelay + jitter;
                },
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Log.Warning("[POLLY-RETRY] Attempt {RetryCount}/{MaxRetries} after {Delay}ms delay | Reason: {Exception}",
                        retryAttempt,
                        retryCount,
                        (int)timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Creates SIMPLE circuit breaker: Opens after N consecutive failures
    /// Easier to understand, best for simple scenarios
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> CreateSimpleCircuitBreakerPolicy(
        IConfiguration configuration)
    {
        var failureThreshold = configuration.GetValue<int>(
            "Resilience:CircuitBreaker:Simple:FailureThreshold", 3);
        var durationOfBreak = configuration.GetValue<int>(
            "Resilience:CircuitBreaker:Simple:DurationOfBreakSeconds", 30);

        Log.Information("SIMPLE Circuit Breaker configured: ConsecutiveFailures={Threshold}, Break={Break}s",
            failureThreshold, durationOfBreak);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(durationOfBreak),
                onBreak: (outcome, timespan) =>
                {
                    Log.Error("[POLLY-CIRCUIT] ⚠️ SIMPLE Circuit Breaker OPENED | Duration: {Duration}s | Reason: {Threshold} consecutive failures",
                        (int)timespan.TotalSeconds, failureThreshold);
                },
                onReset: () =>
                {
                    Log.Information("[POLLY-CIRCUIT] ✅ SIMPLE Circuit Breaker CLOSED | Service recovered");
                });
    }

    /// <summary>
    /// Creates ADVANCED circuit breaker: Opens when failure rate exceeds threshold in time window
    /// More sophisticated, best for production with variable load
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> CreateAdvancedCircuitBreakerPolicy(
        IConfiguration configuration)
    {
        var failureThreshold = configuration.GetValue<double>(
            "Resilience:CircuitBreaker:Advanced:FailureThreshold", 0.75);
        var samplingDuration = configuration.GetValue<int>(
            "Resilience:CircuitBreaker:Advanced:SamplingDurationSeconds", 60);
        var minimumThroughput = configuration.GetValue<int>(
            "Resilience:CircuitBreaker:Advanced:MinimumThroughput", 3);
        var durationOfBreak = configuration.GetValue<int>(
            "Resilience:CircuitBreaker:Advanced:DurationOfBreakSeconds", 30);

        Log.Information("ADVANCED Circuit Breaker configured: FailureRate>{Threshold}%, Sampling={Sampling}s, MinThroughput={MinThroughput}, Break={Break}s",
            failureThreshold * 100, samplingDuration, minimumThroughput, durationOfBreak);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: failureThreshold,
                samplingDuration: TimeSpan.FromSeconds(samplingDuration),
                minimumThroughput: minimumThroughput,
                durationOfBreak: TimeSpan.FromSeconds(durationOfBreak),
                onBreak: (outcome, timespan) =>
                {
                    Log.Error("[POLLY-CIRCUIT] ⚠️ ADVANCED Circuit Breaker OPENED | Duration: {Duration}s | Reason: Failure rate {Threshold}% exceeded in {Sampling}s window",
                        (int)timespan.TotalSeconds, (int)(failureThreshold * 100), samplingDuration);
                },
                onReset: () =>
                {
                    Log.Information("[POLLY-CIRCUIT] ✅ ADVANCED Circuit Breaker CLOSED | Service recovered");
                },
                onHalfOpen: () =>
                {
                    Log.Warning("[POLLY-CIRCUIT] 🔄 ADVANCED Circuit Breaker HALF-OPEN | Testing recovery...");
                });
    }
}
