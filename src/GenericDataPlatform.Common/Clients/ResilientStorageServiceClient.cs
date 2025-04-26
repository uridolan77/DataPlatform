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
        public async Task<StorageMetadata> GetMetadataAsync(string bucket, string path, CancellationToken cancellationToken = default)
        {
            var request = new GetMetadataRequest
            {
                Bucket = bucket,
                Path = path
            };

            return await _clientFactory.CallServiceAsync(
                () => _client.GetMetadataAsync(request, cancellationToken: cancellationToken),
                _serviceName,
                nameof(GetMetadataAsync));
        }

        /// <summary>
        /// Lists files in a bucket
        /// </summary>
        public async Task<ListFilesResponse> ListFilesAsync(string bucket, string prefix = null, CancellationToken cancellationToken = default)
        {
            var request = new ListFilesRequest
            {
                Bucket = bucket,
                Prefix = prefix ?? string.Empty
            };

            return await _clientFactory.CallServiceAsync(
                () => _client.ListFilesAsync(request, cancellationToken: cancellationToken),
                _serviceName,
                nameof(ListFilesAsync));
        }

        /// <summary>
        /// Uploads a file to storage
        /// </summary>
        public async Task<UploadFileResponse> UploadFileAsync(string bucket, string path, Stream content, CancellationToken cancellationToken = default)
        {
            // Create a call with resilience policies
            var call = _client.UploadFile(cancellationToken: cancellationToken);

            try
            {
                // Send metadata first
                await call.RequestStream.WriteAsync(new UploadFileRequest
                {
                    Metadata = new FileMetadata
                    {
                        Bucket = bucket,
                        Path = path,
                        ContentType = GetContentType(path),
                        Size = content.Length
                    }
                });

                // Send file content in chunks
                var buffer = new byte[64 * 1024]; // 64 KB chunks
                int bytesRead;
                while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await call.RequestStream.WriteAsync(new UploadFileRequest
                    {
                        ChunkData = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead)
                    });
                }

                // Complete the request
                await call.RequestStream.CompleteAsync();

                // Get the response
                return await call.ResponseAsync;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Upload cancelled by user for {Bucket}/{Path}", bucket, path);
                throw;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error uploading file to {Bucket}/{Path}. Status: {Status}, Detail: {Detail}",
                    bucket, path, ex.StatusCode, ex.Status.Detail);
                throw GrpcErrorHandling.MapRpcExceptionToApplicationException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading file to {Bucket}/{Path}", bucket, path);
                throw;
            }
        }

        /// <summary>
        /// Downloads a file from storage
        /// </summary>
        public async Task DownloadFileAsync(string bucket, string path, Stream destination, CancellationToken cancellationToken = default)
        {
            var request = new DownloadFileRequest
            {
                Bucket = bucket,
                Path = path
            };

            // Create streaming call
            using var call = _client.DownloadFile(request, cancellationToken: cancellationToken);

            try
            {
                // Process the response stream
                await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    if (response.ChunkData != null)
                    {
                        await destination.WriteAsync(response.ChunkData.ToByteArray(), 0, response.ChunkData.Length, cancellationToken);
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Download cancelled by user for {Bucket}/{Path}", bucket, path);
                throw;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error downloading file from {Bucket}/{Path}. Status: {Status}, Detail: {Detail}",
                    bucket, path, ex.StatusCode, ex.Status.Detail);
                throw GrpcErrorHandling.MapRpcExceptionToApplicationException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading file from {Bucket}/{Path}", bucket, path);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        public async Task<DeleteFileResponse> DeleteFileAsync(string bucket, string path, CancellationToken cancellationToken = default)
        {
            var request = new DeleteFileRequest
            {
                Bucket = bucket,
                Path = path
            };

            return await _clientFactory.CallServiceAsync(
                () => _client.DeleteFileAsync(request, cancellationToken: cancellationToken),
                _serviceName,
                nameof(DeleteFileAsync));
        }

        /// <summary>
        /// Gets storage statistics
        /// </summary>
        public async Task<StorageStatistics> GetStatisticsAsync(string bucket = null, CancellationToken cancellationToken = default)
        {
            var request = new GetStatisticsRequest
            {
                Bucket = bucket ?? string.Empty
            };

            return await _clientFactory.CallServiceAsync(
                () => _client.GetStatisticsAsync(request, cancellationToken: cancellationToken),
                _serviceName,
                nameof(GetStatisticsAsync));
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
