using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Models;
using Elsa.Services;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.ElsaWorkflows.Activities;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Services
{
    /// <summary>
    /// Implementation of workflow definition builder
    /// </summary>
    public class WorkflowDefinitionBuilder : IWorkflowDefinitionBuilder
    {
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;
        private readonly IWorkflowBuilder _workflowBuilder;
        private readonly ILogger<WorkflowDefinitionBuilder> _logger;

        public WorkflowDefinitionBuilder(
            IWorkflowBlueprintMaterializer workflowBlueprintMaterializer,
            IWorkflowBuilder workflowBuilder,
            ILogger<WorkflowDefinitionBuilder> logger)
        {
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
            _workflowBuilder = workflowBuilder;
            _logger = logger;
        }

        /// <summary>
        /// Builds an Elsa workflow definition from a GenericDataPlatform workflow definition
        /// </summary>
        public async Task<IWorkflowBlueprint> BuildWorkflowBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Building workflow blueprint for workflow: {WorkflowId}", workflowDefinition.Id);

                // Create a new workflow definition
                var workflowDefinitionId = workflowDefinition.Id;
                var workflowName = workflowDefinition.Name;
                var workflowDisplayName = workflowDefinition.Name;
                var workflowDescription = workflowDefinition.Description;
                var workflowVersion = int.TryParse(workflowDefinition.Version, out var version) ? version : 1;
                var workflowPersistence = WorkflowPersistenceBehavior.WorkflowBurst;
                var workflowDeleteCompletedInstances = false;

                // Build the workflow
                var builder = _workflowBuilder
                    .WithId(workflowDefinitionId)
                    .WithName(workflowName)
                    .WithDisplayName(workflowDisplayName)
                    .WithDescription(workflowDescription)
                    .WithVersion(workflowVersion)
                    .WithPersistenceBehavior(workflowPersistence)
                    .WithDeleteCompletedInstances(workflowDeleteCompletedInstances);

                // Add variables
                foreach (var parameter in workflowDefinition.Parameters)
                {
                    builder.WithVariable(parameter.Key, parameter.Value);
                }

                // Add activities
                foreach (var step in workflowDefinition.Steps)
                {
                    await AddActivityAsync(builder, step, workflowDefinition);
                }

                // Build the workflow blueprint
                var workflowBlueprint = await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(builder, cancellationToken);

                _logger.LogInformation("Workflow blueprint built successfully for workflow: {WorkflowId}", workflowDefinition.Id);

                return workflowBlueprint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building workflow blueprint for workflow: {WorkflowId}", workflowDefinition.Id);
                throw;
            }
        }

        /// <summary>
        /// Converts an Elsa workflow definition to a GenericDataPlatform workflow definition
        /// </summary>
        public async Task<WorkflowDefinition> ConvertToWorkflowDefinitionAsync(IWorkflowBlueprint workflowBlueprint, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Converting workflow blueprint to workflow definition: {WorkflowId}", workflowBlueprint.Id);

                // Create a new workflow definition
                var workflowDefinition = new WorkflowDefinition
                {
                    Id = workflowBlueprint.Id,
                    Name = workflowBlueprint.Name,
                    Description = workflowBlueprint.Description,
                    Version = workflowBlueprint.Version.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add parameters
                foreach (var variable in workflowBlueprint.Variables)
                {
                    workflowDefinition.Parameters[variable.Key] = variable.Value;
                }

                // Add steps
                foreach (var activity in workflowBlueprint.Activities)
                {
                    var step = ConvertActivityToStep(activity, workflowBlueprint);
                    if (step != null)
                    {
                        workflowDefinition.Steps.Add(step);
                    }
                }

                _logger.LogInformation("Workflow blueprint converted successfully to workflow definition: {WorkflowId}", workflowBlueprint.Id);

                return workflowDefinition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting workflow blueprint to workflow definition: {WorkflowId}", workflowBlueprint.Id);
                throw;
            }
        }

        /// <summary>
        /// Adds an activity to the workflow builder
        /// </summary>
        private async Task AddActivityAsync(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            switch (step.Type)
            {
                case WorkflowStepType.Extract:
                    AddExtractActivity(builder, step, workflowDefinition);
                    break;

                case WorkflowStepType.Transform:
                    AddTransformActivity(builder, step, workflowDefinition);
                    break;

                case WorkflowStepType.Load:
                    AddLoadActivity(builder, step, workflowDefinition);
                    break;

                case WorkflowStepType.Validate:
                    AddValidateActivity(builder, step, workflowDefinition);
                    break;

                case WorkflowStepType.Enrich:
                    AddEnrichActivity(builder, step, workflowDefinition);
                    break;

                case WorkflowStepType.Branch:
                    AddBranchActivity(builder, step, workflowDefinition);
                    break;

                default:
                    _logger.LogWarning("Unsupported step type: {StepType}", step.Type);
                    break;
            }
        }

        /// <summary>
        /// Adds an extract activity to the workflow builder
        /// </summary>
        private void AddExtractActivity(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            builder.Then<ExtractActivity>(activity =>
            {
                activity.WithId(step.Id)
                    .WithName(step.Name)
                    .WithDisplayName(step.Name)
                    .WithDescription(step.Description);

                if (step.Configuration.TryGetValue("extractorType", out var extractorType))
                {
                    activity.WithExtractorType(extractorType.ToString());
                }

                activity.WithConfiguration(step.Configuration);

                if (step.Configuration.TryGetValue("source", out var source) && source is DataSourceDefinition dataSource)
                {
                    activity.WithSource(dataSource);
                }

                if (step.Configuration.TryGetValue("parameters", out var parameters))
                {
                    activity.WithParameters(parameters);
                }

                // Set up connections to dependent activities
                foreach (var dependsOn in step.DependsOn)
                {
                    activity.WithDependsOnActivity(dependsOn);
                }
            });
        }

        /// <summary>
        /// Adds a transform activity to the workflow builder
        /// </summary>
        private void AddTransformActivity(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            builder.Then<TransformActivity>(activity =>
            {
                activity.WithId(step.Id)
                    .WithName(step.Name)
                    .WithDisplayName(step.Name)
                    .WithDescription(step.Description);

                if (step.Configuration.TryGetValue("transformerType", out var transformerType))
                {
                    activity.WithTransformerType(transformerType.ToString());
                }

                activity.WithConfiguration(step.Configuration);

                if (step.Configuration.TryGetValue("source", out var source) && source is DataSourceDefinition dataSource)
                {
                    activity.WithSource(dataSource);
                }

                // Set up input from dependent activities
                if (step.DependsOn.Any())
                {
                    var inputStepId = step.DependsOn.First();
                    activity.WithInput(context => context.GetActivityOutput(inputStepId));
                }

                // Set up connections to dependent activities
                foreach (var dependsOn in step.DependsOn)
                {
                    activity.WithDependsOnActivity(dependsOn);
                }
            });
        }

        /// <summary>
        /// Adds a load activity to the workflow builder
        /// </summary>
        private void AddLoadActivity(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            builder.Then<LoadActivity>(activity =>
            {
                activity.WithId(step.Id)
                    .WithName(step.Name)
                    .WithDisplayName(step.Name)
                    .WithDescription(step.Description);

                if (step.Configuration.TryGetValue("loaderType", out var loaderType))
                {
                    activity.WithLoaderType(loaderType.ToString());
                }

                activity.WithConfiguration(step.Configuration);

                if (step.Configuration.TryGetValue("source", out var source) && source is DataSourceDefinition dataSource)
                {
                    activity.WithSource(dataSource);
                }

                // Set up input from dependent activities
                if (step.DependsOn.Any())
                {
                    var inputStepId = step.DependsOn.First();
                    activity.WithInput(context => context.GetActivityOutput(inputStepId));
                }

                // Set up connections to dependent activities
                foreach (var dependsOn in step.DependsOn)
                {
                    activity.WithDependsOnActivity(dependsOn);
                }
            });
        }

        /// <summary>
        /// Adds a validate activity to the workflow builder
        /// </summary>
        private void AddValidateActivity(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            builder.Then<ValidateActivity>(activity =>
            {
                activity.WithId(step.Id)
                    .WithName(step.Name)
                    .WithDisplayName(step.Name)
                    .WithDescription(step.Description);

                if (step.Configuration.TryGetValue("validatorType", out var validatorType))
                {
                    activity.WithValidatorType(validatorType.ToString());
                }

                activity.WithConfiguration(step.Configuration);

                if (step.Configuration.TryGetValue("source", out var source) && source is DataSourceDefinition dataSource)
                {
                    activity.WithSource(dataSource);
                }

                if (step.Configuration.TryGetValue("failOnInvalid", out var failOnInvalid) && failOnInvalid is bool failOnInvalidBool)
                {
                    activity.WithFailOnInvalid(failOnInvalidBool);
                }

                // Set up input from dependent activities
                if (step.DependsOn.Any())
                {
                    var inputStepId = step.DependsOn.First();
                    activity.WithInput(context => context.GetActivityOutput(inputStepId));
                }

                // Set up connections to dependent activities
                foreach (var dependsOn in step.DependsOn)
                {
                    activity.WithDependsOnActivity(dependsOn);
                }
            });

            // Add a branch activity to handle validation outcomes
            builder.Then<If>(activity =>
            {
                activity.WithId($"{step.Id}_Branch")
                    .WithName($"{step.Name} Branch")
                    .WithDisplayName($"{step.Name} Branch")
                    .WithDescription($"Branch based on validation result for {step.Name}")
                    .WithCondition(context => 
                    {
                        var validationResult = context.GetActivityOutput<ValidationResult>(step.Id);
                        return validationResult?.IsValid ?? false;
                    });
            });
        }

        /// <summary>
        /// Adds an enrich activity to the workflow builder
        /// </summary>
        private void AddEnrichActivity(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            builder.Then<EnrichActivity>(activity =>
            {
                activity.WithId(step.Id)
                    .WithName(step.Name)
                    .WithDisplayName(step.Name)
                    .WithDescription(step.Description);

                if (step.Configuration.TryGetValue("enricherType", out var enricherType))
                {
                    activity.WithEnricherType(enricherType.ToString());
                }

                activity.WithConfiguration(step.Configuration);

                if (step.Configuration.TryGetValue("source", out var source) && source is DataSourceDefinition dataSource)
                {
                    activity.WithSource(dataSource);
                }

                // Set up input from dependent activities
                if (step.DependsOn.Any())
                {
                    var inputStepId = step.DependsOn.First();
                    activity.WithInput(context => context.GetActivityOutput(inputStepId));
                }

                // Set up connections to dependent activities
                foreach (var dependsOn in step.DependsOn)
                {
                    activity.WithDependsOnActivity(dependsOn);
                }
            });
        }

        /// <summary>
        /// Adds a branch activity to the workflow builder
        /// </summary>
        private void AddBranchActivity(IWorkflowBuilder builder, WorkflowStep step, WorkflowDefinition workflowDefinition)
        {
            builder.Then<BranchActivity>(activity =>
            {
                activity.WithId(step.Id)
                    .WithName(step.Name)
                    .WithDisplayName(step.Name)
                    .WithDescription(step.Description);

                if (step.Configuration.TryGetValue("branches", out var branches))
                {
                    activity.WithBranches(branches);
                }

                if (step.Configuration.TryGetValue("defaultBranchName", out var defaultBranchName) && defaultBranchName is string defaultBranchNameStr)
                {
                    activity.WithDefaultBranchName(defaultBranchNameStr);
                }

                // Set up input from dependent activities
                if (step.DependsOn.Any())
                {
                    var inputStepId = step.DependsOn.First();
                    activity.WithInput(context => context.GetActivityOutput(inputStepId));
                }

                // Set up connections to dependent activities
                foreach (var dependsOn in step.DependsOn)
                {
                    activity.WithDependsOnActivity(dependsOn);
                }
            });
        }

        /// <summary>
        /// Converts an Elsa activity to a GenericDataPlatform workflow step
        /// </summary>
        private WorkflowStep ConvertActivityToStep(IActivity activity, IWorkflowBlueprint workflowBlueprint)
        {
            try
            {
                var activityType = activity.Type;
                var stepType = GetStepTypeFromActivityType(activityType);

                if (stepType == null)
                {
                    return null;
                }

                var step = new WorkflowStep
                {
                    Id = activity.Id,
                    Name = activity.Name,
                    Description = activity.Description,
                    Type = stepType.Value
                };

                // Set configuration based on activity type
                switch (activityType)
                {
                    case nameof(ExtractActivity):
                        if (activity is ExtractActivity extractActivity)
                        {
                            step.Configuration["extractorType"] = extractActivity.ExtractorType;
                            step.Configuration["configuration"] = extractActivity.Configuration;
                            step.Configuration["source"] = extractActivity.Source;
                            step.Configuration["parameters"] = extractActivity.Parameters;
                        }
                        break;

                    case nameof(TransformActivity):
                        if (activity is TransformActivity transformActivity)
                        {
                            step.Configuration["transformerType"] = transformActivity.TransformerType;
                            step.Configuration["configuration"] = transformActivity.Configuration;
                            step.Configuration["source"] = transformActivity.Source;
                        }
                        break;

                    case nameof(LoadActivity):
                        if (activity is LoadActivity loadActivity)
                        {
                            step.Configuration["loaderType"] = loadActivity.LoaderType;
                            step.Configuration["configuration"] = loadActivity.Configuration;
                            step.Configuration["source"] = loadActivity.Source;
                        }
                        break;

                    case nameof(ValidateActivity):
                        if (activity is ValidateActivity validateActivity)
                        {
                            step.Configuration["validatorType"] = validateActivity.ValidatorType;
                            step.Configuration["configuration"] = validateActivity.Configuration;
                            step.Configuration["source"] = validateActivity.Source;
                            step.Configuration["failOnInvalid"] = validateActivity.FailOnInvalid;
                        }
                        break;

                    case nameof(EnrichActivity):
                        if (activity is EnrichActivity enrichActivity)
                        {
                            step.Configuration["enricherType"] = enrichActivity.EnricherType;
                            step.Configuration["configuration"] = enrichActivity.Configuration;
                            step.Configuration["source"] = enrichActivity.Source;
                        }
                        break;

                    case nameof(BranchActivity):
                        if (activity is BranchActivity branchActivity)
                        {
                            step.Configuration["branches"] = branchActivity.Branches;
                            step.Configuration["defaultBranchName"] = branchActivity.DefaultBranchName;
                        }
                        break;
                }

                // Set dependencies
                var connections = workflowBlueprint.Connections
                    .Where(c => c.Target.Activity.Id == activity.Id)
                    .ToList();

                foreach (var connection in connections)
                {
                    step.DependsOn.Add(connection.Source.Activity.Id);
                }

                return step;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting activity to step: {ActivityId}", activity.Id);
                return null;
            }
        }

        /// <summary>
        /// Gets the step type from the activity type
        /// </summary>
        private WorkflowStepType? GetStepTypeFromActivityType(string activityType)
        {
            switch (activityType)
            {
                case nameof(ExtractActivity):
                    return WorkflowStepType.Extract;

                case nameof(TransformActivity):
                    return WorkflowStepType.Transform;

                case nameof(LoadActivity):
                    return WorkflowStepType.Load;

                case nameof(ValidateActivity):
                    return WorkflowStepType.Validate;

                case nameof(EnrichActivity):
                    return WorkflowStepType.Enrich;

                case nameof(BranchActivity):
                    return WorkflowStepType.Branch;

                default:
                    return null;
            }
        }
    }
}
