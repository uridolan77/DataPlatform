using System;
using System.Collections.Generic;

namespace GenericDataPlatform.Security.Models.DataCatalog
{
    /// <summary>
    /// Represents a term in the business glossary
    /// </summary>
    public class GlossaryTerm
    {
        /// <summary>
        /// Unique identifier for the glossary term
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the glossary term
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Definition of the glossary term
        /// </summary>
        public string Definition { get; set; }
        
        /// <summary>
        /// Category of the glossary term
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Abbreviation for the glossary term
        /// </summary>
        public string Abbreviation { get; set; }
        
        /// <summary>
        /// Synonyms for the glossary term
        /// </summary>
        public List<string> Synonyms { get; set; } = new List<string>();
        
        /// <summary>
        /// Related terms
        /// </summary>
        public List<string> RelatedTerms { get; set; } = new List<string>();
        
        /// <summary>
        /// Examples of the glossary term
        /// </summary>
        public List<string> Examples { get; set; } = new List<string>();
        
        /// <summary>
        /// Owner of the glossary term
        /// </summary>
        public string Owner { get; set; }
        
        /// <summary>
        /// Steward of the glossary term
        /// </summary>
        public string Steward { get; set; }
        
        /// <summary>
        /// Status of the glossary term
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Date and time when the glossary term was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date and time when the glossary term was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
