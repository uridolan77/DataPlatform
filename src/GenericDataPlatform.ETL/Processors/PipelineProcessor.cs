using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Enrichers;
using GenericDataPlatform.ETL.Extractors.Base;
using GenericDataPlatform.ETL.Loaders.Base;
using GenericDataPlatform.ETL.Transformers.Base;
using GenericDataPlatform.ETL.Validators;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Processors
{
    public class PipelineProcessor : BasePipeline
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IExtractor> _extractors;
        private readonly Dictionary<string, ITransformer> _transformers;
        private readonly Dictionary<string, ILoader> _loaders;
        private readonly Dictionary<string, IValidator> _validators;
        private readonly Dictionary<string, IEnricher> _enrichers;

        public PipelineProcessor(
            IServiceProvider serviceProvider,
            IEnumerable<IExtractor> extractors,
            IEnumerable<ITransformer> transformers,
            IEnumerable<ILoader> loaders,
            IEnumerable<IValidator> validators,
            IEnumerable<IEnricher> enrichers,
            ILogger<PipelineProcessor> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider;

            // Register extractors by type
            _extractors = extractors.ToDictionary(e => e.Type, e => e);

            // Register transformers by type
            _transformers = transformers.ToDictionary(t => t.Type, t => t);

            // Register loaders by type
            _loaders = loaders.ToDictionary(l => l.Type, l => l);

            // Register validators by type
            _validators = validators.ToDictionary(v => v.Type, v => v);

            // Register enrichers by type
            _enrichers = enrichers.ToDictionary(e => e.Type, e => e);
        }

        public override async Task<PipelineResult> ProcessAsync(PipelineContext context)
        {
            try
            {
                LogPipelineStart(context);

                // Initialize the result
                var result = new PipelineResult
                {
                    PipelineId = context.PipelineId,
                    Status = PipelineExecutionStatus.Running,
                    StartTime = DateTime.UtcNow,
                    StageResults = new List<StageResult>(),
                    Errors = new List<string>(),
                    OutputParameters = new Dictionary<string, object>()
                };

                // Update the pipeline status
                UpdatePipelineStatus(context.PipelineId, PipelineExecutionStatus.Running);

                // Process each stage in order, respecting dependencies
                var stageStatuses = new Dictionary<string, StageExecutionStatus>();
                var stageOutputs = new Dictionary<string, object>();
                var recordsProcessed = 0L;

                // Initialize all stages as not started
                foreach (var stage in context.Stages)
                {
                    stageStatuses[stage.Id] = StageExecutionStatus.NotStarted;
                }

                // Process stages until all are completed or failed
                while (stageStatuses.Values.Any(s => s == StageExecutionStatus.NotStarted || s == StageExecutionStatus.Running))
                {
                    // Check if the pipeline has been cancelled
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        result.Status = PipelineExecutionStatus.Cancelled;
                        result.EndTime = DateTime.UtcNow;
                        result.RecordsProcessed = recordsProcessed;

                        UpdatePipelineStatus(context.PipelineId, PipelineExecutionStatus.Cancelled, recordsProcessed);
                        LogPipelineEnd(result);

                        return result;
                    }

                    // Find stages that can be executed (not started and all dependencies completed)
                    var executableStages = context.Stages
                        .Where(s => stageStatuses[s.Id] == StageExecutionStatus.NotStarted &&
                                   AreDependenciesCompleted(s, stageStatuses))
                        .ToList();

                    if (!executableStages.Any())
                    {
                        // No stages can be executed, check if there are any running stages
                        if (stageStatuses.Values.Any(s => s == StageExecutionStatus.Running))
                        {
                            // Wait for running stages to complete
                            await Task.Delay(100);
                            continue;
                        }

                        // No running stages and no executable stages, but there are still not started stages
                        // This means there's a dependency cycle or failed dependencies
                        var notStartedStages = context.Stages
                            .Where(s => stageStatuses[s.Id] == StageExecutionStatus.NotStarted)
                            .ToList();

                        foreach (var stage in notStartedStages)
                        {
                            var stageResult = new StageResult
                            {
                                StageId = stage.Id,
                                Status = StageExecutionStatus.Skipped,
                                StartTime = DateTime.UtcNow,
                                EndTime = DateTime.UtcNow,
                                RecordsProcessed = 0,
                                Errors = new List<string> { "Skipped due to failed dependencies" }
                            };

                            result.StageResults.Add(stageResult);
                            stageStatuses[stage.Id] = StageExecutionStatus.Skipped;

                            UpdateStageStatus(context.PipelineId, stage.Id, StageExecutionStatus.Skipped);
                            LogStageEnd(context.PipelineId, stageResult);
                        }

                        break;
                    }

                    // Execute each stage
                    foreach (var stage in executableStages)
                    {
                        // Update stage status to running
                        stageStatuses[stage.Id] = StageExecutionStatus.Running;
                        UpdateStageStatus(context.PipelineId, stage.Id, StageExecutionStatus.Running);

                        LogStageStart(context.PipelineId, stage);

                        var stageResult = new StageResult
                        {
                            StageId = stage.Id,
                            Status = StageExecutionStatus.Running,
                            StartTime = DateTime.UtcNow,
                            Errors = new List<string>()
                        };

                        try
                        {
                            // Execute the stage based on its type
                            object stageOutput = null;

                            switch (stage.Type)
                            {
                                case StageType.Extract:
                                    stageOutput = await ExecuteExtractStageAsync(stage, context, stageOutputs);
                                    break;

                                case StageType.Transform:
                                    stageOutput = await ExecuteTransformStageAsync(stage, context, stageOutputs);
                                    break;

                                case StageType.Load:
                                    stageOutput = await ExecuteLoadStageAsync(stage, context, stageOutputs);
                                    break;

                                case StageType.Validate:
                                    stageOutput = await ExecuteValidateStageAsync(stage, context, stageOutputs);
                                    break;

                                case StageType.Enrich:
                                    stageOutput = await ExecuteEnrichStageAsync(stage, context, stageOutputs);
                                    break;

                                case StageType.Custom:
                                    stageOutput = await ExecuteCustomStageAsync(stage, context, stageOutputs);
                                    break;
                            }

                            // Store the stage output for use by dependent stages
                            stageOutputs[stage.Id] = stageOutput;

                            // Update stage status to completed
                            stageResult.Status = StageExecutionStatus.Completed;
                            stageResult.EndTime = DateTime.UtcNow;
                            stageResult.RecordsProcessed = GetRecordCount(stageOutput);

                            recordsProcessed += stageResult.RecordsProcessed;

                            stageStatuses[stage.Id] = StageExecutionStatus.Completed;
                            UpdateStageStatus(context.PipelineId, stage.Id, StageExecutionStatus.Completed);
                        }
                        catch (Exception ex)
                        {
                            // Update stage status to failed
                            stageResult.Status = StageExecutionStatus.Failed;
                            stageResult.EndTime = DateTime.UtcNow;
                            stageResult.Errors.Add(ex.Message);

                            stageStatuses[stage.Id] = StageExecutionStatus.Failed;
                            UpdateStageStatus(context.PipelineId, stage.Id, StageExecutionStatus.Failed);

                            _logger.LogError(ex, "Error executing stage {StageId} ({StageName}) of type {StageType} in pipeline {PipelineId}",
                                stage.Id, stage.Name, stage.Type, context.PipelineId);
                        }

                        result.StageResults.Add(stageResult);
                        LogStageEnd(context.PipelineId, stageResult);
                    }
                }

                // Determine the overall pipeline status
                if (stageStatuses.Values.Any(s => s == StageExecutionStatus.Failed))
                {
                    result.Status = PipelineExecutionStatus.Failed;
                    result.Errors.Add("One or more stages failed");
                }
                else
                {
                    result.Status = PipelineExecutionStatus.Completed;
                }

                result.EndTime = DateTime.UtcNow;
                result.RecordsProcessed = recordsProcessed;

                // Update the pipeline status
                UpdatePipelineStatus(context.PipelineId, result.Status, recordsProcessed);

                LogPipelineEnd(result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pipeline {PipelineId}", context.PipelineId);

                var result = new PipelineResult
                {
                    PipelineId = context.PipelineId,
                    Status = PipelineExecutionStatus.Failed,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    RecordsProcessed = 0,
                    StageResults = new List<StageResult>(),
                    Errors = new List<string> { ex.Message },
                    OutputParameters = new Dictionary<string, object>()
                };

                UpdatePipelineStatus(context.PipelineId, PipelineExecutionStatus.Failed);

                return result;
            }
        }

        public override async Task<bool> CancelAsync(string pipelineId)
        {
            if (!_pipelineStatuses.TryGetValue(pipelineId, out var status))
            {
                return false;
            }

            if (status.Status != PipelineExecutionStatus.Running)
            {
                return false;
            }

            // Update the status to cancelled
            status.Status = PipelineExecutionStatus.Cancelled;
            status.EndTime = DateTime.UtcNow;

            return true;
        }

        private async Task<object> ExecuteExtractStageAsync(PipelineStage stage, PipelineContext context, Dictionary<string, object> stageOutputs)
        {
            // Get the extractor type from the configuration
            if (!stage.Configuration.TryGetValue("extractorType", out var extractorType))
            {
                throw new ArgumentException($"Extractor type not specified for stage {stage.Id}");
            }

            // Get the extractor
            if (!_extractors.TryGetValue(extractorType.ToString(), out var extractor))
            {
                throw new ArgumentException($"Extractor of type {extractorType} not found");
            }

            // Execute the extractor
            return await extractor.ExtractAsync(stage.Configuration, context.Source, context.Parameters);
        }

        private async Task<object> ExecuteTransformStageAsync(PipelineStage stage, PipelineContext context, Dictionary<string, object> stageOutputs)
        {
            // Get the transformer type from the configuration
            if (!stage.Configuration.TryGetValue("transformerType", out var transformerType))
            {
                throw new ArgumentException($"Transformer type not specified for stage {stage.Id}");
            }

            // Get the transformer
            if (!_transformers.TryGetValue(transformerType.ToString(), out var transformer))
            {
                throw new ArgumentException($"Transformer of type {transformerType} not found");
            }

            // Get the input data from the dependent stage
            if (!stage.DependsOn.Any())
            {
                throw new ArgumentException($"Transform stage {stage.Id} must depend on at least one other stage");
            }

            var inputStageId = stage.DependsOn.First();
            if (!stageOutputs.TryGetValue(inputStageId, out var inputData))
            {
                throw new ArgumentException($"Input data not found for stage {stage.Id} from dependent stage {inputStageId}");
            }

            // Execute the transformer
            return await transformer.TransformAsync(inputData, stage.Configuration, context.Source);
        }

        private async Task<object> ExecuteLoadStageAsync(PipelineStage stage, PipelineContext context, Dictionary<string, object> stageOutputs)
        {
            // Get the loader type from the configuration
            if (!stage.Configuration.TryGetValue("loaderType", out var loaderType))
            {
                throw new ArgumentException($"Loader type not specified for stage {stage.Id}");
            }

            // Get the loader
            if (!_loaders.TryGetValue(loaderType.ToString(), out var loader))
            {
                throw new ArgumentException($"Loader of type {loaderType} not found");
            }

            // Get the input data from the dependent stage
            if (!stage.DependsOn.Any())
            {
                throw new ArgumentException($"Load stage {stage.Id} must depend on at least one other stage");
            }

            var inputStageId = stage.DependsOn.First();
            if (!stageOutputs.TryGetValue(inputStageId, out var inputData))
            {
                throw new ArgumentException($"Input data not found for stage {stage.Id} from dependent stage {inputStageId}");
            }

            // Execute the loader
            return await loader.LoadAsync(inputData, stage.Configuration, context.Source);
        }

        private async Task<object> ExecuteValidateStageAsync(PipelineStage stage, PipelineContext context, Dictionary<string, object> stageOutputs)
        {
            // Get the validator type from the configuration
            if (!stage.Configuration.TryGetValue("validatorType", out var validatorType))
            {
                throw new ArgumentException($"Validator type not specified for stage {stage.Id}");
            }

            // Get the validator
            if (!_validators.TryGetValue(validatorType.ToString(), out var validator))
            {
                throw new ArgumentException($"Validator of type {validatorType} not found");
            }

            // Get the input data from the dependent stage
            if (!stage.DependsOn.Any())
            {
                throw new ArgumentException($"Validate stage {stage.Id} must depend on at least one other stage");
            }

            var inputStageId = stage.DependsOn.First();
            if (!stageOutputs.TryGetValue(inputStageId, out var inputData))
            {
                throw new ArgumentException($"Input data not found for stage {stage.Id} from dependent stage {inputStageId}");
            }

            // Execute the validator
            var validationResult = await validator.ValidateAsync(inputData, stage.Configuration, context.Source);

            // Check if validation failed and we need to stop the pipeline
            if (!validationResult.IsValid && stage.Configuration.TryGetValue("failOnError", out var failOnErrorObj) &&
                failOnErrorObj is bool failOnError && failOnError)
            {
                throw new ValidationException($"Validation failed for stage {stage.Id}", validationResult);
            }

            // Return the validation result
            return validationResult;
        }

        private async Task<object> ExecuteEnrichStageAsync(PipelineStage stage, PipelineContext context, Dictionary<string, object> stageOutputs)
        {
            // Get the enricher type from the configuration
            if (!stage.Configuration.TryGetValue("enricherType", out var enricherType))
            {
                throw new ArgumentException($"Enricher type not specified for stage {stage.Id}");
            }

            // Get the enricher
            if (!_enrichers.TryGetValue(enricherType.ToString(), out var enricher))
            {
                throw new ArgumentException($"Enricher of type {enricherType} not found");
            }

            // Get the input data from the dependent stage
            if (!stage.DependsOn.Any())
            {
                throw new ArgumentException($"Enrich stage {stage.Id} must depend on at least one other stage");
            }

            var inputStageId = stage.DependsOn.First();
            if (!stageOutputs.TryGetValue(inputStageId, out var inputData))
            {
                throw new ArgumentException($"Input data not found for stage {stage.Id} from dependent stage {inputStageId}");
            }

            // Execute the enricher
            return await enricher.EnrichAsync(inputData, stage.Configuration, context.Source);
        }

        private async Task<object> ExecuteCustomStageAsync(PipelineStage stage, PipelineContext context, Dictionary<string, object> stageOutputs)
        {
            // Get the custom processor type from the configuration
            if (!stage.Configuration.TryGetValue("processorType", out var processorType))
            {
                throw new ArgumentException($"Processor type not specified for custom stage {stage.Id}");
            }

            // Get the processor implementation
            var processorTypeName = processorType.ToString();
            var processorImplementation = Type.GetType(processorTypeName);

            if (processorImplementation == null)
            {
                throw new ArgumentException($"Custom processor type {processorTypeName} not found");
            }

            // Create an instance of the processor
            var processor = _serviceProvider.GetService(processorImplementation);

            if (processor == null)
            {
                throw new ArgumentException($"Could not create an instance of custom processor type {processorTypeName}");
            }

            // Check if the processor implements the required interface
            if (!(processor is ICustomStageProcessor customProcessor))
            {
                throw new ArgumentException($"Custom processor type {processorTypeName} does not implement ICustomStageProcessor");
            }

            // Get the input data from dependent stages
            var inputData = new Dictionary<string, object>();

            if (stage.DependsOn != null && stage.DependsOn.Any())
            {
                foreach (var dependencyId in stage.DependsOn)
                {
                    if (stageOutputs.TryGetValue(dependencyId, out var dependencyOutput))
                    {
                        inputData[dependencyId] = dependencyOutput;
                    }
                }
            }

            // Execute the custom processor
            return await customProcessor.ProcessAsync(inputData, stage.Configuration, context.Source);
        }

        private long GetRecordCount(object stageOutput)
        {
            if (stageOutput == null)
            {
                return 0;
            }

            if (stageOutput is IEnumerable<DataRecord> records)
            {
                return records.Count();
            }

            if (stageOutput is LoadResult loadResult)
            {
                return loadResult.RecordsProcessed;
            }

            if (stageOutput is ValidationResult validationResult)
            {
                return validationResult.RecordsProcessed;
            }

            return 1; // Default to 1 for unknown types
        }
    }

    public class LoadResult
    {
        public long RecordsProcessed { get; set; }
        public string DestinationId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public interface ICustomStageProcessor
    {
        Task<object> ProcessAsync(Dictionary<string, object> inputData, Dictionary<string, object> configuration, DataSourceDefinition source);
    }

    public class ValidationException : Exception
    {
        public ValidationResult ValidationResult { get; }

        public ValidationException(string message, ValidationResult validationResult) : base(message)
        {
            ValidationResult = validationResult;
        }
    }
}
