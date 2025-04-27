using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Extensions;
using GenericDataPlatform.ETL.Transformers.Base;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Transformers.Xml
{
    public class XmlTransformer : ITransformer
    {
        private readonly ILogger<XmlTransformer> _logger;

        public string Type => "Xml";

        public XmlTransformer(ILogger<XmlTransformer> logger)
        {
            _logger = logger;
        }

        public async Task<object> TransformAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            try
            {
                // Determine the input type and handle accordingly
                if (input is IEnumerable<DataRecord> inputRecords)
                {
                    // Input is already a collection of DataRecord objects
                    return await TransformDataRecordsAsync(inputRecords, configuration, source);
                }
                else if (input is string xmlContent)
                {
                    // Input is an XML string
                    return await TransformXmlStringAsync(xmlContent, configuration, source);
                }
                else if (input is byte[] xmlBytes)
                {
                    // Input is XML bytes
                    var xmlString = Encoding.UTF8.GetString(xmlBytes);
                    return await TransformXmlStringAsync(xmlString, configuration, source);
                }
                else if (input is Stream xmlStream)
                {
                    // Input is a stream
                    using var reader = new StreamReader(xmlStream, Encoding.UTF8, true, 1024, true);
                    var xmlString = await reader.ReadToEndAsync();
                    return await TransformXmlStringAsync(xmlString, configuration, source);
                }
                else
                {
                    throw new ArgumentException($"Unsupported input type: {input?.GetType().Name ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming XML data");
                throw;
            }
        }

        private async Task<object> TransformDataRecordsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            // Get transformation type
            if (!configuration.TryGetValue("transformationType", out var transformationTypeObj))
            {
                throw new ArgumentException("Transformation type is required");
            }

            var transformationType = transformationTypeObj.ToString();

            // Apply the transformation
            switch (transformationType.ToLowerInvariant())
            {
                case "toxml":
                    return await ConvertToXmlAsync(records, configuration);

                case "filter":
                    return await FilterRecordsAsync(records, configuration);

                case "map":
                    return await MapFieldsAsync(records, configuration);

                default:
                    throw new NotSupportedException($"Transformation type {transformationType} is not supported");
            }
        }

        private async Task<object> TransformXmlStringAsync(string xmlContent, Dictionary<string, object> configuration, DataSourceDefinition source)
        {
            // Load XML document
            var doc = XDocument.Parse(xmlContent);

            // Get record path
            if (!configuration.TryGetValue("recordPath", out var recordPathObj))
            {
                throw new ArgumentException("Record path is required for XML transformation");
            }

            var recordPath = recordPathObj.ToString();

            // Get field mappings
            var fieldMappings = configuration.TryGetValue("fieldMappings", out var fieldMappingsObj) ?
                fieldMappingsObj as Dictionary<string, object> : null;

            // Get namespace manager
            var nsManager = new XmlNamespaceManager(new NameTable());

            if (configuration.TryGetValue("namespaces", out var namespacesObj) &&
                namespacesObj is Dictionary<string, object> namespaces)
            {
                foreach (var ns in namespaces)
                {
                    nsManager.AddNamespace(ns.Key, ns.Value.ToString());
                }
            }

            // Find record elements
            var records = new List<DataRecord>();
            var recordElements = doc.XPathSelectElements(recordPath, nsManager).ToList();

            for (int i = 0; i < recordElements.Count; i++)
            {
                var element = recordElements[i];
                var data = new Dictionary<string, object>();

                if (fieldMappings != null)
                {
                    // Use field mappings
                    foreach (var mapping in fieldMappings)
                    {
                        var fieldName = mapping.Key;
                        var xpath = mapping.Value.ToString();

                        try
                        {
                            var node = element.XPathSelectElement(xpath, nsManager);
                            if (node != null)
                            {
                                data[fieldName] = node.Value;
                            }
                            else
                            {
                                var attr = element.XPathSelectAttribute(xpath, nsManager);
                                if (attr != null)
                                {
                                    data[fieldName] = attr.Value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error extracting field {FieldName} with XPath {XPath}", fieldName, xpath);
                        }
                    }
                }
                else
                {
                    // Auto-extract fields from elements and attributes
                    // Elements
                    foreach (var childElement in element.Elements())
                    {
                        data[childElement.Name.LocalName] = childElement.Value;
                    }

                    // Attributes
                    foreach (var attribute in element.Attributes())
                    {
                        data[$"@{attribute.Name.LocalName}"] = attribute.Value;
                    }
                }

                // Create record
                var record = new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SchemaId = source.Schema?.Id,
                    SourceId = source.Id,
                    Data = data,
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "XML",
                        ["recordIndex"] = i.ToString(),
                        ["elementName"] = element.Name.LocalName
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                records.Add(record);
            }

            // Apply additional transformations if specified
            if (configuration.TryGetValue("transformationType", out var transformationTypeObj))
            {
                return await TransformDataRecordsAsync(records, configuration, source);
            }

            return records;
        }

        private async Task<object> ConvertToXmlAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get XML configuration
            var rootElement = configuration.TryGetValue("rootElement", out var rootElementObj) ?
                rootElementObj.ToString() : "root";

            var recordElement = configuration.TryGetValue("recordElement", out var recordElementObj) ?
                recordElementObj.ToString() : "record";

            var includeDeclaration = !configuration.TryGetValue("includeDeclaration", out var includeDeclarationObj) ||
                (includeDeclarationObj is bool includeDeclarationBool && includeDeclarationBool);

            var indent = configuration.TryGetValue("indent", out var indentObj) &&
                indentObj is bool indentBool && indentBool;

            // Get fields to include
            var includeFields = configuration.TryGetValue("fields", out var fieldsObj) ?
                (fieldsObj as IEnumerable<object>)?.Select(f => f.ToString()).ToList() : null;

            // Create XML document
            var doc = new XDocument();

            if (includeDeclaration)
            {
                doc.Declaration = new XDeclaration("1.0", "utf-8", null);
            }

            // Create root element
            var root = new XElement(rootElement);
            doc.Add(root);

            // Get all field names if not specified
            if (includeFields == null || !includeFields.Any())
            {
                includeFields = records
                    .SelectMany(r => r.Data.Keys)
                    .Distinct()
                    .ToList();
            }

            // Add records
            foreach (var record in records)
            {
                var recordElem = new XElement(recordElement);

                foreach (var field in includeFields)
                {
                    if (record.Data.TryGetValue(field, out var value))
                    {
                        if (field.StartsWith("@"))
                        {
                            // Add as attribute
                            recordElem.Add(new XAttribute(field.Substring(1), value?.ToString() ?? string.Empty));
                        }
                        else
                        {
                            // Add as element
                            recordElem.Add(new XElement(field, value?.ToString() ?? string.Empty));
                        }
                    }
                }

                root.Add(recordElem);
            }

            // Convert to string
            var settings = new XmlWriterSettings
            {
                Indent = indent,
                OmitXmlDeclaration = !includeDeclaration
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            doc.WriteTo(xmlWriter);
            await xmlWriter.FlushAsync();

            return stringWriter.ToString();
        }

        private async Task<object> FilterRecordsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get filter conditions
            if (!configuration.TryGetValue("filterConditions", out var filterConditionsObj))
            {
                throw new ArgumentException("Filter conditions are required for filter transformation");
            }

            var filterConditions = filterConditionsObj as Dictionary<string, object>;
            if (filterConditions == null)
            {
                throw new ArgumentException("Filter conditions must be a dictionary");
            }

            // Apply filter
            var filteredRecords = records.ToList();

            foreach (var condition in filterConditions)
            {
                var field = condition.Key;
                var value = condition.Value;

                filteredRecords = filteredRecords
                    .Where(r => r.Data.TryGetValue(field, out var fieldValue) &&
                               IsMatch(fieldValue, value))
                    .ToList();
            }

            return await Task.FromResult(filteredRecords);
        }

        private async Task<object> MapFieldsAsync(IEnumerable<DataRecord> records, Dictionary<string, object> configuration)
        {
            // Get field mappings
            if (!configuration.TryGetValue("fieldMappings", out var fieldMappingsObj))
            {
                throw new ArgumentException("Field mappings are required for map transformation");
            }

            var fieldMappings = fieldMappingsObj as Dictionary<string, object>;
            if (fieldMappings == null)
            {
                throw new ArgumentException("Field mappings must be a dictionary");
            }

            // Apply mappings
            var mappedRecords = new List<DataRecord>();

            foreach (var record in records)
            {
                var mappedData = new Dictionary<string, object>();

                foreach (var mapping in fieldMappings)
                {
                    var targetField = mapping.Key;
                    var sourceField = mapping.Value.ToString();

                    if (record.Data.TryGetValue(sourceField, out var value))
                    {
                        mappedData[targetField] = value;
                    }
                }

                // Copy fields that are not mapped
                if (configuration.TryGetValue("includeUnmappedFields", out var includeUnmappedFieldsObj) &&
                    includeUnmappedFieldsObj is bool includeUnmappedFields && includeUnmappedFields)
                {
                    foreach (var field in record.Data)
                    {
                        if (!fieldMappings.Values.Contains(field.Key))
                        {
                            mappedData[field.Key] = field.Value;
                        }
                    }
                }

                // Create a new record with mapped data
                var mappedRecord = new DataRecord
                {
                    Id = record.Id,
                    SchemaId = record.SchemaId,
                    SourceId = record.SourceId,
                    Data = mappedData,
                    Metadata = record.Metadata,
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                    Version = record.Version
                };

                mappedRecords.Add(mappedRecord);
            }

            return await Task.FromResult(mappedRecords);
        }

        private bool IsMatch(object fieldValue, object conditionValue)
        {
            if (fieldValue == null && conditionValue == null)
            {
                return true;
            }

            if (fieldValue == null || conditionValue == null)
            {
                return false;
            }

            // Handle special condition values
            if (conditionValue is string conditionString)
            {
                // Check for null condition
                if (conditionString == "null")
                {
                    return fieldValue == null;
                }

                // Check for not null condition
                if (conditionString == "!null")
                {
                    return fieldValue != null;
                }

                // Check for wildcard condition
                if (conditionString.Contains("*"))
                {
                    var pattern = "^" + conditionString.Replace("*", ".*") + "$";
                    return System.Text.RegularExpressions.Regex.IsMatch(fieldValue.ToString(), pattern);
                }

                // Check for range condition
                if (conditionString.StartsWith("[") && conditionString.EndsWith("]") && conditionString.Contains(","))
                {
                    var range = conditionString.Trim('[', ']').Split(',');
                    if (range.Length == 2)
                    {
                        var min = double.Parse(range[0]);
                        var max = double.Parse(range[1]);
                        var value = Convert.ToDouble(fieldValue);

                        return value >= min && value <= max;
                    }
                }

                // Check for comparison conditions
                if (conditionString.StartsWith(">"))
                {
                    var value = double.Parse(conditionString.Substring(1));
                    return Convert.ToDouble(fieldValue) > value;
                }

                if (conditionString.StartsWith("<"))
                {
                    var value = double.Parse(conditionString.Substring(1));
                    return Convert.ToDouble(fieldValue) < value;
                }

                if (conditionString.StartsWith(">="))
                {
                    var value = double.Parse(conditionString.Substring(2));
                    return Convert.ToDouble(fieldValue) >= value;
                }

                if (conditionString.StartsWith("<="))
                {
                    var value = double.Parse(conditionString.Substring(2));
                    return Convert.ToDouble(fieldValue) <= value;
                }

                if (conditionString.StartsWith("!="))
                {
                    var value = conditionString.Substring(2);
                    return !fieldValue.ToString().Equals(value);
                }
            }

            // Default equality check
            return fieldValue.ToString().Equals(conditionValue.ToString());
        }
    }
}
