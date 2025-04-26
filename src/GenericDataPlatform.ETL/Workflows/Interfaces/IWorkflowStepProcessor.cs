using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;

namespace GenericDataPlatform.ETL.Workflows.Interfaces
{
    public interface IWorkflowStepProcessor
    {
        string StepType { get; }
        Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context);
        Task<bool> ValidateStepConfigurationAsync(WorkflowStep step);
    }
}
