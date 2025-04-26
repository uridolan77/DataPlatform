using System;
using System.Net;

namespace GenericDataPlatform.Common.Errors
{
    /// <summary>
    /// Represents an exception that can be converted to an API error response
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// The HTTP status code associated with this exception
        /// </summary>
        public HttpStatusCode StatusCode { get; }
        
        /// <summary>
        /// The error code associated with this exception
        /// </summary>
        public string ErrorCode { get; }
        
        /// <summary>
        /// The target of the error (e.g., field name, parameter name)
        /// </summary>
        public string Target { get; }
        
        /// <summary>
        /// Creates a new instance of ApiException
        /// </summary>
        public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string errorCode = null, string target = null, Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode ?? ErrorCodes.InternalServerError;
            Target = target;
        }
        
        /// <summary>
        /// Converts this exception to an ApiErrorResponse
        /// </summary>
        public ApiErrorResponse ToApiErrorResponse(bool includeStackTrace = false)
        {
            var error = new ApiError
            {
                Code = ErrorCode,
                Message = Message,
                Target = Target
            };
            
            if (includeStackTrace)
            {
                error.Details["stackTrace"] = StackTrace;
            }
            
            if (InnerException != null)
            {
                error.InnerErrors.Add(ApiError.FromException(InnerException, includeStackTrace));
            }
            
            return new ApiErrorResponse(error);
        }
    }
    
    /// <summary>
    /// Represents a validation exception
    /// </summary>
    public class ValidationApiException : ApiException
    {
        public ValidationApiException(string message, string target = null, Exception innerException = null)
            : base(message, HttpStatusCode.BadRequest, ErrorCodes.ValidationError, target, innerException)
        {
        }
    }
    
    /// <summary>
    /// Represents a not found exception
    /// </summary>
    public class NotFoundApiException : ApiException
    {
        public NotFoundApiException(string message, string target = null, Exception innerException = null)
            : base(message, HttpStatusCode.NotFound, ErrorCodes.NotFound, target, innerException)
        {
        }
    }
    
    /// <summary>
    /// Represents an unauthorized exception
    /// </summary>
    public class UnauthorizedApiException : ApiException
    {
        public UnauthorizedApiException(string message, string target = null, Exception innerException = null)
            : base(message, HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized, target, innerException)
        {
        }
    }
    
    /// <summary>
    /// Represents a forbidden exception
    /// </summary>
    public class ForbiddenApiException : ApiException
    {
        public ForbiddenApiException(string message, string target = null, Exception innerException = null)
            : base(message, HttpStatusCode.Forbidden, ErrorCodes.Forbidden, target, innerException)
        {
        }
    }
    
    /// <summary>
    /// Represents a conflict exception
    /// </summary>
    public class ConflictApiException : ApiException
    {
        public ConflictApiException(string message, string target = null, Exception innerException = null)
            : base(message, HttpStatusCode.Conflict, ErrorCodes.Conflict, target, innerException)
        {
        }
    }
    
    /// <summary>
    /// Represents a service unavailable exception
    /// </summary>
    public class ServiceUnavailableApiException : ApiException
    {
        public ServiceUnavailableApiException(string message, string target = null, Exception innerException = null)
            : base(message, HttpStatusCode.ServiceUnavailable, ErrorCodes.ServiceUnavailable, target, innerException)
        {
        }
    }
}
