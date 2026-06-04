using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Common.Constants;
using Shared.Common.ExceptionHandling;
using System.Net;

namespace Shared.Common.Middleware
{
    public class CustomExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private const string applicationJson = "application/problem+json";

        public CustomExceptionMiddleware(RequestDelegate next, ILogger<CustomExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (CustomException ex)
            {
                _logger.LogError(ex, AppConstants.ExceptionMessages.ExceptionErrorMessage, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, AppConstants.ExceptionMessages.ExceptionErrorMessage, ex.Message);
                await HandleExceptionAsync(context, new CustomException(ex));
            }
        }


        private async Task HandleExceptionAsync(HttpContext context, CustomException exception)
        {
            CustomExceptionDetails details = InitExceptionDetails(context, exception);
            await HandleException(context, exception, details);
        }


        /// <summary>Initializes the exception details with custom extensions.</summary>
        /// <param name="context">The context.</param>
        /// <param name="code">The ExceptionCode.</param>
        /// <param name="customExtensions">Custom exception extensions to provide additional information to consumers.</param>
        /// <returns></returns>
        private CustomExceptionDetails InitExceptionDetails(HttpContext context, CustomException exception)
        {
            return new CustomExceptionDetails()
            {
                TraceIdentifier = context.TraceIdentifier,
                Path = context.Request.Path,
                HttpStatusCode = GetDefaultHttpStatusCodeIfZero(exception.HttpStatusCode),
                Exception = exception
            };
        }

        private HttpStatusCode GetDefaultHttpStatusCodeIfZero(HttpStatusCode httpStatusCode)
        {
            return httpStatusCode > 0 ? httpStatusCode : HttpStatusCode.InternalServerError;
        }
        private async Task HandleException(HttpContext context, CustomException exception, CustomExceptionDetails details)
        {
            _logger.LogError(exception, AppConstants.ExceptionMessages.ExceptionErrorMessage, details.ToString());
            CustomProblemDetails problemDetails = CreateProblemDetails(context, exception, details);
            await context.Response.WriteAsync(problemDetails.ToString());
        }


        /// <summary>
        /// Creates the problem details.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="details">The details.</param>
        /// <returns>The dunnhumby problem details.</returns>
        private CustomProblemDetails CreateProblemDetails(HttpContext context, CustomException exception, CustomExceptionDetails details)
        {
            context.Response.ContentType = applicationJson;
            context.Response.StatusCode = (int)GetDefaultHttpStatusCodeIfZero(details.HttpStatusCode);

            var problemDetails = new CustomProblemDetails
            {
                Type = exception.GetType().Name,
                Title = exception.Message,
                Status = (int)details.HttpStatusCode,
                Instance = details.Path,
                Detail = exception.InnerException == null ? String.Empty : exception.InnerException.Message
            };

            return problemDetails;
        }
    }
}

