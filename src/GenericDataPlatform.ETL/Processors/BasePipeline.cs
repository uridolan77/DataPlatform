using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Processors
{
    public abstract class BasePipeline : IPipelineProcessor
    {
        protected readonly ILogger _logger;
        protected readonly Dictionary<string, PipelineStatus> _pipelineStatuses;

        protected BasePipeline(ILogger logger)
        {
            _logger = logger;
            _pipelineStatuses = new Dictionary<string, PipelineStatus>();
        }

        public abstract Task<PipelineResult> ProcessAsync(PipelineContext context);

        public Task<PipelineStatus> GetStatusAsync(string pipelineId)
        {
            if (_pipelineStatuses.TryGetValue(pipelineId, out var status))
            {
                return Task.FromResult(status);
            }

            return Task.FromResult<PipelineStatus>(null);
        }

        public abstract Task<bool> CancelAsync(string pipelineId);

        protected void UpdatePipelineStatus(string pipelineId, PipelineExecutionStatus status, long recordsProcessed = 0)
        {
            if (!_pipelineStatuses.TryGetValue(pipelineId, out var pipelineStatus))
            {
                pipelineStatus = new PipelineStatus
                {
                    PipelineId = pipelineId,
                    Status = status,
                    StartTime = DateTime.UtcNow,
                    RecordsProcessed = recordsProcessed,
                    StageStatuses = new Dictionary<string, StageExecutionStatus>()
                };
                _pipelineStatuses[pipelineId] = pipelineStatus;
            }
            else
            {
                pipelineStatus.Status = status;
                pipelineStatus.RecordsProcessed = recordsProcessed;
                
                if (status == PipelineExecutionStatus.Completed || 
                    status == PipelineExecutionStatus.Failed || 
                    status == PipelineExecutionStatus.Cancelled)
                {
                    pipelineStatus.EndTime = DateTime.UtcNow;
                }
            }
        }

        protected void UpdateStageStatus(string pipelineId, string stageId, StageExecutionStatus status)
        {
            if (_pipelineStatuses.TryGetValue(pipelineId, out var pipelineStatus))
            {
                pipelineStatus.StageStatuses[stageId] = status;
            }
        }

        protected bool AreDependenciesCompleted(PipelineStage stage, Dictionary<string, StageExecutionStatus> stageStatuses)
        {
            if (stage.DependsOn == null || !stage.DependsOn.Any())
                return true;

            return stage.DependsOn.All(dependencyId => 
                stageStatuses.TryGetValue(dependencyId, out var status) && 
                status == StageExecutionStatus.Completed);
        }

        protected void LogPipelineStart(PipelineContext context)
        {
            _logger.LogInformation("Starting pipeline {PipelineId} for source {SourceName}", 
                context.PipelineId, context.Source.Name);
        }

        protected void LogPipelineEnd(PipelineResult result)
        {
            if (result.Status == PipelineExecutionStatus.Completed)
            {
                _logger.LogInformation("Pipeline {PipelineId} completed successfully. Processed {RecordsProcessed} records in {Duration} seconds", 
                    result.PipelineId, result.RecordsProcessed, (result.EndTime - result.StartTime).TotalSeconds);
            }
            else if (result.Status == PipelineExecutionStatus.Failed)
            {
                _logger.LogError("Pipeline {PipelineId} failed. Errors: {Errors}", 
                    result.PipelineId, string.Join("; ", result.Errors));
            }
            else if (result.Status == PipelineExecutionStatus.Cancelled)
            {
                _logger.LogWarning("Pipeline {PipelineId} was cancelled", result.PipelineId);
            }
        }

        protected void LogStageStart(string pipelineId, PipelineStage stage)
        {
            _logger.LogInformation("Starting stage {StageId} ({StageName}) of type {StageType} in pipeline {PipelineId}", 
                stage.Id, stage.Name, stage.Type, pipelineId);
        }

        protected void LogStageEnd(string pipelineId, StageResult result)
        {
            if (result.Status == StageExecutionStatus.Completed)
            {
                _logger.LogInformation("Stage {StageId} in pipeline {PipelineId} completed successfully. Processed {RecordsProcessed} records in {Duration} seconds", 
                    result.StageId, pipelineId, result.RecordsProcessed, (result.EndTime - result.StartTime).TotalSeconds);
            }
            else if (result.Status == StageExecutionStatus.Failed)
            {
                _logger.LogError("Stage {StageId} in pipeline {PipelineId} failed. Errors: {Errors}", 
                    result.StageId, pipelineId, string.Join("; ", result.Errors));
            }
            else if (result.Status == StageExecutionStatus.Skipped)
            {
                _logger.LogWarning("Stage {StageId} in pipeline {PipelineId} was skipped", 
                    result.StageId, pipelineId);
            }
        }
    }
}
