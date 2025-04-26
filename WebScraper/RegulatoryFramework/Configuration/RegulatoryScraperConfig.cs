using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WebScraper.RegulatoryFramework.Interfaces;

namespace WebScraper.RegulatoryFramework.Configuration
{
    /// <summary>
    /// Main configuration for the regulatory scraper
    /// </summary>
    public class RegulatoryScraperConfig
    {
        public string DomainName { get; set; } = "Default";
        
        // Feature flags
        public bool EnableHierarchicalExtraction { get; set; } = false;
        public bool EnablePriorityCrawling { get; set; } = false;
        public bool EnableDocumentProcessing { get; set; } = false;
        public bool EnableComplianceChangeDetection { get; set; } = false;
        public bool EnableDomainClassification { get; set; } = false;
        public bool EnableDynamicContentRendering { get; set; } = false;
        public bool EnableAlertSystem { get; set; } = false;
        
        // HTTP client settings
        public string UserAgent { get; set; } = "RegulatoryWebScraper/1.0";
        public int MaxConcurrentRequests { get; set; } = 5;
        public int RequestTimeoutSeconds { get; set; } = 30;
        public bool RespectRobotsTxt { get; set; } = true;
        
        // Component configurations
        public HierarchicalExtractionConfig HierarchicalExtractionConfig { get; set; } = new HierarchicalExtractionConfig();
        public PriorityCrawlingConfig PriorityCrawlingConfig { get; set; } = new PriorityCrawlingConfig();
        public DocumentProcessingConfig DocumentProcessingConfig { get; set; } = new DocumentProcessingConfig();
        public ChangeDetectionConfig ChangeDetectionConfig { get; set; } = new ChangeDetectionConfig();
        public ClassificationConfig ClassificationConfig { get; set; } = new ClassificationConfig();
        public DynamicContentConfig DynamicContentConfig { get; set; } = new DynamicContentConfig();
        public AlertSystemConfig AlertSystemConfig { get; set; } = new AlertSystemConfig();
        
        // State management
        public StateStoreType StateStoreType { get; set; } = StateStoreType.Memory;
        public StateStoreConfig StateStoreConfig { get; set; } = new StateStoreConfig();
        
        /// <summary>
        /// Validates the configuration and returns any validation errors
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            
            // Validate base settings
            if (string.IsNullOrWhiteSpace(DomainName))
                errors.Add("DomainName must be specified");
                
            if (MaxConcurrentRequests <= 0)
                errors.Add("MaxConcurrentRequests must be greater than 0");
                
            if (RequestTimeoutSeconds <= 0)
                errors.Add("RequestTimeoutSeconds must be greater than 0");
                
            // Validate component configurations if enabled
            if (EnableHierarchicalExtraction)
                errors.AddRange(ValidateObject(HierarchicalExtractionConfig, "HierarchicalExtractionConfig"));
                
            if (EnablePriorityCrawling)
                errors.AddRange(ValidateObject(PriorityCrawlingConfig, "PriorityCrawlingConfig"));
                
            if (EnableDocumentProcessing)
                errors.AddRange(ValidateObject(DocumentProcessingConfig, "DocumentProcessingConfig"));
                
            if (EnableComplianceChangeDetection)
                errors.AddRange(ValidateObject(ChangeDetectionConfig, "ChangeDetectionConfig"));
                
            if (EnableDomainClassification)
                errors.AddRange(ValidateObject(ClassificationConfig, "ClassificationConfig"));
                
            if (EnableDynamicContentRendering)
                errors.AddRange(ValidateObject(DynamicContentConfig, "DynamicContentConfig"));
                
            if (EnableAlertSystem)
                errors.AddRange(ValidateObject(AlertSystemConfig, "AlertSystemConfig"));
                
            // Validate state store configuration
            errors.AddRange(ValidateObject(StateStoreConfig, "StateStoreConfig"));
            
            return errors;
        }
        
        private List<string> ValidateObject(object obj, string prefix)
        {
            var errors = new List<string>();
            var context = new ValidationContext(obj, null, null);
            var validationResults = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(obj, context, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    errors.Add($"{prefix}: {validationResult.ErrorMessage}");
                }
            }
            
