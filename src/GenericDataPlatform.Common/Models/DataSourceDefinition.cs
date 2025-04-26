using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Common.Models
{
    public class DataSourceDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DataSourceType Type { get; set; }
        public Dictionary<string, string> ConnectionProperties { get; set; }
        public DataSchema Schema { get; set; }
        public DataIngestMode IngestMode { get; set; }
        public DataRefreshPolicy RefreshPolicy { get; set; }
        public Dictionary<string, string> ValidationRules { get; set; }
        public Dictionary<string, string> MetadataProperties { get; set; }
    }

    public enum DataSourceType
    {
        RestApi,
        Database,
        FileSystem,
        Streaming,
        Ftp,
        Custom
    }

    public enum DataIngestMode
    {
        FullLoad,
        Incremental,
        ChangeDataCapture
    }

    public enum DataRefreshPolicy
    {
        Manual,
        Scheduled,
        EventDriven
    }
}
