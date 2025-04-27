using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.ML.Models;
using GenericDataPlatform.ML.Services.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ML.Controllers
{
    /// <summary>
    /// Controller for ML operations
    /// </summary>
    [ApiController]
    [Route("api/ml")]
    public class MLController : ControllerBase
    {
        private readonly ILogger<MLController> _logger;
        private readonly IMLService _mlService;
        private readonly IModelManagementService _modelManagementService;
        private readonly ITrainingOrchestrationService _trainingOrchestrationService;
        private readonly IPredictionService _predictionService;

        public MLController(
            ILogger<MLController> logger,
            IMLService mlService,
            IModelManagementService modelManagementService,
            ITrainingOrchestrationService trainingOrchestrationService,
            IPredictionService predictionService)
        {
            _logger = logger;
            _mlService = mlService;
            _modelManagementService = modelManagementService;
            _trainingOrchestrationService = trainingOrchestrationService;
            _predictionService = predictionService;
        }

        /// <summary>
        /// Gets a model by ID
        /// </summary>
        [HttpGet("models/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ModelDefinition>> GetModelAsync(string id)
        {
            try
            {
                var model = await _modelManagementService.GetModelDefinitionAsync(id);

                if (model == null)
                {
                    return NotFound();
                }

                return Ok(model);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model: {ModelId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lists all models
        /// </summary>
        [HttpGet("models")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ModelDefinition>>> ListModelsAsync([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            try
            {
                var models = await _modelManagementService.ListModelDefinitionsAsync(limit, offset);
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new model
        /// </summary>
        [HttpPost("models")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ModelDefinition>> CreateModelAsync([FromBody] ModelDefinition model)
        {
            try
            {
                var createdModel = await _modelManagementService.CreateModelDefinitionAsync(model);
                return CreatedAtAction(nameof(GetModelAsync), new { id = createdModel.Id }, createdModel);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating model: {ModelName}", model.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates a model
        /// </summary>
        [HttpPut("models/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ModelDefinition>> UpdateModelAsync(string id, [FromBody] ModelDefinition model)
        {
            try
            {
                if (id != model.Id)
                {
                    return BadRequest(new { error = "Model ID in URL does not match model ID in body" });
                }

                var updatedModel = await _modelManagementService.UpdateModelDefinitionAsync(model);
                return Ok(updatedModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating model: {ModelId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a model
        /// </summary>
        [HttpDelete("models/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteModelAsync(string id)
        {
            try
            {
                await _modelManagementService.DeleteModelDefinitionAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model: {ModelId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a training job
        /// </summary>
        [HttpPost("training")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TrainingJob>> CreateTrainingJobAsync([FromBody] TrainingJobRequest request)
        {
            try
            {
                // Get the model definition
                var modelDefinition = await _modelManagementService.GetModelAsync(request.ModelId);

                if (modelDefinition == null)
                {
                    return NotFound(new { error = $"Model not found: {request.ModelId}" });
                }

                // Use the model definition from the metadata
                var modelDef = modelDefinition.Definition;

                // Create the training job
                var job = await _trainingOrchestrationService.CreateTrainingJobAsync(
                    modelDef,
                    request.DataSourceId,
                    request.DataQuery);

                // Start the job if requested
                if (request.StartImmediately)
                {
                    job = await _trainingOrchestrationService.StartTrainingJobAsync(job.Id);
                }

                return CreatedAtAction(nameof(GetTrainingJobAsync), new { id = job.Id }, job);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating training job for model: {ModelId}", request.ModelId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a training job by ID
        /// </summary>
        [HttpGet("training/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrainingJob>> GetTrainingJobAsync(string id)
        {
            try
            {
                var job = await _trainingOrchestrationService.GetTrainingJobAsync(id);

                if (job == null)
                {
                    return NotFound();
                }

                return Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training job: {JobId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lists all training jobs
        /// </summary>
        [HttpGet("training")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TrainingJob>>> ListTrainingJobsAsync([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            try
            {
                var jobs = await _trainingOrchestrationService.ListTrainingJobsAsync(limit, offset);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing training jobs");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Starts a training job
        /// </summary>
        [HttpPost("training/{id}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrainingJob>> StartTrainingJobAsync(string id)
        {
            try
            {
                var job = await _trainingOrchestrationService.StartTrainingJobAsync(id);
                return Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting training job: {JobId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Cancels a training job
        /// </summary>
        [HttpPost("training/{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrainingJob>> CancelTrainingJobAsync(string id)
        {
            try
            {
                var job = await _trainingOrchestrationService.CancelTrainingJobAsync(id);
                return Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling training job: {JobId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Makes a prediction
        /// </summary>
        [HttpPost("predict/{modelName}/{modelVersion?}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PredictionResponse>> PredictAsync(
            string modelName,
            string? modelVersion,
            [FromBody] PredictionRequest request)
        {
            try
            {
                var result = await _predictionService.PredictAsync(
                    modelName,
                    modelVersion,
                    request.Instances);

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Model not found: {modelName}" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making prediction with model: {ModelName} version {ModelVersion}",
                    modelName, modelVersion ?? "latest");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a prediction result by ID
        /// </summary>
        [HttpGet("predict/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PredictionResult>> GetPredictionResultAsync(string id)
        {
            try
            {
                var result = await _predictionService.GetPredictionResultAsync(id);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prediction result: {ResultId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request for creating a training job
    /// </summary>
    public class TrainingJobRequest
    {
        /// <summary>
        /// ID of the model to train
        /// </summary>
        public required string ModelId { get; set; }

        /// <summary>
        /// ID of the data source to use for training
        /// </summary>
        public required string DataSourceId { get; set; }

        /// <summary>
        /// Optional query to filter training data
        /// </summary>
        public string? DataQuery { get; set; }

        /// <summary>
        /// Whether to start the training job immediately
        /// </summary>
        public bool StartImmediately { get; set; } = true;
    }
}
