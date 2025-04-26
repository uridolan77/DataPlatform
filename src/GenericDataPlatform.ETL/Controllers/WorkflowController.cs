using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.ElsaWorkflows.Services;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/workflows")]
    public class WorkflowController : ControllerBase
    {
        private readonly IEtlWorkflowService _workflowService;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(IEtlWorkflowService workflowService, ILogger<WorkflowController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkflows([FromQuery] int skip = 0, [FromQuery] int take = 100)
        {
            try
            {
                var workflows = await _workflowService.GetWorkflowDefinitionsAsync(skip, take);
                return Ok(workflows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflows");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkflow(string id, [FromQuery] string version = null)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowDefinitionAsync(id, version);
                if (workflow == null)
                {
                    return NotFound();
                }
                return Ok(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkflow([FromBody] WorkflowDefinition workflow)
        {
            try
            {
                if (string.IsNullOrEmpty(workflow.Id))
                {
                    workflow.Id = Guid.NewGuid().ToString();
                }

                workflow.CreatedAt = DateTime.UtcNow;
                workflow.UpdatedAt = DateTime.UtcNow;

                var workflowId = await _workflowService.CreateWorkflowDefinitionAsync(workflow);
                return CreatedAtAction(nameof(GetWorkflow), new { id = workflowId }, workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkflow(string id, [FromBody] WorkflowDefinition workflow)
        {
            try
            {
                if (id != workflow.Id)
                {
                    workflow.Id = id;
                }

                workflow.UpdatedAt = DateTime.UtcNow;

                var workflowId = await _workflowService.UpdateWorkflowDefinitionAsync(workflow);
                return Ok(new { Id = workflowId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkflow(string id)
        {
            try
            {
                var result = await _workflowService.DeleteWorkflowDefinitionAsync(id);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/execute")]
        public async Task<IActionResult> ExecuteWorkflow(string id, [FromBody] Dictionary<string, object> input = null)
        {
            try
            {
                var result = await _workflowService.ExecuteWorkflowAsync(id, input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("executions/{executionId}")]
        public async Task<IActionResult> GetWorkflowExecution(string executionId)
        {
            try
            {
                var execution = await _workflowService.GetWorkflowExecutionAsync(executionId);
                if (execution == null)
                {
                    return NotFound();
                }
                return Ok(execution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow execution {ExecutionId}", executionId);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetWorkflowExecutionHistory(string id, [FromQuery] int skip = 0, [FromQuery] int take = 10)
        {
            try
            {
                var executions = await _workflowService.GetWorkflowExecutionHistoryAsync(id, skip, take);
                return Ok(executions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow execution history {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("executions/{executionId}/cancel")]
        public async Task<IActionResult> CancelWorkflowExecution(string executionId)
        {
            try
            {
                var result = await _workflowService.CancelWorkflowExecutionAsync(executionId);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling workflow execution {ExecutionId}", executionId);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
