namespace GenericDataPlatform.Common.Errors
{
    /// <summary>
    /// Standard error codes used across the platform
    /// </summary>
    public static class ErrorCodes
    {
        // General error codes
        public const string InternalServerError = "internal_server_error";
        public const string BadRequest = "bad_request";
        public const string Unauthorized = "unauthorized";
        public const string Forbidden = "forbidden";
        public const string NotFound = "not_found";
        public const string Conflict = "conflict";
        public const string TooManyRequests = "too_many_requests";
        public const string ServiceUnavailable = "service_unavailable";
        public const string GatewayTimeout = "gateway_timeout";
        
        // Validation error codes
        public const string ValidationError = "validation_error";
        public const string RequiredField = "required_field";
        public const string InvalidFormat = "invalid_format";
        public const string InvalidValue = "invalid_value";
        public const string OutOfRange = "out_of_range";
        
        // Authentication error codes
        public const string InvalidCredentials = "invalid_credentials";
        public const string ExpiredToken = "expired_token";
        public const string InvalidToken = "invalid_token";
        public const string AccountLocked = "account_locked";
        
        // Data error codes
        public const string DataNotFound = "data_not_found";
        public const string DuplicateData = "duplicate_data";
        public const string DataValidationFailed = "data_validation_failed";
        public const string SchemaValidationFailed = "schema_validation_failed";
        
        // Connection error codes
        public const string ConnectionFailed = "connection_failed";
        public const string ConnectionTimeout = "connection_timeout";
        public const string ServiceNotAvailable = "service_not_available";
        
        // File error codes
        public const string FileNotFound = "file_not_found";
        public const string FileTooLarge = "file_too_large";
        public const string InvalidFileType = "invalid_file_type";
        public const string FileUploadFailed = "file_upload_failed";
        
        // Workflow error codes
        public const string WorkflowNotFound = "workflow_not_found";
        public const string WorkflowExecutionFailed = "workflow_execution_failed";
        public const string WorkflowStepFailed = "workflow_step_failed";
        public const string WorkflowCancelled = "workflow_cancelled";
    }
}
