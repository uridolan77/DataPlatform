using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.StorageService.Repositories
{
    public class S3Repository : IStorageRepository
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IOptions<S3Options> _options;
        private readonly ILogger<S3Repository> _logger;
        
        public S3Repository(
            IAmazonS3 s3Client,
            IOptions<S3Options> options,
            ILogger<S3Repository> logger)
        {
            _s3Client = s3Client;
            _options = options;
            _logger = logger;
        }
        
        public async Task<string> StoreAsync(Stream dataStream, StorageMetadata metadata)
        {
            try
            {
                // Generate a path based on metadata
                var path = GeneratePath(metadata);
                
                // Create a put request
                var putRequest = new PutObjectRequest
                {
                    BucketName = _options.Value.BucketName,
                    Key = path,
                    InputStream = dataStream,
                    ContentType = metadata.ContentType
                };
                
                // Add metadata as headers
                foreach (var (key, value) in metadata.Properties)
                {
                    putRequest.Metadata.Add(key, value);
                }
                
                // Upload to S3
                await _s3Client.PutObjectAsync(putRequest);
                
                // Return the generated path
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file in S3 {filename}", metadata.Filename);
                throw;
            }
        }
        
        public async Task<Stream> RetrieveAsync(string path)
        {
            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _options.Value.BucketName,
                    Key = path
                };
                
                var response = await _s3Client.GetObjectAsync(getRequest);
                
                // Create a memory stream to detach from the response
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file from S3 {path}", path);
                throw;
            }
        }
        
        public async Task<StorageMetadata> GetMetadataAsync(string path)
        {
            try
            {
                var metadataRequest = new GetObjectMetadataRequest
                {
                    BucketName = _options.Value.BucketName,
                    Key = path
                };
                
                var response = await _s3Client.GetObjectMetadataAsync(metadataRequest);
                
                // Extract metadata from response
                var metadata = new StorageMetadata
                {
                    Id = path,
                    Filename = Path.GetFileName(path),
                    ContentType = response.Headers.ContentType,
                    Size = response.Headers.ContentLength,
                    CreatedAt = response.LastModified,
                    Properties = new Dictionary<string, string>()
                };
                
                // Add custom metadata
                foreach (var key in response.Metadata.Keys)
                {
                    metadata.Properties[key] = response.Metadata[key];
                }
                
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for file {path}", path);
                throw;
            }
        }
        
        public async Task<IEnumerable<StorageItem>> ListAsync(string prefix, bool recursive = false)
        {
            try
            {
                var items = new List<StorageItem>();
                var request = new ListObjectsV2Request
                {
                    BucketName = _options.Value.BucketName,
                    Prefix = prefix,
                    Delimiter = recursive ? null : "/"
                };
                
                ListObjectsV2Response response;
                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);
                    
                    // Add directories (common prefixes)
                    foreach (var commonPrefix in response.CommonPrefixes)
                    {
                        items.Add(new StorageItem
                        {
                            Path = commonPrefix,
                            IsDirectory = true,
                            Metadata = new StorageMetadata
                            {
                                Id = commonPrefix,
                                Filename = Path.GetFileName(commonPrefix.TrimEnd('/')),
                                ContentType = "application/x-directory",
                                CreatedAt = DateTime.UtcNow
                            }
                        });
                    }
                    
                    // Add files
                    foreach (var s3Object in response.S3Objects)
                    {
                        // Skip directories (objects ending with /)
                        if (s3Object.Key.EndsWith("/"))
                        {
                            continue;
                        }
                        
                        items.Add(new StorageItem
                        {
                            Path = s3Object.Key,
                            IsDirectory = false,
                            Metadata = new StorageMetadata
                            {
                                Id = s3Object.Key,
                                Filename = Path.GetFileName(s3Object.Key),
                                Size = s3Object.Size,
                                CreatedAt = s3Object.LastModified,
                                ContentType = GetContentTypeFromExtension(Path.GetExtension(s3Object.Key))
                            }
                        });
                    }
                    
                    request.ContinuationToken = response.NextContinuationToken;
                }
                while (response.IsTruncated);
                
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files with prefix {prefix}", prefix);
                throw;
            }
        }
        
        public async Task<bool> DeleteAsync(string path)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _options.Value.BucketName,
                    Key = path
                };
                
                await _s3Client.DeleteObjectAsync(deleteRequest);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {path}", path);
                throw;
            }
        }
        
        public async Task<string> CopyAsync(string sourcePath, string destinationPath)
        {
            try
            {
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _options.Value.BucketName,
                    SourceKey = sourcePath,
                    DestinationBucket = _options.Value.BucketName,
                    DestinationKey = destinationPath
                };
                
                await _s3Client.CopyObjectAsync(copyRequest);
                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {sourcePath} to {destinationPath}", sourcePath, destinationPath);
                throw;
            }
        }
        
        public async Task<StorageStatistics> GetStatisticsAsync(string prefix = null)
        {
            try
            {
                var statistics = new StorageStatistics
                {
                    TotalItems = 0,
                    TotalSize = 0,
                    ItemsByType = new Dictionary<string, long>(),
                    SizeByType = new Dictionary<string, long>()
                };
                
                var request = new ListObjectsV2Request
                {
                    BucketName = _options.Value.BucketName,
                    Prefix = prefix
                };
                
                ListObjectsV2Response response;
                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);
                    
                    foreach (var s3Object in response.S3Objects)
                    {
                        // Skip directories (objects ending with /)
                        if (s3Object.Key.EndsWith("/"))
                        {
                            continue;
                        }
                        
                        statistics.TotalItems++;
                        statistics.TotalSize += s3Object.Size;
                        
                        var extension = Path.GetExtension(s3Object.Key).ToLowerInvariant();
                        if (string.IsNullOrEmpty(extension))
                        {
                            extension = "unknown";
                        }
                        
                        if (!statistics.ItemsByType.ContainsKey(extension))
                        {
                            statistics.ItemsByType[extension] = 0;
                            statistics.SizeByType[extension] = 0;
                        }
                        
                        statistics.ItemsByType[extension]++;
                        statistics.SizeByType[extension] += s3Object.Size;
                    }
                    
                    request.ContinuationToken = response.NextContinuationToken;
                }
                while (response.IsTruncated);
                
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for prefix {prefix}", prefix);
                throw;
            }
        }
        
        public async Task<QueryResult> QueryAsync(DataQuery query)
        {
            try
            {
                // Validate query
                if (query == null)
                {
                    throw new ArgumentNullException(nameof(query));
                }
                
                // Initialize result
                var result = new QueryResult
                {
                    Records = new List<DataRecord>(),
                    TotalCount = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
                
                // Get the prefix to search in
                string prefix = query.Prefix ?? string.Empty;
                
                // List all objects with the given prefix
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _options.Value.BucketName,
                    Prefix = prefix,
                    MaxKeys = 1000 // Use a large value to minimize API calls
                };
                
                // Collect all matching objects
                var allObjects = new List<S3Object>();
                ListObjectsV2Response listResponse;
                
                do
                {
                    listResponse = await _s3Client.ListObjectsV2Async(listRequest);
                    allObjects.AddRange(listResponse.S3Objects);
                    listRequest.ContinuationToken = listResponse.NextContinuationToken;
                }
                while (listResponse.IsTruncated);
                
                // Filter objects based on query conditions
                var filteredObjects = allObjects;
                
                // Apply filters if specified
                if (query.Filters != null && query.Filters.Any())
                {
                    filteredObjects = filteredObjects.Where(obj => 
                    {
                        // Get object metadata
                        var metadataTask = _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                        {
                            BucketName = _options.Value.BucketName,
                            Key = obj.Key
                        });
                        metadataTask.Wait(); // Synchronously wait for metadata
                        var metadata = metadataTask.Result;
                        
                        // Check if all filters match
                        foreach (var filter in query.Filters)
                        {
                            // Check metadata properties
                            if (metadata.Metadata.TryGetValue(filter.Field, out var value))
                            {
                                if (!MatchesFilter(value, filter.Operator, filter.Value))
                                {
                                    return false;
                                }
                            }
                            // Check standard properties
                            else if (filter.Field.Equals("size", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!MatchesFilter(metadata.Headers.ContentLength.ToString(), filter.Operator, filter.Value))
                                {
                                    return false;
                                }
                            }
                            else if (filter.Field.Equals("lastModified", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!MatchesFilter(metadata.LastModified.ToString("o"), filter.Operator, filter.Value))
                                {
                                    return false;
                                }
                            }
                            else if (filter.Field.Equals("contentType", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!MatchesFilter(metadata.Headers.ContentType, filter.Operator, filter.Value))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                // If the field doesn't exist, the filter doesn't match
                                return false;
                            }
                        }
                        
                        return true;
                    }).ToList();
                }
                
                // Apply sorting if specified
                if (!string.IsNullOrEmpty(query.SortField))
                {
                    filteredObjects = SortObjects(filteredObjects, query.SortField, query.SortDirection).ToList();
                }
                else
                {
                    // Default sort by last modified date
                    filteredObjects = filteredObjects.OrderByDescending(obj => obj.LastModified).ToList();
                }
                
                // Get total count
                result.TotalCount = filteredObjects.Count;
                
                // Apply pagination
                var paginatedObjects = filteredObjects
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();
                
                // Load content for each object and convert to DataRecord
                foreach (var obj in paginatedObjects)
                {
                    // Get object content
                    var getRequest = new GetObjectRequest
                    {
                        BucketName = _options.Value.BucketName,
                        Key = obj.Key
                    };
                    
                    using var response = await _s3Client.GetObjectAsync(getRequest);
                    using var reader = new StreamReader(response.ResponseStream);
                    var content = await reader.ReadToEndAsync();
                    
                    // Try to parse content as JSON
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(content);
                        var data = new Dictionary<string, object>();
                        
                        // Extract properties from JSON
                        ExtractJsonProperties(jsonDoc.RootElement, data);
                        
                        // Create DataRecord
                        var record = new DataRecord
                        {
                            Id = obj.Key,
                            SchemaId = query.SchemaId,
                            SourceId = query.SourceId,
                            Data = data,
                            Metadata = new Dictionary<string, string>
                            {
                                ["path"] = obj.Key,
                                ["size"] = obj.Size.ToString(),
                                ["lastModified"] = obj.LastModified.ToString("o"),
                                ["storageClass"] = obj.StorageClass
                            },
                            CreatedAt = obj.LastModified,
                            UpdatedAt = obj.LastModified,
                            Version = "1.0"
                        };
                        
                        result.Records.Add(record);
                    }
                    catch (JsonException)
                    {
                        // If not valid JSON, create a simple DataRecord with the content as a string
                        var record = new DataRecord
                        {
                            Id = obj.Key,
                            SchemaId = query.SchemaId,
                            SourceId = query.SourceId,
                            Data = new Dictionary<string, object>
                            {
                                ["content"] = content
                            },
                            Metadata = new Dictionary<string, string>
                            {
                                ["path"] = obj.Key,
                                ["size"] = obj.Size.ToString(),
                                ["lastModified"] = obj.LastModified.ToString("o"),
                                ["storageClass"] = obj.StorageClass
                            },
                            CreatedAt = obj.LastModified,
                            UpdatedAt = obj.LastModified,
                            Version = "1.0"
                        };
                        
                        result.Records.Add(record);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query");
                throw;
            }
        }
        
        private string GeneratePath(StorageMetadata metadata)
        {
            // Generate a path based on metadata
            var path = string.Empty;
            
            // Use the specified path if available
            if (metadata.Properties.TryGetValue("path", out var specifiedPath))
            {
                path = specifiedPath;
            }
            else
            {
                // Generate a path based on date and content type
                var date = metadata.CreatedAt.ToString("yyyy/MM/dd");
                var contentTypeFolder = metadata.ContentType.Split('/')[0];
                
                path = $"{contentTypeFolder}/{date}/{metadata.Id}_{metadata.Filename}";
            }
            
            return path;
        }
        
        private string GetContentTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                ".zip" => "application/zip",
                ".doc" or ".docx" => "application/msword",
                ".xls" or ".xlsx" => "application/vnd.ms-excel",
                ".ppt" or ".pptx" => "application/vnd.ms-powerpoint",
                _ => "application/octet-stream"
            };
        }
        
        private bool MatchesFilter(string value, string op, string filterValue)
        {
            return op.ToLowerInvariant() switch
            {
                "eq" => value.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                "neq" => !value.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                "contains" => value.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                "startswith" => value.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                "endswith" => value.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                "gt" => string.Compare(value, filterValue, StringComparison.OrdinalIgnoreCase) > 0,
                "gte" => string.Compare(value, filterValue, StringComparison.OrdinalIgnoreCase) >= 0,
                "lt" => string.Compare(value, filterValue, StringComparison.OrdinalIgnoreCase) < 0,
                "lte" => string.Compare(value, filterValue, StringComparison.OrdinalIgnoreCase) <= 0,
                _ => false
            };
        }
        
        private IEnumerable<S3Object> SortObjects(IEnumerable<S3Object> objects, string sortField, string sortDirection)
        {
            var isAscending = string.IsNullOrEmpty(sortDirection) || 
                              sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
            
            return sortField.ToLowerInvariant() switch
            {
                "key" => isAscending ? 
                    objects.OrderBy(o => o.Key) : 
                    objects.OrderByDescending(o => o.Key),
                
                "size" => isAscending ? 
                    objects.OrderBy(o => o.Size) : 
                    objects.OrderByDescending(o => o.Size),
                
                "lastmodified" => isAscending ? 
                    objects.OrderBy(o => o.LastModified) : 
                    objects.OrderByDescending(o => o.LastModified),
                
                "storageclass" => isAscending ? 
                    objects.OrderBy(o => o.StorageClass) : 
                    objects.OrderByDescending(o => o.StorageClass),
                
                _ => isAscending ? 
                    objects.OrderBy(o => o.LastModified) : 
                    objects.OrderByDescending(o => o.LastModified)
            };
        }
        
        private void ExtractJsonProperties(JsonElement element, Dictionary<string, object> data, string prefix = "")
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var propertyName = string.IsNullOrEmpty(prefix) ? 
                            property.Name : 
                            $"{prefix}.{property.Name}";
                        
                        ExtractJsonProperties(property.Value, data, propertyName);
                    }
                    break;
                
                case JsonValueKind.Array:
                    // For arrays, store the JSON string
                    data[prefix] = element.GetRawText();
                    break;
                
                case JsonValueKind.String:
                    data[prefix] = element.GetString();
                    break;
                
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                    {
                        data[prefix] = intValue;
                    }
                    else if (element.TryGetInt64(out var longValue))
                    {
                        data[prefix] = longValue;
                    }
                    else if (element.TryGetDouble(out var doubleValue))
                    {
                        data[prefix] = doubleValue;
                    }
                    else
                    {
                        data[prefix] = element.GetRawText();
                    }
                    break;
                
                case JsonValueKind.True:
                    data[prefix] = true;
                    break;
                
                case JsonValueKind.False:
                    data[prefix] = false;
                    break;
                
                case JsonValueKind.Null:
                    data[prefix] = null;
                    break;
            }
        }
    }
    
    public class S3Options
    {
        public string BucketName { get; set; }
        public string Region { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
    }
    
    public class DataQuery
    {
        public string SourceId { get; set; }
        public string SchemaId { get; set; }
        public string Prefix { get; set; }
        public List<QueryFilter> Filters { get; set; }
        public string SortField { get; set; }
        public string SortDirection { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
    
    public class QueryFilter
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
    
    public class QueryResult
    {
        public List<DataRecord> Records { get; set; }
        public long TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
