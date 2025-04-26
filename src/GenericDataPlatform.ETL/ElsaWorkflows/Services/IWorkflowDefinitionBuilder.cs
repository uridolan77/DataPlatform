using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Services
{
    /// <summary>
    /// Interface for workflow definition builder
    /// </summary>
    public interface IWorkflowDefinitionBuilder
    {
        /// <summary>
        /// Builds an Elsa workflow definition from a GenericDataPlatform workflow definition
        /// </summary>
        Task<IWorkflowBlueprint> BuildWorkflowBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Converts an Elsa workflow definition to a GenericDataPlatform workflow definition
        /// </summary>
        Task<WorkflowDefinition> ConvertToWorkflowDefinitionAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default);
    }
}
