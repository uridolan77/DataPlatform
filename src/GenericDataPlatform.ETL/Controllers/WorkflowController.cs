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
                var workflows = await _workflowService.GetWorkflowsAsync(skip, take);
                return Ok(workflows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflows");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkflow(string id)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowAsync(id);
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
        public async Task<IActionResult> CreateWorkflow([FromBody] EtlWorkflowDefinition workflow)
        {
            try
            {
                var workflowId = await _workflowService.CreateWorkflowAsync(workflow);
                return CreatedAtAction(nameof(GetWorkflow), new { id = workflowId }, workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkflow(string id, [FromBody] EtlWorkflowDefinition workflow)
        {
            try
            {
                var result = await _workflowService.UpdateWorkflowAsync(id, workflow);
                return Ok(new { Success = result });
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
                var result = await _workflowService.DeleteWorkflowAsync(id);
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


    }
}
