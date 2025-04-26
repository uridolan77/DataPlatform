using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.StorageService.Repositories
{
    public class FileSystemRepository : IStorageRepository
    {
        private readonly FileSystemOptions _options;
        private readonly ILogger<FileSystemRepository> _logger;
        
        public FileSystemRepository(IOptions<FileSystemOptions> options, ILogger<FileSystemRepository> logger)
        {
            _options = options.Value;
            _logger = logger;
            
            // Ensure the base directory exists
            Directory.CreateDirectory(_options.BasePath);
        }
        
        public async Task<string> StoreAsync(Stream dataStream, StorageMetadata metadata)
        {
            try
            {
                // Generate a path based on metadata
                var path = GeneratePath(metadata);
                var fullPath = Path.Combine(_options.BasePath, path);
                
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write the file
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 
                    bufferSize: 4096, useAsync: true))
                {
                    await dataStream.CopyToAsync(fileStream);
                }
                
                // Calculate checksum
                var checksum = await CalculateChecksumAsync(fullPath);
                
                // Save metadata
                await SaveMetadataAsync(path, metadata with { Checksum = checksum });
                
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file {filename}", metadata.Filename);
                throw;
            }
        }
        
        public async Task<Stream> RetrieveAsync(string path)
        {
            try
            {
                var fullPath = Path.Combine(_options.BasePath, path);
                
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {path}");
                }
                
                // Create a memory stream to detach from the file
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    bufferSize: 4096, useAsync: true))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }
                
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {path}", path);
                throw;
            }
        }
        
        public async Task<StorageMetadata> GetMetadataAsync(string path)
        {
            try
            {
                var metadataPath = GetMetadataPath(path);
                
                if (!File.Exists(metadataPath))
                {
                    throw new FileNotFoundException($"Metadata not found for: {path}");
                }
                
                var json = await File.ReadAllTextAsync(metadataPath);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<StorageMetadata>(json);
                
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for {path}", path);
                throw;
            }
        }
        
        public async Task<IEnumerable<StorageItem>> ListAsync(string prefix, bool recursive = false)
        {
            try
            {
                var basePath = Path.Combine(_options.BasePath, prefix);
                
                if (!Directory.Exists(basePath))
                {
                    return Enumerable.Empty<StorageItem>();
                }
                
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(basePath, "*", searchOption);
                var directories = Directory.GetDirectories(basePath, "*", searchOption);
                
                var items = new List<StorageItem>();
                
                // Add directories
                foreach (var directory in directories)
                {
                    var relativePath = directory.Substring(_options.BasePath.Length).TrimStart(Path.DirectorySeparatorChar);
                    
                    items.Add(new StorageItem
                    {
                        Path = relativePath,
                        IsDirectory = true
                    });
                }
                
                // Add files
                foreach (var file in files)
                {
                    // Skip metadata files
                    if (file.EndsWith(".metadata.json"))
                    {
                        continue;
                    }
                    
                    var relativePath = file.Substring(_options.BasePath.Length).TrimStart(Path.DirectorySeparatorChar);
                    
                    try
                    {
                        var metadata = await GetMetadataAsync(relativePath);
                        
                        items.Add(new StorageItem
                        {
                            Path = relativePath,
                            IsDirectory = false,
                            Metadata = metadata
                        });
                    }
                    catch
                    {
                        // If metadata is not available, create a basic item
                        var fileInfo = new FileInfo(file);
                        
                        items.Add(new StorageItem
                        {
                            Path = relativePath,
                            IsDirectory = false,
                            Metadata = new StorageMetadata
                            {
                                Id = relativePath,
                                Filename = Path.GetFileName(file),
                                Size = fileInfo.Length,
                                CreatedAt = fileInfo.CreationTimeUtc,
                                ContentType = GetContentTypeFromExtension(Path.GetExtension(file))
                            }
                        });
                    }
                }
                
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
                var fullPath = Path.Combine(_options.BasePath, path);
                var metadataPath = GetMetadataPath(path);
                
                if (!File.Exists(fullPath))
                {
                    return false;
                }
                
                File.Delete(fullPath);
                
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {path}", path);
                return false;
            }
        }
        
        public async Task<string> CopyAsync(string sourcePath, string destinationPath)
        {
            try
            {
                var sourceFullPath = Path.Combine(_options.BasePath, sourcePath);
                var destinationFullPath = Path.Combine(_options.BasePath, destinationPath);
                
                if (!File.Exists(sourceFullPath))
                {
                    throw new FileNotFoundException($"Source file not found: {sourcePath}");
                }
                
                // Ensure the destination directory exists
                var directory = Path.GetDirectoryName(destinationFullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Copy the file
                File.Copy(sourceFullPath, destinationFullPath, overwrite: true);
                
                // Copy the metadata
                var sourceMetadataPath = GetMetadataPath(sourcePath);
                var destinationMetadataPath = GetMetadataPath(destinationPath);
                
                if (File.Exists(sourceMetadataPath))
                {
                    var metadata = await GetMetadataAsync(sourcePath);
                    
                    // Update the metadata for the destination
                    metadata = metadata with
                    {
                        Id = destinationPath,
                        Filename = Path.GetFileName(destinationPath)
                    };
                    
                    await SaveMetadataAsync(destinationPath, metadata);
                }
                
                return destinationPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {sourcePath} to {destinationPath}", 
                    sourcePath, destinationPath);
                throw;
            }
        }
        
        public async Task<StorageStatistics> GetStatisticsAsync(string prefix = null)
        {
            try
            {
                var items = await ListAsync(prefix ?? "", true);
                
                var statistics = new StorageStatistics
                {
                    TotalItems = 0,
                    TotalSize = 0,
                    ItemsByType = new Dictionary<string, long>(),
                    SizeByType = new Dictionary<string, long>()
                };
                
                foreach (var item in items)
                {
                    if (item.IsDirectory)
                    {
                        continue;
                    }
                    
                    statistics.TotalItems++;
                    statistics.TotalSize += item.Metadata.Size;
                    
                    // Group by content type
                    var contentType = item.Metadata.ContentType ?? "application/octet-stream";
                    
                    if (!statistics.ItemsByType.ContainsKey(contentType))
                    {
                        statistics.ItemsByType[contentType] = 0;
                        statistics.SizeByType[contentType] = 0;
                    }
                    
                    statistics.ItemsByType[contentType]++;
                    statistics.SizeByType[contentType] += item.Metadata.Size;
                }
                
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for prefix {prefix}", prefix);
                throw;
            }
        }
        
        private string GeneratePath(StorageMetadata metadata)
        {
            // Generate a path based on metadata
            // Format: {sourceId}/{year}/{month}/{day}/{filename}
            
            var timestamp = metadata.CreatedAt;
            var sourceId = metadata.SourceId ?? "unknown";
            var filename = metadata.Filename;
            
            // Ensure filename is unique by adding a timestamp if needed
            if (_options.EnsureUniqueFilenames)
            {
                var extension = Path.GetExtension(filename);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                filename = $"{nameWithoutExtension}_{timestamp:yyyyMMddHHmmss}{extension}";
            }
            
            return Path.Combine(
                sourceId,
                timestamp.Year.ToString(),
                timestamp.Month.ToString("00"),
                timestamp.Day.ToString("00"),
                filename);
        }
        
        private string GetMetadataPath(string path)
        {
            return Path.Combine(_options.BasePath, $"{path}.metadata.json");
        }
        
        private async Task SaveMetadataAsync(string path, StorageMetadata metadata)
        {
            var metadataPath = GetMetadataPath(path);
            
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(metadataPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(metadataPath, json);
        }
        
        private async Task<string> CalculateChecksumAsync(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        
        private string GetContentTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                ".7z" => "application/x-7z-compressed",
                _ => "application/octet-stream"
            };
        }
    }
    
    public class FileSystemOptions
    {
        public string BasePath { get; set; }
        public bool EnsureUniqueFilenames { get; set; } = true;
    }
}