            return errors;
        }
    }
    
    /// <summary>
    /// Configuration for hierarchical content extraction
    /// </summary>
    public class HierarchicalExtractionConfig
    {
        [Required]
        public string ParentSelector { get; set; } = "section, article, div.content";
        
        [Required]
        public string TitleSelector { get; set; } = "h1, h2, h3, h4";
        
        [Required]
        public string ContentSelector { get; set; } = "p, ul, ol";
        
        public bool ExtractMetadata { get; set; } = true;
        
        public Dictionary<string, string> MetadataSelectors { get; set; } = new Dictionary<string, string>
        {
            { "PublishedDate", "span.date, .published-date, time" },
            { "Author", ".author, .byline" },
            { "Category", ".category, .section" }
        };
    }
    
    /// <summary>
    /// Configuration for priority-based crawling
    /// </summary>
    public class PriorityCrawlingConfig
    {
        public class UrlPriorityPattern
        {
            public string Pattern { get; set; }
            public double Priority { get; set; } = 0.5;
        }
        
        public List<UrlPriorityPattern> UrlPatterns { get; set; } = new List<UrlPriorityPattern>();
        
        public List<string> ContentKeywords { get; set; } = new List<string>();
        
        public int MaxUrlsPerCrawl { get; set; } = 100;
        
        public int MaxDepth { get; set; } = 3;
        
        public bool EnableAdaptivePrioritization { get; set; } = false;
    }
    
    /// <summary>
    /// Configuration for document processing
    /// </summary>
    public class DocumentProcessingConfig
    {
        public bool DownloadDocuments { get; set; } = true;
        
        public bool ExtractMetadata { get; set; } = true;
        
        public bool ExtractFullText { get; set; } = true;
        
        public List<string> DocumentTypes { get; set; } = new List<string> { ".pdf", ".docx", ".xlsx" };
        
        public string StoragePath { get; set; } = "DocumentStorage";
        
        public Dictionary<string, string> MetadataPatterns { get; set; } = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// Configuration for change detection
    /// </summary>
    public class ChangeDetectionConfig
    {
        public List<string> SignificantKeywords { get; set; } = new List<string>();
        
        public double MinChangeThreshold { get; set; } = 0.01; // 1%
        
        public double MajorChangeThreshold { get; set; } = 0.2; // 20%
        
        public bool EnableDiffGeneration { get; set; } = true;
        
        public bool TrackFullHistory { get; set; } = true;
        
        public int MaxHistoryVersions { get; set; } = 10;
    }
    
    /// <summary>
    /// Configuration for content classification
    /// </summary>
    public class ClassificationConfig
    {
        public Dictionary<string, List<string>> Categories { get; set; } = new Dictionary<string, List<string>>();
        
        public double MinConfidenceThreshold { get; set; } = 0.6;
        
        public bool EnableMachineLearning { get; set; } = false;
        
        public string ModelPath { get; set; } = "";
    }
    
    /// <summary>
    /// Configuration for dynamic content rendering
    /// </summary>
    public class DynamicContentConfig
    {
        [Required]
        public string BrowserType { get; set; } = "chromium";
        
        public int MaxConcurrentSessions { get; set; } = 2;
        
        public string WaitForSelector { get; set; } = "";
        
        public string AutoClickSelector { get; set; } = "";
        
        public int PostNavigationDelay { get; set; } = 1000;
        
        public bool EnableJavaScript { get; set; } = true;
        
        public string CustomJavaScript { get; set; } = "";
        
        public string ProxyServer { get; set; } = "";
    }
    
    /// <summary>
    /// Configuration for the alert system
    /// </summary>
    public class AlertSystemConfig
    {
        public class AlertRule
        {
            public string Name { get; set; }
            public List<string> Keywords { get; set; } = new List<string>();
            public AlertImportance Importance { get; set; } = AlertImportance.Medium;
        }
        
        public bool EnableEmailAlerts { get; set; } = false;
        
        public string EmailRecipient { get; set; } = "";
        
        public List<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
    }
    
    /// <summary>
    /// Configuration for state storage
    /// </summary>
    public class StateStoreConfig
    {
        public string FilePath { get; set; } = "StateStore";
        
        public string ConnectionString { get; set; } = "";
        
        public string DatabaseName { get; set; } = "RegulatoryScraperState";
        
        public int CacheExpirationMinutes { get; set; } = 60;
    }
}