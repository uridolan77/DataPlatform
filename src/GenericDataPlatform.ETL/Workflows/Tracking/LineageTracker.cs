using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Tracking
{
    /// <summary>
    /// Tracks data lineage events in ETL workflows
    /// </summary>
    public class LineageTracker : ILineageTracker
    {
        private readonly IDataLineageService _lineageService;
        private readonly ILogger<LineageTracker> _logger;

        public LineageTracker(IDataLineageService lineageService, ILogger<LineageTracker> logger)
        {
            _lineageService = lineageService;
            _logger = logger;
        }

        /// <summary>
        /// Tracks a data extraction event
        /// </summary>
        public async Task TrackExtractionAsync(WorkflowStep step, WorkflowContext context, object output)
        {
            try
            {
                _logger.LogInformation("Tracking extraction event for step {StepId}", step.Id);

                // Get source information from step configuration
                string sourceType = GetConfigValue<string>(step.Configuration, "extractorType");
                string sourceLocation = GetConfigValue<string>(step.Configuration, "source") ?? context.Source;

                // Create source entity if it doesn't exist
                var sourceEntity = new DataEntity
                {
                    Name = $"{sourceType} Source: {sourceLocation}",
                    Type = sourceType,
                    Location = sourceLocation,
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id
                    }
                };

                // Create output entity
                var outputEntity = new DataEntity
                {
                    Name = $"Extracted Data: {step.Name}",
                    Type = "ExtractedData",
                    Location = $"memory://{context.WorkflowId}/{step.Id}/output",
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                // Add schema information if available
                if (output != null)
                {
                    outputEntity.Properties["dataType"] = output.GetType().Name;

                    // Add sample data if it's a collection
                    if (output is System.Collections.IEnumerable enumerable)
                    {
                        var sample = new List<object>();
                        var enumerator = enumerable.GetEnumerator();
                        int count = 0;
                        while (enumerator.MoveNext() && count < 5)
                        {
                            sample.Add(enumerator.Current);
                            count++;
                        }

                        outputEntity.Properties["sampleData"] = sample;
                        outputEntity.Properties["isCollection"] = true;
                    }
                    else
                    {
                        outputEntity.Properties["sampleData"] = output;
                        outputEntity.Properties["isCollection"] = false;
                    }
                }

                // Save entities
                await _lineageService.SaveDataEntityAsync(sourceEntity);
                await _lineageService.SaveDataEntityAsync(outputEntity);

                // Create lineage event
                var lineageEvent = new LineageEvent
                {
                    EventType = "Extraction",
                    SourceId = sourceEntity.Id,
                    TargetId = outputEntity.Id,
                    Timestamp = DateTime.UtcNow,
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["stepName"] = step.Name,
                        ["extractorType"] = sourceType
                    }
                };

                // Record lineage event
                await _lineageService.RecordLineageEventAsync(lineageEvent);

                // Store entity IDs in context for later use
                if (context.Metadata == null)
                {
                    context.Metadata = new Dictionary<string, object>();
                }

                if (!context.Metadata.ContainsKey("lineage"))
                {
                    context.Metadata["lineage"] = new Dictionary<string, object>();
                }

                var lineageMetadata = (Dictionary<string, object>)context.Metadata["lineage"];
                lineageMetadata[$"step_{step.Id}_sourceEntityId"] = sourceEntity.Id;
                lineageMetadata[$"step_{step.Id}_outputEntityId"] = outputEntity.Id;

                _logger.LogInformation("Tracked extraction event for step {StepId}", step.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking extraction event for step {StepId}", step.Id);
            }
        }

        /// <summary>
        /// Tracks a data transformation event
        /// </summary>
        public async Task TrackTransformationAsync(WorkflowStep step, WorkflowContext context, object input, object output)
        {
            try
            {
                _logger.LogInformation("Tracking transformation event for step {StepId}", step.Id);

                // Get input entity ID from context
                string inputEntityId = null;
                if (step.DependsOn != null && step.DependsOn.Count > 0)
                {
                    var dependencyId = step.DependsOn[0];
                    if (context.Metadata != null &&
                        context.Metadata.TryGetValue("lineage", out var lineageObj) &&
                        lineageObj is Dictionary<string, object> lineageMetadata &&
                        lineageMetadata.TryGetValue($"step_{dependencyId}_outputEntityId", out var entityIdObj))
                    {
                        inputEntityId = entityIdObj.ToString();
                    }
                }

                // If input entity ID is not found, create a new input entity
                DataEntity inputEntity;
                if (inputEntityId == null)
                {
                    inputEntity = new DataEntity
                    {
                        Name = $"Input Data: {step.Name}",
                        Type = "InputData",
                        Location = $"memory://{context.WorkflowId}/{step.Id}/input",
                        Properties = new Dictionary<string, object>
                        {
                            ["workflowId"] = context.WorkflowId,
                            ["stepId"] = step.Id,
                            ["timestamp"] = DateTime.UtcNow
                        }
                    };

                    // Add schema information if available
                    if (input != null)
                    {
                        inputEntity.Properties["dataType"] = input.GetType().Name;
                    }

                    // Save entity
                    await _lineageService.SaveDataEntityAsync(inputEntity);
                }
                else
                {
                    // Use existing entity
                    inputEntity = await _lineageService.GetDataEntityAsync(inputEntityId);
                    if (inputEntity == null)
                    {
                        _logger.LogWarning("Input entity {EntityId} not found for step {StepId}", inputEntityId, step.Id);
                        return;
                    }
                }

                // Get transformation type from step configuration
                string transformationType = GetConfigValue<string>(step.Configuration, "transformerType");

                // Create output entity
                var outputEntity = new DataEntity
                {
                    Name = $"Transformed Data: {step.Name}",
                    Type = "TransformedData",
                    Location = $"memory://{context.WorkflowId}/{step.Id}/output",
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["transformationType"] = transformationType,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                // Add schema information if available
                if (output != null)
                {
                    outputEntity.Properties["dataType"] = output.GetType().Name;

                    // Add sample data if it's a collection
                    if (output is System.Collections.IEnumerable enumerable)
                    {
                        var sample = new List<object>();
                        var enumerator = enumerable.GetEnumerator();
                        int count = 0;
                        while (enumerator.MoveNext() && count < 5)
                        {
                            sample.Add(enumerator.Current);
                            count++;
                        }

                        outputEntity.Properties["sampleData"] = sample;
                        outputEntity.Properties["isCollection"] = true;
                    }
                    else
                    {
                        outputEntity.Properties["sampleData"] = output;
                        outputEntity.Properties["isCollection"] = false;
                    }
                }

                // Save entity
                await _lineageService.SaveDataEntityAsync(outputEntity);

                // Create lineage event
                var lineageEvent = new LineageEvent
                {
                    EventType = "Transformation",
                    SourceId = inputEntity.Id,
                    TargetId = outputEntity.Id,
                    Timestamp = DateTime.UtcNow,
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["stepName"] = step.Name,
                        ["transformationType"] = transformationType
                    }
                };

                // Add transformation details if available
                if (step.Configuration.TryGetValue("configuration", out var configObj) && configObj != null)
                {
                    lineageEvent.Properties["transformationConfig"] = configObj;
                }

                // Record lineage event
                await _lineageService.RecordLineageEventAsync(lineageEvent);

                // Store entity IDs in context for later use
                if (context.Metadata == null)
                {
                    context.Metadata = new Dictionary<string, object>();
                }

                if (!context.Metadata.ContainsKey("lineage"))
                {
                    context.Metadata["lineage"] = new Dictionary<string, object>();
                }

                var lineageMetadata = (Dictionary<string, object>)context.Metadata["lineage"];
                lineageMetadata[$"step_{step.Id}_inputEntityId"] = inputEntity.Id;
                lineageMetadata[$"step_{step.Id}_outputEntityId"] = outputEntity.Id;

                _logger.LogInformation("Tracked transformation event for step {StepId}", step.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking transformation event for step {StepId}", step.Id);
            }
        }

        /// <summary>
        /// Tracks a data loading event
        /// </summary>
        public async Task TrackLoadingAsync(WorkflowStep step, WorkflowContext context, object input, string targetLocation)
        {
            try
            {
                _logger.LogInformation("Tracking loading event for step {StepId}", step.Id);

                // Get input entity ID from context
                string inputEntityId = null;
                if (step.DependsOn != null && step.DependsOn.Count > 0)
                {
                    var dependencyId = step.DependsOn[0];
                    if (context.Metadata != null &&
                        context.Metadata.TryGetValue("lineage", out var lineageObj) &&
                        lineageObj is Dictionary<string, object> lineageMetadata &&
                        lineageMetadata.TryGetValue($"step_{dependencyId}_outputEntityId", out var entityIdObj))
                    {
                        inputEntityId = entityIdObj.ToString();
                    }
                }

                // If input entity ID is not found, create a new input entity
                DataEntity inputEntity;
                if (inputEntityId == null)
                {
                    inputEntity = new DataEntity
                    {
                        Name = $"Input Data: {step.Name}",
                        Type = "InputData",
                        Location = $"memory://{context.WorkflowId}/{step.Id}/input",
                        Properties = new Dictionary<string, object>
                        {
                            ["workflowId"] = context.WorkflowId,
                            ["stepId"] = step.Id,
                            ["timestamp"] = DateTime.UtcNow
                        }
                    };

                    // Add schema information if available
                    if (input != null)
                    {
                        inputEntity.Properties["dataType"] = input.GetType().Name;
                    }

                    // Save entity
                    await _lineageService.SaveDataEntityAsync(inputEntity);
                }
                else
                {
                    // Use existing entity
                    inputEntity = await _lineageService.GetDataEntityAsync(inputEntityId);
                    if (inputEntity == null)
                    {
                        _logger.LogWarning("Input entity {EntityId} not found for step {StepId}", inputEntityId, step.Id);
                        return;
                    }
                }

                // Get loader type from step configuration
                string loaderType = GetConfigValue<string>(step.Configuration, "loaderType");

                // Create target entity
                var targetEntity = new DataEntity
                {
                    Name = $"Target: {targetLocation}",
                    Type = loaderType,
                    Location = targetLocation,
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                // Save entity
                await _lineageService.SaveDataEntityAsync(targetEntity);

                // Create lineage event
                var lineageEvent = new LineageEvent
                {
                    EventType = "Loading",
                    SourceId = inputEntity.Id,
                    TargetId = targetEntity.Id,
                    Timestamp = DateTime.UtcNow,
                    Properties = new Dictionary<string, object>
                    {
                        ["workflowId"] = context.WorkflowId,
                        ["stepId"] = step.Id,
                        ["stepName"] = step.Name,
                        ["loaderType"] = loaderType,
                        ["targetLocation"] = targetLocation
                    }
                };

                // Record lineage event
                await _lineageService.RecordLineageEventAsync(lineageEvent);

                // Store entity IDs in context for later use
                if (context.Metadata == null)
                {
                    context.Metadata = new Dictionary<string, object>();
                }

                if (!context.Metadata.ContainsKey("lineage"))
                {
                    context.Metadata["lineage"] = new Dictionary<string, object>();
                }

                var lineageMetadata = (Dictionary<string, object>)context.Metadata["lineage"];
                lineageMetadata[$"step_{step.Id}_inputEntityId"] = inputEntity.Id;
                lineageMetadata[$"step_{step.Id}_targetEntityId"] = targetEntity.Id;

                _logger.LogInformation("Tracked loading event for step {StepId}", step.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking loading event for step {StepId}", step.Id);
            }
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
