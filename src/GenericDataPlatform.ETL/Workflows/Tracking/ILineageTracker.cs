using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Tracking
{
    /// <summary>
    /// Interface for tracking data lineage events in ETL workflows
    /// </summary>
    public interface ILineageTracker
    {
        /// <summary>
        /// Tracks a data extraction event
        /// </summary>
        Task TrackExtractionAsync(WorkflowStep step, WorkflowContext context, object output);
        
        /// <summary>
        /// Tracks a data transformation event
        /// </summary>
        Task TrackTransformationAsync(WorkflowStep step, WorkflowContext context, object input, object output);
        
        /// <summary>
        /// Tracks a data loading event
        /// </summary>
        Task TrackLoadingAsync(WorkflowStep step, WorkflowContext context, object input, string targetLocation);
    }
}
