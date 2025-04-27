using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Clients;
using GenericDataPlatform.Common.Errors;
using GenericDataPlatform.Protos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.StorageService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly ResilientStorageServiceClient _storageClient;
        private readonly ILogger<StorageController> _logger;

        public StorageController(
            ResilientStorageServiceClient storageClient,
            ILogger<StorageController> logger)
        {
            _storageClient = storageClient;
            _logger = logger;
        }

        [HttpGet("files")]
        public async Task<IActionResult> ListFiles(string bucket, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _storageClient.ListFilesAsync(bucket, prefix, recursive, cancellationToken);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Error listing files in bucket {Bucket} with prefix {Prefix}", bucket, prefix);
                return StatusCode((int)ex.StatusCode, ex.ToApiErrorResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing files in bucket {Bucket} with prefix {Prefix}", bucket, prefix);
                return StatusCode(500, ApiErrorResponse.FromException(ex));
            }
        }

        [HttpGet("files/{*path}")]
        public async Task<IActionResult> GetFile(string bucket, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get metadata first to determine content type
                var metadata = await _storageClient.GetMetadataAsync(path, cancellationToken);

                // Create memory stream to hold the file content
                var memoryStream = new MemoryStream();

                // Download the file
                var downloadStream = await _storageClient.DownloadFileAsync(path, cancellationToken);
                await downloadStream.CopyToAsync(memoryStream, cancellationToken);

                // Reset the stream position to the beginning
                memoryStream.Position = 0;

                // Return the file
                return File(memoryStream, metadata.ContentType, Path.GetFileName(path));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Error getting file {Path} from bucket {Bucket}", path, bucket);
                return StatusCode((int)ex.StatusCode, ex.ToApiErrorResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting file {Path} from bucket {Bucket}", path, bucket);
                return StatusCode(500, ApiErrorResponse.FromException(ex));
            }
        }

        [HttpPost("files/{*path}")]
        public async Task<IActionResult> UploadFile(string bucket, string path, IFormFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var contentType = file.ContentType;
                var filename = file.FileName;
                var response = await _storageClient.UploadFileAsync(path, stream, contentType, filename, null, null, false, false, cancellationToken);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Error uploading file {Path} to bucket {Bucket}", path, bucket);
                return StatusCode((int)ex.StatusCode, ex.ToApiErrorResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading file {Path} to bucket {Bucket}", path, bucket);
                return StatusCode(500, ApiErrorResponse.FromException(ex));
            }
        }

        [HttpDelete("files/{*path}")]
        public async Task<IActionResult> DeleteFile(string bucket, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _storageClient.DeleteFileAsync(bucket, path, cancellationToken);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Error deleting file {Path} from bucket {Bucket}", path, bucket);
                return StatusCode((int)ex.StatusCode, ex.ToApiErrorResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting file {Path} from bucket {Bucket}", path, bucket);
                return StatusCode(500, ApiErrorResponse.FromException(ex));
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(string bucket = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _storageClient.GetStatisticsAsync(bucket, cancellationToken);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Error getting statistics for bucket {Bucket}", bucket);
                return StatusCode((int)ex.StatusCode, ex.ToApiErrorResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting statistics for bucket {Bucket}", bucket);
                return StatusCode(500, ApiErrorResponse.FromException(ex));
            }
        }
    }
}
