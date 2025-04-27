using System;
using System.Collections.Generic;
using System.Linq;
using GenericDataPlatform.ML.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Data;

namespace GenericDataPlatform.ML.Utils
{
    /// <summary>
    /// Utility for converting between ML.NET schemas and platform schemas
    /// </summary>
    public interface ISchemaConverter
    {
        /// <summary>
        /// Converts a ML.NET schema to a platform schema
        /// </summary>
        /// <param name="dataViewSchema">ML.NET schema</param>
        /// <returns>Platform schema (features and labels)</returns>
        (List<FeatureDefinition> Features, List<LabelDefinition> Labels) ConvertToPlatformSchema(DataViewSchema dataViewSchema);
        
        /// <summary>
        /// Converts platform schema to a ML.NET schema definition
        /// </summary>
        /// <param name="features">Feature definitions</param>
        /// <param name="labels">Label definitions</param>
        /// <returns>Schema metadata for ML.NET</returns>
        Dictionary<string, ColumnMetadata> ConvertToColumnMetadata(List<FeatureDefinition> features, List<LabelDefinition> labels);
        
        /// <summary>
        /// Converts ML.NET column type to platform feature data type
        /// </summary>
        /// <param name="columnType">ML.NET column type</param>
        /// <returns>Platform feature data type</returns>
        FeatureDataType ConvertToFeatureDataType(DataViewType columnType);
        
        /// <summary>
        /// Converts ML.NET column type to platform label data type
        /// </summary>
        /// <param name="columnType">ML.NET column type</param>
        /// <returns>Platform label data type</returns>
        LabelDataType ConvertToLabelDataType(DataViewType columnType);
        
        /// <summary>
        /// Converts platform feature data type to ML.NET data kind
        /// </summary>
        /// <param name="dataType">Platform feature data type</param>
        /// <returns>ML.NET data kind</returns>
        DataKind ConvertToDataKind(FeatureDataType dataType);
        
        /// <summary>
        /// Converts platform label data type to ML.NET data kind
        /// </summary>
        /// <param name="dataType">Platform label data type</param>
        /// <returns>ML.NET data kind</returns>
        DataKind ConvertToDataKind(LabelDataType dataType);
    }
    
    /// <summary>
    /// Implementation of the schema converter
    /// </summary>
    public class SchemaConverter : ISchemaConverter
    {
        private readonly ILogger<SchemaConverter> _logger;
        
