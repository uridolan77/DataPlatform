using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.FileSystem
{
    public class LocalFileSystemConnector : BaseFileSystemConnector
    {
        public LocalFileSystemConnector(ILogger<LocalFileSystemConnector> logger) : base(logger)
        {
        }

        protected override async Task<IEnumerable<string>> ListFilesAsync(DataSourceDefinition source, string filePattern)
        {
            // Extract connection properties
            if (!source.ConnectionProperties.TryGetValue("directory", out var directory))
            {
                throw new ArgumentException("Directory is required for local file system connection");
            }
            
            // Ensure directory exists
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }
            
            // Get file pattern
            if (string.IsNullOrEmpty(filePattern))
            {
                filePattern = "*.*"; // Default to all files
            }
            
            // Get recursive option
            bool recursive = source.ConnectionProperties.TryGetValue("recursive", out var recursiveStr) && 
                bool.TryParse(recursiveStr, out var recursiveBool) && recursiveBool;
            
            // List files
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directory, filePattern, searchOption);
            
            // Sort files by name
            Array.Sort(files);
            
            return await Task.FromResult(files.AsEnumerable());
        }

        protected override async Task<Stream> ReadFileAsync(DataSourceDefinition source, string filePath)
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            
            // Create a memory stream to detach from the file
            var memoryStream = new MemoryStream();
            
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                bufferSize: 4096, useAsync: true))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
