using System.Net;
using System.Text.Json.Serialization;

namespace Shared.Common.DTOs;

/// <summary>
/// Unified API response envelope used across all services.
/// Every endpoint returns this shape so clients have a predictable contract.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// True when StatusCode is 2xx.
    /// </summary>
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;

    /// <summary>
    /// HTTP status code mirrored in the body for client convenience.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Populated on success; null on failure.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// FluentValidation errors; empty on success or non-validation failures.
    /// </summary>
    public List<string> Errors { get; private set; } = new();

    [JsonIgnore]
    public HttpStatusCode HttpStatusCode => (HttpStatusCode)StatusCode;

    public static ApiResponse<T> Ok(T data) => new() { Data = data, StatusCode = HttpStatusCode.OK };

    public static ApiResponse<T> Created(T data) => new() { Data = data, StatusCode = HttpStatusCode.Created };

    public static ApiResponse<T> NoContent() => new() { StatusCode = HttpStatusCode.NoContent };

    public static ApiResponse<T> NotFound(string message) => new() { StatusCode = HttpStatusCode.NotFound, Errors = [message] };

    public static ApiResponse<T> BadRequest(string message) => new() { StatusCode = HttpStatusCode.BadRequest, Errors = [message] };

    public static ApiResponse<T> BadRequest(List<string> messages) => new() { StatusCode = HttpStatusCode.BadRequest, Errors = messages };

    public static ApiResponse<T> Conflict(string message) => new() { StatusCode = HttpStatusCode.Conflict, Errors = [message] };

    public static ApiResponse<T> Forbidden(string message) => new() { StatusCode = HttpStatusCode.Forbidden, Errors = [message] };

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized.") => new() { StatusCode = HttpStatusCode.Unauthorized, Errors = [message] };

    public static ApiResponse<T> UnprocessableEntity(List<string> errors) => new() { StatusCode = HttpStatusCode.UnprocessableEntity, Errors = errors };

    public static ApiResponse<T> InternalError(string message = "An unexpected error occurred.") => new() { StatusCode = HttpStatusCode.InternalServerError, Errors = [message] };

    /// <summary>
    /// Fluent validation failure builder — use static factories for new code.
    /// </summary>
    public ApiResponse<T> Fail(HttpStatusCode statusCode, List<string> validationErrors)
    {
        StatusCode = statusCode;
        Errors = validationErrors;
        return this;
    }

    /// <summary>
    /// Fluent success builder — use static factories for new code.
    /// </summary>
    public ApiResponse<T> Success(T data, HttpStatusCode statusCode)
    {
        Data = data;
        StatusCode = statusCode;
        return this;
    }
}