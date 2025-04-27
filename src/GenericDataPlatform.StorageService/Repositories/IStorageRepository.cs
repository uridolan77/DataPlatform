using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GenericDataPlatform.StorageService.Repositories
{
    public interface IStorageRepository
    {
        Task<string> StoreAsync(Stream dataStream, StorageMetadata metadata);
        Task<Stream> RetrieveAsync(string path);
        Task<StorageMetadata> GetMetadataAsync(string path);
        Task<IEnumerable<StorageItem>> ListAsync(string prefix, bool recursive = false);
        Task<bool> DeleteAsync(string path);
        Task<string> CopyAsync(string sourcePath, string destinationPath);
        Task<StorageStatistics> GetStatisticsAsync(string prefix = null);
    }

    public record StorageMetadata
    {
        public string Id { get; set; }
        public string SourceId { get; set; }
        public string ContentType { get; set; }
        public string Filename { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Checksum { get; set; }
        public string StorageTier { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
        public Dictionary<string, string> CustomMetadata { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public class StorageItem
    {
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public StorageMetadata Metadata { get; set; }
    }

    public class StorageStatistics
    {
        public long TotalItems { get; set; }
        public long TotalSize { get; set; }
        public Dictionary<string, long> ItemsByType { get; set; }
        public Dictionary<string, long> SizeByType { get; set; }
    }
}
