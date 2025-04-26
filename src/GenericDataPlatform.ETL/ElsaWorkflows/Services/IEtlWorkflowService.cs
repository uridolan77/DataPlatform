using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Services
{
    /// <summary>
    /// Interface for ETL workflow service
    /// </summary>
    public interface IEtlWorkflowService
    {
        /// <summary>
        /// Creates a new workflow definition
        /// </summary>
        Task<string> CreateWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing workflow definition
        /// </summary>
        Task<string> UpdateWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a workflow definition by ID
        /// </summary>
        Task<WorkflowDefinition> GetWorkflowDefinitionAsync(string id, string version = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all workflow definitions
        /// </summary>
        Task<IEnumerable<WorkflowDefinition>> GetWorkflowDefinitionsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes a workflow definition
        /// </summary>
        Task<bool> DeleteWorkflowDefinitionAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a workflow
        /// </summary>
        Task<WorkflowExecutionResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> input = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a workflow execution by ID
        /// </summary>
        Task<WorkflowExecutionResult> GetWorkflowExecutionAsync(string executionId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets workflow execution history
        /// </summary>
        Task<IEnumerable<WorkflowExecutionResult>> GetWorkflowExecutionHistoryAsync(string workflowId, int skip = 0, int take = 10, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Cancels a workflow execution
        /// </summary>
        Task<bool> CancelWorkflowExecutionAsync(string executionId, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Workflow execution result
    /// </summary>
    public class WorkflowExecutionResult
    {
        /// <summary>
        /// Workflow instance ID
        /// </summary>
        public string WorkflowInstanceId { get; set; }
        
        /// <summary>
        /// Workflow definition ID
        /// </summary>
        public string WorkflowDefinitionId { get; set; }
        
        /// <summary>
        /// Workflow definition version
        /// </summary>
        public int WorkflowDefinitionVersion { get; set; }
        
        /// <summary>
        /// Workflow status
        /// </summary>
        public WorkflowStatus Status { get; set; }
        
        /// <summary>
        /// Correlation ID
        /// </summary>
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Workflow input
        /// </summary>
        public Dictionary<string, object> Input { get; set; }
        
        /// <summary>
        /// Workflow output
        /// </summary>
        public Dictionary<string, object> Output { get; set; }
        
        /// <summary>
        /// Workflow execution error
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// Start time
        /// </summary>
        public System.DateTime StartTime { get; set; }
        
        /// <summary>
        /// End time
        /// </summary>
        public System.DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Last executed activity
        /// </summary>
        public string LastExecutedActivity { get; set; }
    }
}
