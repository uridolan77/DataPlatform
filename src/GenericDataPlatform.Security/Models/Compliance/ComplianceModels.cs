using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models.Compliance
{
    /// <summary>
    /// Compliance report
    /// </summary>
    public class ComplianceReport
    {
        /// <summary>
        /// Type of the report (e.g., GDPR, HIPAA)
        /// </summary>
        public string ReportType { get; set; }
        
        /// <summary>
        /// Time the report was generated
        /// </summary>
        public DateTime GenerationTime { get; set; }
        
        /// <summary>
        /// Compliance status
        /// </summary>
        public ComplianceStatus ComplianceStatus { get; set; }
        
        /// <summary>
        /// Compliance score (0-100)
        /// </summary>
        public int ComplianceScore { get; set; }
        
        /// <summary>
        /// Summary of the compliance status
        /// </summary>
        public string Summary { get; set; }
        
        /// <summary>
        /// Compliance requirements
        /// </summary>
        public List<ComplianceRequirement> Requirements { get; set; } = new List<ComplianceRequirement>();
        
        /// <summary>
        /// Compliance recommendations
        /// </summary>
        public List<ComplianceRecommendation> Recommendations { get; set; } = new List<ComplianceRecommendation>();
    }
    
    /// <summary>
    /// Compliance requirement
    /// </summary>
    public class ComplianceRequirement
    {
        /// <summary>
        /// ID of the requirement
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Category of the requirement
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Title of the requirement
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of the requirement
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Status of the requirement
        /// </summary>
        public RequirementStatus Status { get; set; }
        
        /// <summary>
        /// Evidence supporting the status
        /// </summary>
        public string Evidence { get; set; }
        
        /// <summary>
        /// When the requirement was last checked
        /// </summary>
        public DateTime LastChecked { get; set; }
    }
    
    /// <summary>
    /// Compliance recommendation
    /// </summary>
    public class ComplianceRecommendation
    {
        /// <summary>
        /// ID of the recommendation
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Priority of the recommendation (1-3, where 1 is highest)
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Title of the recommendation
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Description of the recommendation
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Action to take
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// IDs of the requirements this recommendation addresses
        /// </summary>
        public List<string> RequirementIds { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Compliance status
    /// </summary>
    public enum ComplianceStatus
    {
        Unknown,
        NonCompliant,
        PartiallyCompliant,
        Compliant
    }
    
    /// <summary>
    /// Requirement status
    /// </summary>
    public enum RequirementStatus
    {
        Unknown,
        NonCompliant,
        PartiallyCompliant,
        Compliant,
        NotApplicable
    }
}
