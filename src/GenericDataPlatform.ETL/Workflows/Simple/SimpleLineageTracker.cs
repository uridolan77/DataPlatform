using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Tracking;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Simple
{
    /// <summary>
    /// A simple implementation of ILineageTracker
    /// </summary>
    public class SimpleLineageTracker : ILineageTracker
    {
        private readonly IDataLineageService _lineageService;
        private readonly ILogger<SimpleLineageTracker> _logger;

        public SimpleLineageTracker(IDataLineageService lineageService, ILogger<SimpleLineageTracker> logger)
        {
            _lineageService = lineageService;
            _logger = logger;
        }

        /// <summary>
        /// Tracks a data extraction event
        /// </summary>
        public async Task TrackExtractionAsync(WorkflowStep step, WorkflowContext context, object output)
        {
            _logger.LogInformation("Tracking extraction in workflow {WorkflowId}, step {StepId}",
                context.WorkflowId, step.Id);

            try
            {
                // Get source information from step configuration
                string sourceType = GetConfigValue<string>(step.Configuration, "extractorType");
                string sourceLocation = GetConfigValue<string>(step.Configuration, "source") ?? context.Source;

                var lineageEvent = new LineageEvent
                {
                    SourceEntityId = sourceLocation,
                    SourceEntityType = sourceType,
                    TargetEntityId = $"{context.WorkflowId}:{step.Id}",
                    TargetEntityType = "WorkflowStep",
                    EventType = "Extract",
                    Timestamp = DateTime.UtcNow,
                    UserId = "system",
                    Metadata = new Dictionary<string, string>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["sourceType"] = sourceType,
                        ["recordCount"] = GetRecordCount(output).ToString()
                    }
                };

                await _lineageService.RecordLineageEventAsync(lineageEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking extraction lineage");
            }
        }

        /// <summary>
        /// Tracks a data transformation event
        /// </summary>
        public async Task TrackTransformationAsync(WorkflowStep step, WorkflowContext context, object input, object output)
        {
            _logger.LogInformation("Tracking transformation in workflow {WorkflowId}, step {StepId}",
                context.WorkflowId, step.Id);

            try
            {
                string sourceStepId = null;
                if (step.DependsOn != null && step.DependsOn.Count > 0)
                {
                    sourceStepId = step.DependsOn[0];
                }

                var lineageEvent = new LineageEvent
                {
                    SourceEntityId = sourceStepId != null ? $"{context.WorkflowId}:{sourceStepId}" : "unknown",
                    SourceEntityType = "WorkflowStep",
                    TargetEntityId = $"{context.WorkflowId}:{step.Id}",
                    TargetEntityType = "WorkflowStep",
                    EventType = "Transform",
                    Timestamp = DateTime.UtcNow,
                    UserId = "system",
                    Metadata = new Dictionary<string, string>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["sourceStepId"] = sourceStepId ?? "unknown",
                        ["recordCount"] = GetRecordCount(output).ToString()
                    }
                };

                await _lineageService.RecordLineageEventAsync(lineageEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking transformation lineage");
            }
        }

        /// <summary>
        /// Tracks a data loading event
        /// </summary>
        public async Task TrackLoadingAsync(WorkflowStep step, WorkflowContext context, object input, string targetLocation)
        {
            _logger.LogInformation("Tracking loading in workflow {WorkflowId}, step {StepId} to destination {DestinationId}",
                context.WorkflowId, step.Id, targetLocation);

            try
            {
                string sourceStepId = null;
                if (step.DependsOn != null && step.DependsOn.Count > 0)
                {
                    sourceStepId = step.DependsOn[0];
                }

                // Get loader type from step configuration
                string loaderType = GetConfigValue<string>(step.Configuration, "loaderType");

                var lineageEvent = new LineageEvent
                {
                    SourceEntityId = sourceStepId != null ? $"{context.WorkflowId}:{sourceStepId}" : "unknown",
                    SourceEntityType = "WorkflowStep",
                    TargetEntityId = targetLocation,
                    TargetEntityType = loaderType ?? "Unknown",
                    EventType = "Load",
                    Timestamp = DateTime.UtcNow,
                    UserId = "system",
                    Metadata = new Dictionary<string, string>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["sourceStepId"] = sourceStepId ?? "unknown",
                        ["destinationType"] = loaderType ?? "Unknown",
                        ["recordCount"] = GetRecordCount(input).ToString()
                    }
                };

                await _lineageService.RecordLineageEventAsync(lineageEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking loading lineage");
            }
        }

        /// <summary>
        /// Gets the record count from the data
        /// </summary>
        private int GetRecordCount(object data)
        {
            if (data == null)
                return 0;

            if (data is IEnumerable<object> enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable)
                {
                    count++;
                }
                return count;
            }

            return 1;
        }

        /// <summary>
        /// Gets a configuration value from a step configuration
        /// </summary>
        private T GetConfigValue<T>(Dictionary<string, object> configuration, string key)
        {
            if (configuration.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }
    }
}
