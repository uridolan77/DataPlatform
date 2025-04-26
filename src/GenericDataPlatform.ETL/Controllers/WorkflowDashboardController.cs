using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ETL.Workflows;
using GenericDataPlatform.ETL.Workflows.Models;
using GenericDataPlatform.ETL.Workflows.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowDashboardController : ControllerBase
    {
        private readonly IWorkflowRepository _repository;
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IWorkflowMonitor _monitor;
        private readonly ILogger<WorkflowDashboardController> _logger;
        
        public WorkflowDashboardController(
            IWorkflowRepository repository,
            IWorkflowEngine workflowEngine,
            IWorkflowMonitor monitor,
            ILogger<WorkflowDashboardController> logger)
        {
            _repository = repository;
            _workflowEngine = workflowEngine;
            _monitor = monitor;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets a list of workflow definitions
        /// </summary>
        [HttpGet("workflows")]
        public async Task<ActionResult<List<WorkflowDefinition>>> GetWorkflows(int skip = 0, int take = 100)
        {
            try
            {
                var workflows = await _repository.GetWorkflowsAsync(skip, take);
                return Ok(workflows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflows");
                return StatusCode(500, "Error getting workflows");
            }
        }
        
        /// <summary>
        /// Gets a workflow definition by ID
        /// </summary>
        [HttpGet("workflows/{id}")]
        public async Task<ActionResult<WorkflowDefinition>> GetWorkflow(string id, string version = null)
        {
            try
            {
                var workflow = await _repository.GetWorkflowByIdAsync(id, version);
                
                if (workflow == null)
                {
                    return NotFound();
                }
                
                return Ok(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow {WorkflowId}", id);
                return StatusCode(500, "Error getting workflow");
            }
        }
        
        /// <summary>
        /// Gets workflow versions
        /// </summary>
        [HttpGet("workflows/{id}/versions")]
        public async Task<ActionResult<List<string>>> GetWorkflowVersions(string id)
        {
            try
            {
                var versions = await _repository.GetWorkflowVersionsAsync(id);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow versions for {WorkflowId}", id);
                return StatusCode(500, "Error getting workflow versions");
            }
        }
        
        /// <summary>
        /// Gets recent workflow executions
        /// </summary>
        [HttpGet("executions")]
        public async Task<ActionResult<List<WorkflowExecution>>> GetRecentExecutions(int limit = 10)
        {
            try
            {
                var executions = await _repository.GetRecentExecutionsAsync(limit);
                return Ok(executions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent executions");
                return StatusCode(500, "Error getting recent executions");
            }
        }
        
        /// <summary>
        /// Gets a workflow execution by ID
        /// </summary>
        [HttpGet("executions/{id}")]
        public async Task<ActionResult<WorkflowExecution>> GetExecution(string id)
        {
            try
            {
                var execution = await _repository.GetExecutionByIdAsync(id);
                
                if (execution == null)
                {
                    return NotFound();
                }
                
                return Ok(execution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution {ExecutionId}", id);
                return StatusCode(500, "Error getting execution");
            }
        }
        
        /// <summary>
        /// Gets workflow execution history
        /// </summary>
        [HttpGet("workflows/{id}/executions")]
        public async Task<ActionResult<List<WorkflowExecution>>> GetExecutionHistory(string id, int limit = 10)
        {
            try
            {
                var executions = await _repository.GetExecutionHistoryAsync(id, limit);
                return Ok(executions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for workflow {WorkflowId}", id);
                return StatusCode(500, "Error getting execution history");
            }
        }
        
        /// <summary>
        /// Gets workflow metrics
        /// </summary>
        [HttpGet("workflows/{id}/metrics")]
        public async Task<ActionResult<WorkflowMetrics>> GetWorkflowMetrics(string id)
        {
            try
            {
                var metrics = await _monitor.GetWorkflowMetricsAsync(id);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics for workflow {WorkflowId}", id);
                return StatusCode(500, "Error getting workflow metrics");
            }
        }
        
        /// <summary>
        /// Gets execution summaries for a workflow
        /// </summary>
        [HttpGet("workflows/{id}/execution-summaries")]
        public async Task<ActionResult<List<WorkflowExecutionSummary>>> GetExecutionSummaries(string id, int limit = 10)
        {
            try
            {
                var summaries = await _repository.GetExecutionSummariesAsync(id, limit);
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution summaries for workflow {WorkflowId}", id);
                return StatusCode(500, "Error getting execution summaries");
            }
        }
        
        /// <summary>
        /// Gets timeline events for a workflow execution
        /// </summary>
        [HttpGet("executions/{id}/timeline")]
        public async Task<ActionResult<List<WorkflowTimelineEvent>>> GetTimelineEvents(string id, int limit = 100)
        {
            try
            {
                var events = await _monitor.GetTimelineEventsAsync(id, limit);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timeline events for execution {ExecutionId}", id);
                return StatusCode(500, "Error getting timeline events");
            }
        }
        
        /// <summary>
        /// Cancels a workflow execution
        /// </summary>
        [HttpPost("executions/{id}/cancel")]
        public async Task<ActionResult> CancelExecution(string id)
        {
            try
            {
                var result = await _workflowEngine.CancelExecutionAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling execution {ExecutionId}", id);
                return StatusCode(500, "Error cancelling execution");
            }
        }
        
        /// <summary>
        /// Pauses a workflow execution
        /// </summary>
        [HttpPost("executions/{id}/pause")]
        public async Task<ActionResult> PauseExecution(string id)
        {
            try
            {
                var result = await _workflowEngine.PauseExecutionAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing execution {ExecutionId}", id);
                return StatusCode(500, "Error pausing execution");
            }
        }
        
        /// <summary>
        /// Resumes a workflow execution
        /// </summary>
        [HttpPost("executions/{id}/resume")]
        public async Task<ActionResult> ResumeExecution(string id)
        {
            try
            {
                var result = await _workflowEngine.ResumeExecutionAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming execution {ExecutionId}", id);
                return StatusCode(500, "Error resuming execution");
            }
        }
    }
}
