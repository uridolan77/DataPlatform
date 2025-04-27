using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Interfaces
{
    /// <summary>
    /// Interface for ETL workflow service
    /// </summary>
    public interface IEtlWorkflowService
    {
        /// <summary>
        /// Creates a new ETL workflow
        /// </summary>
        Task<string> CreateWorkflowAsync(EtlWorkflowDefinition etlWorkflow);
        
        /// <summary>
        /// Updates an existing ETL workflow
        /// </summary>
        Task<bool> UpdateWorkflowAsync(string workflowId, EtlWorkflowDefinition etlWorkflow);
        
        /// <summary>
        /// Gets an ETL workflow by ID
        /// </summary>
        Task<EtlWorkflowDefinition> GetWorkflowAsync(string workflowId);
        
        /// <summary>
        /// Executes an ETL workflow
        /// </summary>
        Task<WorkflowExecution> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// Gets a list of ETL workflows
        /// </summary>
        Task<List<EtlWorkflowSummary>> GetWorkflowsAsync(int skip = 0, int take = 100);
        
        /// <summary>
        /// Deletes an ETL workflow
        /// </summary>
        Task<bool> DeleteWorkflowAsync(string workflowId);
    }
}
