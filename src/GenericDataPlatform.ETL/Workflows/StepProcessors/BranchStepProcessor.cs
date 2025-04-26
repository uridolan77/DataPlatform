using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.StepProcessors
{
    public class BranchStepProcessor : IWorkflowStepProcessor
    {
        private readonly ILogger<BranchStepProcessor> _logger;
        
        public string StepType => WorkflowStepType.Branch.ToString();
        
        public BranchStepProcessor(ILogger<BranchStepProcessor> logger)
        {
            _logger = logger;
        }
        
        public async Task<object> ProcessStepAsync(WorkflowStep step, WorkflowContext context)
        {
            try
            {
                _logger.LogInformation("Processing branch step {StepId}", step.Id);
                
                // Get branches
                if (!step.Configuration.TryGetValue("branches", out var branchesObj) || 
                    !(branchesObj is List<object> branchesList))
                {
                    throw new ArgumentException($"Branches not specified for step {step.Id}");
                }
                
                // Get input data from dependent steps
                object inputData = null;
                if (step.DependsOn.Any())
                {
                    var inputStepId = step.DependsOn.First();
                    if (!context.StepOutputs.TryGetValue(inputStepId, out inputData))
                    {
                        _logger.LogWarning("Input data not found for step {StepId} from dependent step {InputStepId}", 
                            step.Id, inputStepId);
                    }
                }
                
                // Evaluate branches
                var result = new Dictionary<string, object>();
                var activeBranches = new List<string>();
                
                foreach (var branchObj in branchesList)
                {
                    if (branchObj is Dictionary<string, object> branch)
                    {
                        if (!branch.TryGetValue("id", out var branchIdObj))
                        {
                            _logger.LogWarning("Branch ID not specified in branch step {StepId}", step.Id);
                            continue;
                        }
                        
                        var branchId = branchIdObj.ToString();
                        
                        if (!branch.TryGetValue("condition", out var conditionObj))
                        {
                            _logger.LogWarning("Condition not specified for branch {BranchId} in step {StepId}", 
                                branchId, step.Id);
                            continue;
                        }
                        
                        var condition = conditionObj.ToString();
                        
                        // Evaluate condition
                        bool conditionResult = EvaluateCondition(condition, context, inputData);
                        
                        if (conditionResult)
                        {
                            activeBranches.Add(branchId);
                            
                            // Add branch output
                            if (branch.TryGetValue("output", out var outputObj))
                            {
                                result[branchId] = outputObj;
                            }
                            else
                            {
                                result[branchId] = inputData;
                            }
                            
                            // Check if this is the default branch
                            if (branch.TryGetValue("isDefault", out var isDefaultObj) && 
                                isDefaultObj is bool isDefault && isDefault)
                            {
                                _logger.LogInformation("Default branch {BranchId} is active", branchId);
                                break;
                            }
                        }
                    }
                }
                
                // Add active branches to result
                result["activeBranches"] = activeBranches;
                
                // Add to context variables
                context.Variables[$"branch.{step.Id}.activeBranches"] = activeBranches;
                
                _logger.LogInformation("Branch step {StepId} completed with {ActiveBranchCount} active branches", 
                    step.Id, activeBranches.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing branch step {StepId}", step.Id);
                throw;
            }
        }
        
        public async Task<bool> ValidateStepConfigurationAsync(WorkflowStep step)
        {
            if (!step.Configuration.TryGetValue("branches", out var branchesObj) || 
                !(branchesObj is List<object> branchesList))
            {
                return false;
            }
            
            foreach (var branchObj in branchesList)
            {
                if (branchObj is Dictionary<string, object> branch)
                {
                    if (!branch.TryGetValue("id", out _))
                    {
                        return false;
                    }
                    
                    if (!branch.TryGetValue("condition", out _))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private bool EvaluateCondition(string condition, WorkflowContext context, object inputData)
        {
            // In a real implementation, this would evaluate the condition
            // For this example, we'll implement a simple condition evaluator
            
            if (string.IsNullOrEmpty(condition))
            {
                return true;
            }
            
            // Check for variable references
            if (condition.Contains("$"))
            {
                // Replace variable references with their values
                foreach (var variable in context.Variables)
                {
                    condition = condition.Replace($"${variable.Key}", variable.Value?.ToString() ?? "null");
                }
                
                // Replace parameter references
                foreach (var parameter in context.Parameters)
                {
                    condition = condition.Replace($"$params.{parameter.Key}", parameter.Value?.ToString() ?? "null");
                }
                
                // Replace step output references
                foreach (var output in context.StepOutputs)
                {
                    condition = condition.Replace($"$steps.{output.Key}", "true");
                }
                
                // Replace input references
                if (inputData != null)
                {
                    condition = condition.Replace("$input", "true");
                    
                    if (inputData is Dictionary<string, object> inputDict)
                    {
                        foreach (var entry in inputDict)
                        {
                            condition = condition.Replace($"$input.{entry.Key}", entry.Value?.ToString() ?? "null");
                        }
                    }
                }
            }
            
            // Simple equality check
            if (condition.Contains("=="))
            {
                var parts = condition.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();
                    
                    return left == right;
                }
            }
            
            // Simple inequality check
            if (condition.Contains("!="))
            {
                var parts = condition.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();
                    
                    return left != right;
                }
            }
            
            // Simple boolean check
            if (bool.TryParse(condition, out var boolResult))
            {
                return boolResult;
            }
            
            // Default to true for unrecognized conditions
            _logger.LogWarning("Could not evaluate condition: {Condition}", condition);
            return true;
        }
    }
}
