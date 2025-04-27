using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Errors;
using GenericDataPlatform.ETL.Validators;
using GenericDataPlatform.ETL.Workflows.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions globally
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");

            var statusCode = HttpStatusCode.InternalServerError;
            var errorCode = ErrorCodes.InternalServerError;
            var errorMessage = "An unexpected error occurred";
            var errorDetails = new Dictionary<string, object>();

            // Determine the status code and error code based on the exception type
            if (exception is ApiException apiException)
            {
                statusCode = apiException.StatusCode;
                errorCode = apiException.ErrorCode;
                errorMessage = apiException.Message;
            }
            else if (exception is ValidationException validationException)
            {
                statusCode = HttpStatusCode.BadRequest;
                errorCode = ErrorCodes.ValidationError;
                errorMessage = validationException.Message;
                errorDetails["validationResult"] = validationException.ValidationResult;
            }
            else if (exception is WorkflowStepFailedException workflowException)
            {
                statusCode = HttpStatusCode.InternalServerError;
                errorCode = ErrorCodes.WorkflowStepFailed;
                errorMessage = workflowException.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                errorCode = ErrorCodes.Unauthorized;
                errorMessage = "Unauthorized access";
            }
            else if (exception is ArgumentException)
            {
                statusCode = HttpStatusCode.BadRequest;
                errorCode = ErrorCodes.BadRequest;
                errorMessage = exception.Message;
            }

            // Create the error response
            var response = new ApiErrorResponse
            {
                Error = new ApiError
                {
                    Code = errorCode,
                    Message = errorMessage,
                    Details = errorDetails
                },
                RequestId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };

            // Include stack trace in development environment
            if (_environment.IsDevelopment())
            {
                response.Error.Details["stackTrace"] = exception.StackTrace;

                if (exception.InnerException != null)
                {
                    response.Error.InnerErrors.Add(ApiError.FromException(exception.InnerException, true));
                }
            }

            // Set the response
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Serialize the response
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }
    }
}
