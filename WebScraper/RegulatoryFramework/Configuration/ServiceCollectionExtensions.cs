using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebScraper.RegulatoryFramework.Implementation;
using WebScraper.RegulatoryFramework.Interfaces;

namespace WebScraper.RegulatoryFramework.Configuration
{
    /// <summary>
    /// Extension methods for service collection to register regulatory framework components
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the regulatory scraper services to the service collection
        /// </summary>
        public static IServiceCollection AddRegulatoryScraper(
            this IServiceCollection services,
            RegulatoryScraperConfig config)
        {
            // Validate configuration
            var validationErrors = config.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException(
                    $"Invalid regulatory scraper configuration: {string.Join(", ", validationErrors)}");
            }
            
            // Register configuration
            services.AddSingleton(config);
            
            // Register the enhanced scraper
            services.AddSingleton<EnhancedScraper>();
            
            // Register each component based on feature flags
            if (config.EnablePriorityCrawling)
            {
                services.AddSingleton<ICrawlStrategy, PriorityCrawler>();
            }
            
            if (config.EnableHierarchicalExtraction)
            {
                services.AddSingleton<IContentExtractor, StructureAwareExtractor>();
            }
            
            if (config.EnableDocumentProcessing)
            {
                services.AddSingleton<IDocumentProcessor, DocumentProcessor>();
            }
            
            if (config.EnableComplianceChangeDetection)
            {
                services.AddSingleton<IChangeDetector, ComplianceChangeDetector>();
            }
            
            if (config.EnableDomainClassification)
            {
                services.AddSingleton<IContentClassifier, ContentClassifier>();
            }
            
            if (config.EnableDynamicContentRendering)
            {
                services.AddSingleton<IDynamicContentRenderer, PlaywrightRenderer>();
            }
            
            if (config.EnableAlertSystem)
            {
                services.AddSingleton<IAlertService, AlertService>();
            }
            
            // Add state store based on configuration
            switch (config.StateStoreType)
            {
                case StateStoreType.Memory:
                    services.AddSingleton<IStateStore, InMemoryStateStore>();
                    break;
                case StateStoreType.File:
                    services.AddSingleton<IStateStore, FileSystemStateStore>();
                    break;
                case StateStoreType.Database:
                    services.AddSingleton<IStateStore, DatabaseStateStore>();
                    break;
                default:
                    services.AddSingleton<IStateStore, InMemoryStateStore>();
                    break;
            }
            
