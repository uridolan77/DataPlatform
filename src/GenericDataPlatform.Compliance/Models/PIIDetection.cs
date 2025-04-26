using System.Collections.Generic;

namespace GenericDataPlatform.Compliance.Models
{
    /// <summary>
    /// Represents a detected PII instance
    /// </summary>
    public class PIIDetection
    {
        /// <summary>
        /// Type of PII detected (e.g., email, phone, creditcard)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// The value that was detected
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Start index in the original text (for string detection)
        /// </summary>
        public int StartIndex { get; set; }
        
        /// <summary>
        /// End index in the original text (for string detection)
        /// </summary>
        public int EndIndex { get; set; }
        
        /// <summary>
        /// Field name where the PII was detected (for dictionary detection)
        /// </summary>
        public string FieldName { get; set; }
        
        /// <summary>
        /// Confidence score of the detection (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }
    }
    
    /// <summary>
    /// Result of a PII scan
    /// </summary>
    public class PIIScanResult
    {
        /// <summary>
        /// List of detected PII instances
        /// </summary>
        public List<PIIDetection> Detections { get; set; } = new List<PIIDetection>();
        
        /// <summary>
        /// Original data that was scanned
        /// </summary>
        public object OriginalData { get; set; }
        
        /// <summary>
        /// Masked data with PII removed or obscured
        /// </summary>
        public object MaskedData { get; set; }
        
        /// <summary>
        /// Whether any PII was detected
        /// </summary>
        public bool ContainsPII => Detections.Count > 0;
        
        /// <summary>
        /// Summary of detected PII types
        /// </summary>
        public Dictionary<string, int> PIITypeCounts
        {
            get
            {
                var counts = new Dictionary<string, int>();
                foreach (var detection in Detections)
                {
                    if (!counts.ContainsKey(detection.Type))
                    {
                        counts[detection.Type] = 0;
                    }
                    counts[detection.Type]++;
                }
                return counts;
            }
        }
    }
}
