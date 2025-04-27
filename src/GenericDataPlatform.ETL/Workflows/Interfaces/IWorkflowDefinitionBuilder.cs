using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Interfaces
{
    /// <summary>
    /// Interface for building workflow definitions
    /// </summary>
    public interface IWorkflowDefinitionBuilder
    {
        /// <summary>
        /// Builds a workflow definition from an ETL workflow definition
        /// </summary>
        Task<WorkflowDefinition> BuildWorkflowDefinitionAsync(EtlWorkflowDefinition etlWorkflow);
    }
}
