using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.ETL.Processors
{
    public interface IPipelineProcessor
    {
        Task<PipelineResult> ProcessAsync(PipelineContext context);
        Task<PipelineStatus> GetStatusAsync(string pipelineId);
        Task<bool> CancelAsync(string pipelineId);
    }

    public class PipelineContext
    {
        public string PipelineId { get; set; }
        public DataSourceDefinition Source { get; set; }
        public List<PipelineStage> Stages { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    public class PipelineStage
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public StageType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public List<string> DependsOn { get; set; }
    }

    public enum StageType
    {
        Extract,
        Transform,
        Load,
        Validate,
        Enrich,
        Custom
    }

    public class PipelineResult
    {
        public string PipelineId { get; set; }
        public PipelineExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long RecordsProcessed { get; set; }
        public List<StageResult> StageResults { get; set; }
        public List<string> Errors { get; set; }
        public Dictionary<string, object> OutputParameters { get; set; }
    }

    public class StageResult
    {
        public string StageId { get; set; }
        public StageExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long RecordsProcessed { get; set; }
        public List<string> Errors { get; set; }
    }

    public enum PipelineExecutionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public enum StageExecutionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Skipped
    }

    public class PipelineStatus
    {
        public string PipelineId { get; set; }
        public PipelineExecutionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long RecordsProcessed { get; set; }
        public Dictionary<string, StageExecutionStatus> StageStatuses { get; set; }
    }
}
