using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Interfaces
{
    public interface IWorkflowEngine
    {
        Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters = null);
        Task<WorkflowExecution> ExecuteWorkflowAsync(WorkflowDefinition workflow, Dictionary<string, object> parameters = null);
        Task<WorkflowExecution> GetExecutionStatusAsync(string executionId);
        Task<bool> CancelExecutionAsync(string executionId);
        Task<bool> PauseExecutionAsync(string executionId);
        Task<bool> ResumeExecutionAsync(string executionId);
        Task<List<WorkflowExecution>> GetExecutionHistoryAsync(string workflowId, int limit = 10);
    }
}