            return services;
        }
        
        /// <summary>
        /// Adds UK Gambling Commission specific scraper configuration
        /// </summary>
        public static IServiceCollection AddUKGCRegulatoryScraper(this IServiceCollection services)
        {
            var config = new RegulatoryScraperConfig
            {
                DomainName = "UKGamblingCommission",
                EnableHierarchicalExtraction = true,
                EnablePriorityCrawling = true,
                EnableDocumentProcessing = true,
                EnableComplianceChangeDetection = true,
                EnableDomainClassification = true,
                EnableDynamicContentRendering = true,
                EnableAlertSystem = true,
                
                HierarchicalExtractionConfig = new HierarchicalExtractionConfig
                {
                    ParentSelector = "section, article, div.gcweb-card, div.gcweb-panel",
                    TitleSelector = "h1, h2, h3, p.gcweb-heading-m",
                    ContentSelector = "p.gc-card__description, p.gcweb-body, .gcweb-body-l"
                },
                
                PriorityCrawlingConfig = new PriorityCrawlingConfig
                {
                    UrlPatterns = new List<PriorityCrawlingConfig.UrlPriorityPattern>
                    {
                        new PriorityCrawlingConfig.UrlPriorityPattern 
                        { 
                            Pattern = "/licensees-and-businesses/lccp", 
                            Priority = 0.9 
                        },
                        new PriorityCrawlingConfig.UrlPriorityPattern 
                        { 
                            Pattern = "/licensees-and-businesses/compliance", 
                            Priority = 0.8 
                        },
                        new PriorityCrawlingConfig.UrlPriorityPattern 
                        { 
                            Pattern = "/licensees-and-businesses/aml", 
                            Priority = 0.8 
                        },
                        new PriorityCrawlingConfig.UrlPriorityPattern 
                        { 
                            Pattern = "/news/enforcement-action", 
                            Priority = 0.8 
                        },
                        new PriorityCrawlingConfig.UrlPriorityPattern 
                        { 
                            Pattern = "/guidance/", 
                            Priority = 0.7 
                        },
                        new PriorityCrawlingConfig.UrlPriorityPattern 
                        { 
                            Pattern = ".pdf", 
                            Priority = 0.7 
                        }
                    },
                    ContentKeywords = new List<string>
                    {
                        "licence", "license", "requirement", "compliance", "condition",
                        "enforcement", "penalty", "fine", "money laundering"
                    }
                },
                
                DocumentProcessingConfig = new DocumentProcessingConfig
                {
                    DownloadDocuments = true,
                    ExtractMetadata = true,
                    ExtractFullText = true,
                    DocumentTypes = new List<string> { ".pdf", ".docx", ".xlsx" },
                    MetadataPatterns = new Dictionary<string, string>
                    {
                        { "EffectiveDate", @"(?i)effective\s*(?:from|date)?\s*:\s*(\d{1,2}\s+\w+\s+\d{4})" },
                        { "LicenceType", @"(?i)(remote|non-remote|gambling software|gaming machine)\s+licen[cs]e" },
                        { "RegulatorySection", @"(?i)(social responsibility|ordinary)\s+code\s+(\d+\.\d+\.\d+)" }
                    }
                },
                
                DynamicContentConfig = new DynamicContentConfig
                {
                    BrowserType = "chromium",
                    MaxConcurrentSessions = 3,
                    WaitForSelector = ".gcweb-card",
                    AutoClickSelector = "#cocc-banner-accept",
                    PostNavigationDelay = 2000
                },
                
                ClassificationConfig = new ClassificationConfig
                {
                    Categories = new Dictionary<string, List<string>>
                    {
                        { "Licensing", new List<string> { "licence", "license", "application", "personal licence", "operating licence" } },
                        { "AML", new List<string> { "anti-money laundering", "aml", "money laundering", "terrorist financing" } },
                        { "ResponsibleGambling", new List<string> { "responsible gambling", "player protection", "self-exclusion" } },
                        { "Compliance", new List<string> { "compliance", "regulatory returns", "key event" } },
                        { "Enforcement", new List<string> { "enforcement", "regulatory action", "sanction", "penalty", "fine" } },
                        { "LCCP", new List<string> { "lccp", "licence conditions and codes of practice", "code of practice" } }
                    }
                },
                
                ChangeDetectionConfig = new ChangeDetectionConfig
                {
                    SignificantKeywords = new List<string>
                    {
                        "must", "required", "mandatory", "shall", "condition", "obligation",
                        "effective", "from", "by", "deadline", "due date",
                        "fee", "payment", "cost", "charge", "price", "amount", "rate",
                        "penalty", "fine", "sanction", "enforcement", "action"
                    }
                },
                
                AlertSystemConfig = new AlertSystemConfig
                {
                    EnableEmailAlerts = true,
                    EmailRecipient = "regulatory-alerts@example.com",
                    AlertRules = new List<AlertSystemConfig.AlertRule>
                    {
                        new AlertSystemConfig.AlertRule
                        {
                            Name = "EnforcementAction",
                            Keywords = new List<string> { "sanction", "fine", "penalty", "regulatory action" },
                            Importance = AlertImportance.High
                        },
                        new AlertSystemConfig.AlertRule
                        {
                            Name = "RequirementChange",
                            Keywords = new List<string> { "must", "shall", "requirement", "obligation", "condition" },
                            Importance = AlertImportance.Medium
                        }
                    }
                }
            };
            
            return services.AddRegulatoryScraper(config);
        }
    }
}