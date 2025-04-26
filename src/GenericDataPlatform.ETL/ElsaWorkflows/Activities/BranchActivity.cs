using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.ElsaWorkflows.Activities
{
    [Activity(
        Category = "ETL",
        DisplayName = "Branch Data",
        Description = "Branches data flow based on conditions.",
        Outcomes = new[] { "Default", "Error" }
    )]
    public class BranchActivity : Activity
    {
        private readonly ILogger<BranchActivity> _logger;

        public BranchActivity(ILogger<BranchActivity> logger)
        {
            _logger = logger;
        }

        [ActivityInput(
            Label = "Input",
            Hint = "The input data to branch.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Input { get; set; }

        [ActivityInput(
            Label = "Branches",
            Hint = "The branch conditions and names.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Json },
            UIHint = ActivityInputUIHints.MultiLine
        )]
        public object Branches { get; set; }

        [ActivityInput(
            Label = "Default Branch Name",
            Hint = "The name of the default branch if no conditions match.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.SingleLine
        )]
        public string DefaultBranchName { get; set; } = "Default";

        [ActivityOutput]
        public object Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Executing Branch activity");

                // Convert branches to list
                var branchesList = ConvertToBranchesList(Branches);
                
                if (branchesList == null || !branchesList.Any())
                {
                    _logger.LogWarning("No branches defined, using default branch");
                    Output = Input;
                    return Outcome(DefaultBranchName, Input);
                }

                // Evaluate each branch condition
                foreach (var branch in branchesList)
                {
                    if (!branch.TryGetValue("name", out var nameObj) || !(nameObj is string name))
                    {
                        continue;
                    }

                    if (!branch.TryGetValue("condition", out var conditionObj))
                    {
                        continue;
                    }

                    bool conditionMet = false;

                    if (conditionObj is string conditionStr)
                    {
                        // Simple condition evaluation (in a real implementation, this would use a proper expression evaluator)
                        conditionMet = EvaluateCondition(conditionStr, Input);
                    }
                    else if (conditionObj is bool conditionBool)
                    {
                        conditionMet = conditionBool;
                    }

                    if (conditionMet)
                    {
                        _logger.LogInformation("Branch condition met for branch: {BranchName}", name);
                        Output = Input;
                        return Outcome(name, Input);
                    }
                }

                // If no conditions matched, use the default branch
                _logger.LogInformation("No branch conditions met, using default branch: {DefaultBranchName}", DefaultBranchName);
                Output = Input;
                return Outcome(DefaultBranchName, Input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Branch activity");
                return Outcome("Error", new { Error = ex.Message, Exception = ex });
            }
        }

        private List<Dictionary<string, object>> ConvertToBranchesList(object branches)
        {
            if (branches == null)
            {
                return new List<Dictionary<string, object>>();
            }
            
            if (branches is List<Dictionary<string, object>> list)
            {
                return list;
            }
            
            try
            {
                // Try to convert from JSON or other formats
                return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                    System.Text.Json.JsonSerializer.Serialize(branches));
            }
            catch
            {
                return new List<Dictionary<string, object>>();
            }
        }

        private bool EvaluateCondition(string condition, object input)
        {
            // In a real implementation, this would use a proper expression evaluator
            // For now, we'll just do some simple checks
            
            if (string.IsNullOrEmpty(condition))
            {
                return false;
            }
            
            // Check if the condition is a simple boolean
            if (bool.TryParse(condition, out var boolResult))
            {
                return boolResult;
            }
            
            // For demo purposes, we'll just return true for non-empty conditions
            // In a real implementation, this would evaluate the condition against the input
            return true;
        }
    }
}
