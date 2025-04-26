using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Common.Clients
{
    /// <summary>
    /// A resilient client for the Storage gRPC service
    /// </summary>
    public class ResilientStorageServiceClient : IDisposable
    {
        private readonly StorageService.StorageServiceClient _client;
        private readonly GrpcClientFactory _clientFactory;
        private readonly ILogger<ResilientStorageServiceClient> _logger;
        private readonly string _serviceName = "StorageService";

        public ResilientStorageServiceClient(
            StorageService.StorageServiceClient client,
            GrpcClientFactory clientFactory,
            ILogger<ResilientStorageServiceClient> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets metadata for a file
        /// </summary>
        public async Task<StorageMetadata> GetMetadataAsync(string path, CancellationToken cancellationToken = default)
        {
            var request = new GetMetadataRequest
            {
                Path = path
            };

            return await _clientFactory.CallServiceAsync(
                async () => 
                {
                    var response = await _client.GetMetadataAsync(request, cancellationToken: cancellationToken);
                    return response;
                },
                _serviceName,
                nameof(GetMetadataAsync));
        }

        /// <summary>
        /// Lists files in storage
        /// </summary>
        public async Task<ListFilesResponse> ListFilesAsync(string path = null, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default)
        {
            var request = new ListFilesRequest
            {
                Path = path ?? "",
                Prefix = prefix ?? "",
                Recursive = recursive
            };

            return await _clientFactory.CallServiceAsync(
                async () => 
                {
                    var response = await _client.ListFilesAsync(request, cancellationToken: cancellationToken);
                    return response;
                },
                _serviceName,
                nameof(ListFilesAsync));
        }

        /// <summary>
        /// Uploads a file to storage
        /// </summary>
        public async Task<UploadFileResponse> UploadFileAsync(string path, Stream content, string contentType = null, string filename = null, 
            Dictionary<string, string> metadata = null, string storageTier = null, bool compress = false, bool encrypt = false, 
            CancellationToken cancellationToken = default)
        {
            // Prepare the file metadata
            var fileMetadata = new FileMetadata
            {
                Filename = filename ?? Path.GetFileName(path),
                ContentType = contentType ?? GetContentType(path),
                TotalSize = content.Length,
                StorageTier = storageTier ?? "standard",
                Compress = compress,
                Encrypt = encrypt
            };

            // Add custom metadata if available
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    fileMetadata.CustomMetadata.Add(item.Key, item.Value);
                }
            }

            // Create the request with metadata
            var request = new UploadFileRequest
            {
                Path = path,
                ContentMimeType = contentType ?? GetContentType(path),
                Name = filename ?? Path.GetFileName(path),
                Metadata = fileMetadata
            };

            // Read content into memory for simple upload
            // For large files, this should be changed to use streaming
            using (var memoryStream = new MemoryStream())
            {
                await content.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                request.Content = Google.Protobuf.ByteString.FromStream(memoryStream);
            }

            try
            {
                // Send the request
                return await _clientFactory.CallServiceAsync(
                    async () => 
                    {
                        var response = await _client.UploadFileAsync(request, cancellationToken: cancellationToken);
                        return response;
                    },
                    _serviceName,
                    nameof(UploadFileAsync));
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error uploading file to {Path}. Status: {Status}, Detail: {Detail}",
                    path, ex.StatusCode, ex.Status.Detail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading file to {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Downloads a file from storage
        /// </summary>
        public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var request = new DownloadFileRequest
            {
                Path = path
            };

            try
            {
                var memoryStream = new MemoryStream();
                using var call = _client.DownloadFile(request, cancellationToken: cancellationToken);
                
                await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    if (response.ResponseCase == DownloadFileResponse.ResponseOneofCase.ChunkData)
                    {
                        await memoryStream.WriteAsync(response.ChunkData.ToByteArray(), 0, response.ChunkData.Length, cancellationToken);
                    }
                }
                
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Download cancelled by user for {Path}", path);
                throw;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error downloading file from {Path}. Status: {Status}, Detail: {Detail}",
                    path, ex.StatusCode, ex.Status.Detail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading file from {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        public async Task<DeleteFileResponse> DeleteFileAsync(string path, string id = null, CancellationToken cancellationToken = default)
        {
            var request = new DeleteFileRequest
            {
                Path = path
            };

            if (!string.IsNullOrEmpty(id))
            {
                request.Id = id;
            }

            return await _clientFactory.CallServiceAsync(
                async () => 
                {
                    var response = await _client.DeleteFileAsync(request, cancellationToken: cancellationToken);
                    return response;
                },
                _serviceName,
                nameof(DeleteFileAsync));
        }

        /// <summary>
        /// Gets storage statistics
        /// </summary>
        public async Task<StorageStatistics> GetStatisticsAsync(string prefix = null, CancellationToken cancellationToken = default)
        {
            var request = new GetStorageStatisticsRequest
            {
                Prefix = prefix ?? ""
            };

            return await _clientFactory.CallServiceAsync(
                async () => 
                {
                    var response = await _client.GetStorageStatisticsAsync(request, cancellationToken: cancellationToken);
                    return response;
                },
                _serviceName,
                nameof(GetStatisticsAsync));
        }

        /// <summary>
        /// Copies a file within storage
        /// </summary>
        public async Task<CopyFileResponse> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        {
            var request = new CopyFileRequest
            {
                SourcePath = sourcePath,
                DestinationPath = destinationPath
            };

            return await _clientFactory.CallServiceAsync(
                async () => 
                {
                    var response = await _client.CopyFileAsync(request, cancellationToken: cancellationToken);
                    return response;
                },
                _serviceName,
                nameof(CopyFileAsync));
        }

        /// <summary>
        /// Gets the content type based on file extension
        /// </summary>
        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                _ => "application/octet-stream"
            };
        }

        public void Dispose()
        {
            // Clean up any resources if needed
        }
    }
}
