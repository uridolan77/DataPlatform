using GenericDataPlatform.IngestionService.Checkpoints;

namespace GenericDataPlatform.IngestionService.Connectors.FileSystem
{
    /// <summary>
    /// Options for file system connectors
    /// </summary>
    public class FileSystemConnectorOptions
    {
        /// <summary>
        /// Type of checkpoint storage to use
        /// </summary>
        public CheckpointStorageType CheckpointStorageType { get; set; } = CheckpointStorageType.File;

        /// <summary>
        /// Default batch size for parallel processing
        /// </summary>
        public int DefaultBatchSize { get; set; } = 5;

        /// <summary>
        /// Whether to continue processing on error
        /// </summary>
        public bool ContinueOnError { get; set; } = false;

        /// <summary>
        /// Whether to enable parallel processing by default
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = false;

        /// <summary>
        /// Whether to enable compression detection by default
        /// </summary>
        public bool EnableCompressionDetection { get; set; } = true;
    }
}
