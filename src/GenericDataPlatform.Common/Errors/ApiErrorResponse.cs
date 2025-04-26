using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GenericDataPlatform.Common.Errors
{
    /// <summary>
    /// Represents a standardized API error response
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// The error details
        /// </summary>
        public ApiError Error { get; set; }
        
        /// <summary>
        /// The request ID that generated this error
        /// </summary>
        public string RequestId { get; set; }
        
        /// <summary>
        /// The timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Creates a new instance of ApiErrorResponse
        /// </summary>
        public ApiErrorResponse()
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Creates a new instance of ApiErrorResponse with the specified error
        /// </summary>
        public ApiErrorResponse(ApiError error) : this()
        {
            Error = error;
        }
        
        /// <summary>
        /// Creates a new instance of ApiErrorResponse with the specified error code and message
        /// </summary>
        public ApiErrorResponse(string errorCode, string errorMessage) : this()
        {
            Error = new ApiError(errorCode, errorMessage);
        }
        
        /// <summary>
        /// Creates a new instance of ApiErrorResponse from an exception
        /// </summary>
        public static ApiErrorResponse FromException(Exception exception, bool includeStackTrace = false)
        {
            return new ApiErrorResponse
            {
                Error = ApiError.FromException(exception, includeStackTrace),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