        public SchemaConverter(ILogger<SchemaConverter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Converts a ML.NET schema to a platform schema
        /// </summary>
        public (List<FeatureDefinition> Features, List<LabelDefinition> Labels) ConvertToPlatformSchema(DataViewSchema dataViewSchema)
        {
            try
            {
                if (dataViewSchema == null)
                {
                    throw new ArgumentNullException(nameof(dataViewSchema));
                }
                
                var features = new List<FeatureDefinition>();
                var labels = new List<LabelDefinition>();
                
                foreach (var column in dataViewSchema)
                {
                    // Check if column is a label
                    var isLabel = IsLabelColumn(column);
                    
                    if (isLabel)
                    {
                        var label = new LabelDefinition
                        {
                            Name = column.Name,
                            Description = GetColumnDescription(column),
                            DataType = ConvertToLabelDataType(column.Type)
                        };
                        
                        // If categorical, add possible values
                        if (label.DataType == LabelDataType.Categorical)
                        {
                            label.PossibleValues = GetKeyValues(column);
                        }
                        
                        labels.Add(label);
                    }
                    else
                    {
                        var feature = new FeatureDefinition
                        {
                            Name = column.Name,
                            Description = GetColumnDescription(column),
                            DataType = ConvertToFeatureDataType(column.Type),
                            IsRequired = IsRequiredColumn(column)
                        };
                        
                        // If array, set IsArray flag
                        if (column.Type.GetItemType() != null)
                        {
                            feature.IsArray = true;
                        }
                        
                        features.Add(feature);
                    }
                }
                
                _logger.LogInformation("Converted ML.NET schema with {ColumnCount} columns to platform schema with {FeatureCount} features and {LabelCount} labels", 
                    dataViewSchema.Count, features.Count, labels.Count);
                
                return (features, labels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting ML.NET schema to platform schema");
                throw;
            }
        }
        
        /// <summary>
        /// Converts platform schema to a ML.NET schema definition
        /// </summary>
        public Dictionary<string, ColumnMetadata> ConvertToColumnMetadata(List<FeatureDefinition> features, List<LabelDefinition> labels)
        {
            try
            {
                var metadata = new Dictionary<string, ColumnMetadata>();
                
                // Add features
                if (features != null)
                {
                    foreach (var feature in features)
                    {
                        metadata[feature.Name] = new ColumnMetadata
                        {
                            DataKind = ConvertToDataKind(feature.DataType),
                            IsFeature = true,
                            IsLabel = false,
                            Description = feature.Description,
                            IsArray = feature.IsArray
                        };
                    }
                }
                
                // Add labels
                if (labels != null)
                {
                    foreach (var label in labels)
                    {
                        metadata[label.Name] = new ColumnMetadata
                        {
                            DataKind = ConvertToDataKind(label.DataType),
                            IsFeature = false,
                            IsLabel = true,
                            Description = label.Description,
                            IsArray = false
                        };
                    }
                }
                
                _logger.LogInformation("Converted platform schema with {FeatureCount} features and {LabelCount} labels to {MetadataCount} column metadata entries", 
                    features?.Count ?? 0, labels?.Count ?? 0, metadata.Count);
                
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting platform schema to column metadata");
                throw;
            }
        }
        
        /// <summary>
        /// Converts ML.NET column type to platform feature data type
        /// </summary>
        public FeatureDataType ConvertToFeatureDataType(DataViewType columnType)
        {
            // Handle vector types
            var itemType = columnType.GetItemType();
            if (itemType != null)
            {
                // Return the item type
                return ConvertToFeatureDataType(itemType);
            }
            
            // Handle scalar types
            if (columnType is TextDataViewType)
            {
                return FeatureDataType.String;
            }
            else if (columnType is NumberDataViewType numberType)
            {
                if (numberType.RawType == typeof(int) || numberType.RawType == typeof(long))
                {
                    return FeatureDataType.Integer;
                }
                else
                {
                    return FeatureDataType.Float;
                }
            }
            else if (columnType is BooleanDataViewType)
            {
                return FeatureDataType.Boolean;
            }
            else if (columnType is DateTimeDataViewType)
            {
                return FeatureDataType.DateTime;
            }
            else if (columnType.ToString().Contains("Key"))
            {
                return FeatureDataType.Categorical;
            }
            else if (columnType.ToString().Contains("Image"))
            {
                return FeatureDataType.Image;
            }
            else
            {
                return FeatureDataType.String;
            }
        }
        
        /// <summary>
        /// Converts ML.NET column type to platform label data type
        /// </summary>
        public LabelDataType ConvertToLabelDataType(DataViewType columnType)
        {
            if (columnType is BooleanDataViewType)
            {
                return LabelDataType.Binary;
            }
            else if (columnType is NumberDataViewType)
            {
                return LabelDataType.Continuous;
            }
            else if (columnType.ToString().Contains("Key"))
            {
                return LabelDataType.Categorical;
            }
            else
            {
                return LabelDataType.Categorical;
            }
        }
        
        /// <summary>
        /// Converts platform feature data type to ML.NET data kind
        /// </summary>
        public DataKind ConvertToDataKind(FeatureDataType dataType)
        {
            switch (dataType)
            {
                case FeatureDataType.String:
                case FeatureDataType.Text:
                    return DataKind.String;
                
                case FeatureDataType.Integer:
                    return DataKind.Int32;
                
                case FeatureDataType.Float:
                    return DataKind.Single;
                
                case FeatureDataType.Boolean:
                    return DataKind.Boolean;
                
                case FeatureDataType.DateTime:
                    return DataKind.DateTime;
                
                case FeatureDataType.Categorical:
                    return DataKind.String; // Will be converted to key later
                
                case FeatureDataType.Image:
                    return DataKind.UInt8; // Byte array for images
                
                default:
                    return DataKind.String;
            }
        }
        
        /// <summary>
        /// Converts platform label data type to ML.NET data kind
        /// </summary>
        public DataKind ConvertToDataKind(LabelDataType dataType)
        {
            switch (dataType)
            {
                case LabelDataType.Binary:
                    return DataKind.Boolean;
                
                case LabelDataType.Continuous:
                    return DataKind.Single;
                
                case LabelDataType.Categorical:
                    return DataKind.String; // Will be converted to key later
                
                default:
                    return DataKind.String;
            }
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Checks if a column is a label column
        /// </summary>
        private bool IsLabelColumn(DataViewSchema.Column column)
        {
            // Check metadata for label annotations
            if (column.HasAnnotation(AnnotationUtils.Kinds.IsLabel))
            {
                return true;
            }
            
            // Check if column name contains "Label" or "Target"
            var name = column.Name.ToLowerInvariant();
            if (name.Contains("label") || name.Contains("target") || name == "y")
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if a column is required
        /// </summary>
        private bool IsRequiredColumn(DataViewSchema.Column column)
        {
            // Check metadata for required annotation
            if (column.HasAnnotation(AnnotationUtils.Kinds.IsNormalize))
            {
                return true;
            }
            
            // Default to true for now
            return true;
        }
        
        /// <summary>
        /// Gets the description of a column
        /// </summary>
        private string GetColumnDescription(DataViewSchema.Column column)
        {
            // Check metadata for slot names annotation
            if (column.HasAnnotation(AnnotationUtils.Kinds.SlotNames))
            {
                var slotNames = column.Annotations.GetValue<VBuffer<ReadOnlyMemory<char>>>(AnnotationUtils.Kinds.SlotNames);
                if (slotNames.Length > 0)
                {
                    return $"Features: {string.Join(", ", slotNames.GetValues().Take(3))}...";
                }
            }
            
            return column.Name;
        }
        
        /// <summary>
        /// Gets the key values for a categorical column
        /// </summary>
        private List<string> GetKeyValues(DataViewSchema.Column column)
        {
            var values = new List<string>();
            
            // Check metadata for key values annotation
            if (column.HasAnnotation(AnnotationUtils.Kinds.KeyValues))
            {
                var keyValues = column.Annotations.GetValue<VBuffer<ReadOnlyMemory<char>>>(AnnotationUtils.Kinds.KeyValues);
                foreach (var value in keyValues.GetValues())
                {
                    values.Add(value.ToString());
                }
            }
            
            return values;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Metadata for a ML.NET column
    /// </summary>
    public class ColumnMetadata
    {
        /// <summary>
        /// Data kind of the column
        /// </summary>
        public DataKind DataKind { get; set; }
        
        /// <summary>
        /// Whether the column is a feature
        /// </summary>
        public bool IsFeature { get; set; }
        
        /// <summary>
        /// Whether the column is a label
        /// </summary>
        public bool IsLabel { get; set; }
        
        /// <summary>
        /// Description of the column
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Whether the column is an array
        /// </summary>
        public bool IsArray { get; set; }
    }
}