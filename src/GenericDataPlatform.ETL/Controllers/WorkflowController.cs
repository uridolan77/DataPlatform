using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows.Interfaces;
using GenericDataPlatform.ETL.Workflows.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/workflows")]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowEngine _workflowEngine;
        private readonly ILogger<WorkflowController> _logger;
        
        // In a real implementation, this would be stored in a database
        private static readonly Dictionary<string, WorkflowDefinition> _workflows = new Dictionary<string, WorkflowDefinition>();
        
        public WorkflowController(IWorkflowEngine workflowEngine, ILogger<WorkflowController> logger)
        {
            _workflowEngine = workflowEngine;
            _logger = logger;
        }
        
        [HttpGet]
        public ActionResult<IEnumerable<WorkflowDefinition>> GetWorkflows()
        {
            return Ok(_workflows.Values);
        }
        
        [HttpGet("{id}")]
        public ActionResult<WorkflowDefinition> GetWorkflow(string id)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
            {
                return NotFound();
            }
            
            return Ok(workflow);
        }
        
        [HttpPost]
        public ActionResult<WorkflowDefinition> CreateWorkflow(WorkflowDefinition workflow)
        {
            if (string.IsNullOrEmpty(workflow.Id))
            {
                workflow.Id = Guid.NewGuid().ToString();
            }
            
            if (_workflows.ContainsKey(workflow.Id))
            {
                return Conflict($"Workflow with ID {workflow.Id} already exists");
            }
            
            workflow.CreatedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            
            _workflows[workflow.Id] = workflow;
            
            return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow);
        }
        
        [HttpPut("{id}")]
        public ActionResult<WorkflowDefinition> UpdateWorkflow(string id, WorkflowDefinition workflow)
        {
            if (!_workflows.ContainsKey(id))
            {
                return NotFound();
            }
            
            workflow.Id = id;
            workflow.CreatedAt = _workflows[id].CreatedAt;
            workflow.UpdatedAt = DateTime.UtcNow;
            
            _workflows[id] = workflow;
            
            return Ok(workflow);
        }
        
        [HttpDelete("{id}")]
        public ActionResult DeleteWorkflow(string id)
        {
            if (!_workflows.ContainsKey(id))
            {
                return NotFound();
            }
            
            _workflows.Remove(id);
            
            return NoContent();
        }
        
        [HttpPost("{id}/execute")]
        public async Task<ActionResult<WorkflowExecution>> ExecuteWorkflow(string id, [FromBody] Dictionary<string, object> parameters = null)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
            {
                return NotFound();
            }
            
            try
            {
                var execution = await _workflowEngine.ExecuteWorkflowAsync(workflow, parameters);
                
                return Ok(execution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpGet("executions/{executionId}")]
        public async Task<ActionResult<WorkflowExecution>> GetExecutionStatus(string executionId)
        {
            try
            {
                var execution = await _workflowEngine.GetExecutionStatusAsync(executionId);
                
                return Ok(execution);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution status {ExecutionId}", executionId);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpPost("executions/{executionId}/cancel")]
        public async Task<ActionResult> CancelExecution(string executionId)
        {
            try
            {
                var result = await _workflowEngine.CancelExecutionAsync(executionId);
                
                if (result)
                {
                    return Ok(new { Message = "Execution cancelled successfully" });
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling execution {ExecutionId}", executionId);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<WorkflowExecution>>> GetExecutionHistory(string id, [FromQuery] int limit = 10)
        {
            if (!_workflows.ContainsKey(id))
            {
                return NotFound();
            }
            
            try
            {
                var history = await _workflowEngine.GetExecutionHistoryAsync(id, limit);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for workflow {WorkflowId}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
