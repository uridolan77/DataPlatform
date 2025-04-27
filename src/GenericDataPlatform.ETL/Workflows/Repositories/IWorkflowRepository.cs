using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Repositories
{
    /// <summary>
    /// Interface for workflow repository operations
    /// </summary>
    public interface IWorkflowRepository
    {
        // Workflow Definition Operations
        Task<WorkflowDefinition> GetWorkflowByIdAsync(string id, string version = null);
        Task<List<WorkflowDefinition>> GetWorkflowsAsync(int skip = 0, int take = 100);
        Task<List<string>> GetWorkflowVersionsAsync(string id);
        Task<string> SaveWorkflowAsync(WorkflowDefinition workflow);
        Task<bool> DeleteWorkflowAsync(string id, string version = null);

        // Workflow Execution Operations
        Task<WorkflowExecution> GetExecutionByIdAsync(string id);
        Task<List<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int limit = 10);
        Task<List<WorkflowExecution>> GetRecentExecutionsAsync(int limit = 10);
        Task<string> SaveExecutionAsync(WorkflowExecution execution);
        Task<bool> UpdateExecutionStatusAsync(string id, WorkflowExecutionStatus status);
        Task<bool> UpdateStepExecutionAsync(string executionId, WorkflowStepExecution stepExecution);

        // Workflow Metrics Operations
        Task<WorkflowMetrics> GetWorkflowMetricsAsync(string workflowId);

        // Workflow Execution Summary Operations
        Task<List<WorkflowExecutionSummary>> GetExecutionSummariesAsync(string workflowId, int limit = 10);
    }
}
