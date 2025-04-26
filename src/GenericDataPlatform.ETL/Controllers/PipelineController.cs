using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/pipelines")]
    public class PipelineController : ControllerBase
    {
        private readonly IPipelineProcessor _pipelineProcessor;
        private readonly ILogger<PipelineController> _logger;
        private static readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new();
        
        public PipelineController(IPipelineProcessor pipelineProcessor, ILogger<PipelineController> logger)
        {
            _pipelineProcessor = pipelineProcessor;
            _logger = logger;
        }
        
        [HttpPost("execute")]
        public async Task<IActionResult> ExecutePipeline([FromBody] PipelineRequest request)
        {
            try
            {
                // Validate the request
                if (request.Source == null)
                {
                    return BadRequest("Source is required");
                }
                
                if (request.Stages == null || request.Stages.Count == 0)
                {
                    return BadRequest("At least one stage is required");
                }
                
                // Create a cancellation token source
                var cts = new CancellationTokenSource();
                var pipelineId = Guid.NewGuid().ToString();
                _cancellationTokens[pipelineId] = cts;
                
                // Create the pipeline context
                var context = new PipelineContext
                {
                    PipelineId = pipelineId,
                    Source = request.Source,
                    Stages = request.Stages,
                    Parameters = request.Parameters ?? new Dictionary<string, object>(),
                    CancellationToken = cts.Token
                };
                
                // Execute the pipeline asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _pipelineProcessor.ProcessAsync(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing pipeline {PipelineId}", pipelineId);
                    }
                    finally
                    {
                        // Clean up the cancellation token source
                        _cancellationTokens.Remove(pipelineId);
                        cts.Dispose();
                    }
                });
                
                // Return the pipeline ID
                return Ok(new { PipelineId = pipelineId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting pipeline execution");
                return StatusCode(500, "An error occurred while starting the pipeline");
            }
        }
        
        [HttpGet("{pipelineId}/status")]
        public async Task<IActionResult> GetPipelineStatus(string pipelineId)
        {
            try
            {
                var status = await _pipelineProcessor.GetStatusAsync(pipelineId);
                
                if (status == null)
                {
                    return NotFound($"Pipeline {pipelineId} not found");
                }
                
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pipeline status for {PipelineId}", pipelineId);
                return StatusCode(500, "An error occurred while getting the pipeline status");
            }
        }
        
        [HttpPost("{pipelineId}/cancel")]
        public async Task<IActionResult> CancelPipeline(string pipelineId)
        {
            try
            {
                // Cancel the pipeline using the cancellation token
                if (_cancellationTokens.TryGetValue(pipelineId, out var cts))
                {
                    cts.Cancel();
                }
                
                // Also cancel through the pipeline processor
                var result = await _pipelineProcessor.CancelAsync(pipelineId);
                
                if (!result)
                {
                    return NotFound($"Pipeline {pipelineId} not found or already completed");
                }
                
                return Ok(new { Message = $"Pipeline {pipelineId} cancellation requested" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling pipeline {PipelineId}", pipelineId);
                return StatusCode(500, "An error occurred while cancelling the pipeline");
            }
        }
    }
    
    public class PipelineRequest
    {
        public DataSourceDefinition Source { get; set; }
        public List<PipelineStage> Stages { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
