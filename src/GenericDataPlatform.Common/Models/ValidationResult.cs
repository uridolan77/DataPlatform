using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Common.Models
{
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Gets or sets additional metadata about the validation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the property path that caused the error.
        /// </summary>
        public string PropertyPath { get; set; }

        /// <summary>
        /// Gets or sets the severity of the error.
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    }

    /// <summary>
    /// Represents the severity of a validation error.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information only, does not affect validation result.
        /// </summary>
        Information,

        /// <summary>
        /// Warning, does not affect validation result but should be addressed.
        /// </summary>
        Warning,

        /// <summary>
        /// Error, affects validation result and must be fixed.
        /// </summary>
        Error,

        /// <summary>
        /// Critical error, affects validation result and must be fixed immediately.
        /// </summary>
        Critical
    }
}
