using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GenericDataPlatform.Common.Errors
{
    /// <summary>
    /// Represents a standardized API error
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// A unique error code that identifies the error
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// A human-readable error message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// The target of the error (e.g., field name, parameter name)
        /// </summary>
        public string Target { get; set; }
        
        /// <summary>
        /// Additional details about the error
        /// </summary>
        public Dictionary<string, object> Details { get; set; }
        
        /// <summary>
        /// Inner errors that caused this error
        /// </summary>
        public List<ApiError> InnerErrors { get; set; }
        
        /// <summary>
        /// Creates a new instance of ApiError
        /// </summary>
        public ApiError()
        {
            Details = new Dictionary<string, object>();
            InnerErrors = new List<ApiError>();
        }
        
        /// <summary>
        /// Creates a new instance of ApiError with the specified code and message
        /// </summary>
        public ApiError(string code, string message) : this()
        {
            Code = code;
            Message = message;
        }
        
        /// <summary>
        /// Creates a new instance of ApiError with the specified code, message, and target
        /// </summary>
        public ApiError(string code, string message, string target) : this(code, message)
        {
            Target = target;
        }
        
        /// <summary>
        /// Creates a new instance of ApiError from an exception
        /// </summary>
        public static ApiError FromException(Exception exception, bool includeStackTrace = false)
        {
            var error = new ApiError
            {
                Code = ErrorCodes.InternalServerError,
                Message = exception.Message
            };
            
            if (includeStackTrace)
            {
                error.Details["stackTrace"] = exception.StackTrace;
            }
            
            if (exception.InnerException != null)
            {
                error.InnerErrors.Add(FromException(exception.InnerException, includeStackTrace));
            }
            
            return error;
        }
    }
}
