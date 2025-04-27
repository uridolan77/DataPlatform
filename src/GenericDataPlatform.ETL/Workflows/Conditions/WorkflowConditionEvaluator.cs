using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Workflows.Conditions
{
    /// <summary>
    /// Evaluates workflow conditions
    /// </summary>
    public class WorkflowConditionEvaluator
    {
        private readonly ILogger<WorkflowConditionEvaluator> _logger;

        public WorkflowConditionEvaluator(ILogger<WorkflowConditionEvaluator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Evaluates a list of conditions
        /// </summary>
        public async Task<bool> EvaluateConditionsAsync(List<WorkflowCondition> conditions, WorkflowContext context)
        {
            if (conditions == null || !conditions.Any())
            {
                return true;
            }

            foreach (var condition in conditions)
            {
                if (!await EvaluateConditionAsync(condition, context))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Evaluates a single condition
        /// </summary>
        public async Task<bool> EvaluateConditionAsync(WorkflowCondition condition, WorkflowContext context)
        {
            switch (condition.Type)
            {
                case WorkflowConditionType.Expression:
                    return EvaluateExpression(condition.Expression, context);

                case WorkflowConditionType.Script:
                case WorkflowConditionType.DataBased:
                case WorkflowConditionType.External:
                    _logger.LogWarning("Condition type {ConditionType} is not implemented", condition.Type);
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Evaluates an expression
        /// </summary>
        private bool EvaluateExpression(string expression, WorkflowContext context)
        {
            // In a real implementation, this would evaluate the expression
            // For this example, we'll implement a simple expression evaluator

            if (string.IsNullOrEmpty(expression))
            {
                return true;
            }

            // Check for variable references
            if (expression.Contains("$"))
            {
                // Replace variable references with their values
                foreach (var variable in context.Variables)
                {
                    expression = expression.Replace($"${variable.Key}", variable.Value?.ToString() ?? "null");
                }

                // Replace parameter references
                foreach (var parameter in context.Parameters)
                {
                    expression = expression.Replace($"$params.{parameter.Key}", parameter.Value?.ToString() ?? "null");
                }

                // Replace step output references
                foreach (var output in context.StepOutputs)
                {
                    expression = expression.Replace($"$steps.{output.Key}", "true");
                }
            }

            // Simple equality check
            if (expression.Contains("=="))
            {
                var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();

                    return left == right;
                }
            }

            // Simple inequality check
            if (expression.Contains("!="))
            {
                var parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = parts[1].Trim();

                    return left != right;
                }
            }

            // Simple boolean check
            if (bool.TryParse(expression, out var boolResult))
            {
                return boolResult;
            }

            // Default to true for unrecognized expressions
            _logger.LogWarning("Could not evaluate expression: {Expression}", expression);
            return true;
        }
    }
}
