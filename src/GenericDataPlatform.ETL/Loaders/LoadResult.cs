using System;
using System.Collections.Generic;

namespace GenericDataPlatform.ETL.Loaders
{
    /// <summary>
    /// Represents the result of a data loading operation
    /// </summary>
    public class LoadResult
    {
        /// <summary>
        /// Gets or sets whether the load operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Gets or sets the number of records processed
        /// </summary>
        public long RecordsProcessed { get; set; }
        
        /// <summary>
        /// Gets or sets the number of records successfully loaded
        /// </summary>
        public long RecordsLoaded { get; set; }
        
        /// <summary>
        /// Gets or sets the number of records that failed to load
        /// </summary>
        public long RecordsFailed { get; set; }
        
        /// <summary>
        /// Gets or sets the destination ID
        /// </summary>
        public string DestinationId { get; set; }
        
        /// <summary>
        /// Gets or sets the destination name
        /// </summary>
        public string DestinationName { get; set; }
        
        /// <summary>
        /// Gets or sets the destination type
        /// </summary>
        public string DestinationType { get; set; }
        
        /// <summary>
        /// Gets or sets the start time of the load operation
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the end time of the load operation
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the duration of the load operation in milliseconds
        /// </summary>
        public double DurationMs => (EndTime - StartTime).TotalMilliseconds;
        
        /// <summary>
        /// Gets or sets the error message if the load operation failed
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the error details if the load operation failed
        /// </summary>
        public string ErrorDetails { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the load operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a successful load result
        /// </summary>
        public static LoadResult Success(string destinationId, long recordsProcessed, Dictionary<string, object> metadata = null)
        {
            return new LoadResult
            {
                IsSuccess = true,
                DestinationId = destinationId,
                RecordsProcessed = recordsProcessed,
                RecordsLoaded = recordsProcessed,
                RecordsFailed = 0,
                StartTime = DateTime.UtcNow.AddSeconds(-1), // Approximate
                EndTime = DateTime.UtcNow,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }
        
        /// <summary>
        /// Creates a failed load result
        /// </summary>
        public static LoadResult Failure(string destinationId, string errorMessage, string errorDetails = null, long recordsProcessed = 0, long recordsLoaded = 0, Dictionary<string, object> metadata = null)
        {
            return new LoadResult
            {
                IsSuccess = false,
                DestinationId = destinationId,
                RecordsProcessed = recordsProcessed,
                RecordsLoaded = recordsLoaded,
                RecordsFailed = recordsProcessed - recordsLoaded,
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails,
                StartTime = DateTime.UtcNow.AddSeconds(-1), // Approximate
                EndTime = DateTime.UtcNow,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }
    }
}
