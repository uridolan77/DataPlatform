using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebScraper.RegulatoryFramework.Interfaces
{
    /// <summary>
    /// Represents a node in a hierarchical content structure
    /// </summary>
    public class ContentNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Content { get; set; }
        public int Depth { get; set; }
        public string NodeType { get; set; } = "Section";
        public List<ContentNode> Children { get; set; } = new List<ContentNode>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Metadata for a processed document
    /// </summary>
    public class DocumentMetadata
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string LocalFilePath { get; set; }
        public string DocumentType { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime ProcessedDate { get; set; } = DateTime.Now;
        public int PageCount { get; set; }
        public string Category { get; set; }
        public double ClassificationConfidence { get; set; }
        public Dictionary<string, string> ExtractedMetadata { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Result of content change analysis
    /// </summary>
    public class ChangeAnalysisResult
    {
        public ChangeType ChangeType { get; set; }
        public int AddedContentCount { get; set; }
        public int RemovedContentCount { get; set; }
        public int ModifiedContentCount { get; set; }
        public double ChangePercentage { get; set; }
        public List<string> ChangedSections { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Types of content changes
    /// </summary>
    public enum ChangeType
    {
        None,
        Minor,
        Moderate,
        Major,
        Critical
    }
    
    /// <summary>
    /// Result of significant changes detection
    /// </summary>
    public class SignificantChangesResult
    {
        public bool HasSignificantChanges { get; set; }
        public ChangeType ChangeType { get; set; }
        public List<SignificantChange> SignificantChanges { get; set; } = new List<SignificantChange>();
        public double SignificanceScore { get; set; }
        public string ChangeSummary { get; set; }
    }
    
    /// <summary>
    /// Represents a specific significant change
    /// </summary>
    public class SignificantChange
    {
        public string Type { get; set; } // Added, Removed, Modified
        public string Content { get; set; }
        public string Context { get; set; }
        public double Significance { get; set; }
        public List<string> MatchedPatterns { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Version information for a page
    /// </summary>
    public class PageVersion
    {
        public string Url { get; set; }
        public string Hash { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.Now;
        public ChangeType ChangeFromPrevious { get; set; } = ChangeType.None;
        
        [JsonIgnore] // Don't serialize full content
        public string FullContent { get; set; }
        
        [JsonIgnore] // Don't serialize full text
        public string TextContent { get; set; }
        
        public string ContentSummary { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Result of content classification
    /// </summary>
    public class ClassificationResult
    {
        public string PrimaryCategory { get; set; }
        public double Confidence { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, double> CategoryScores { get; set; } = new Dictionary<string, double>();
        public List<string> MatchedKeywords { get; set; } = new List<string>();
    }
}