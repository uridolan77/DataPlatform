using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Transformers.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/transformers")]
    public class TransformerController : ControllerBase
    {
        private readonly IEnumerable<ITransformer> _transformers;
        private readonly ILogger<TransformerController> _logger;
        
        public TransformerController(IEnumerable<ITransformer> transformers, ILogger<TransformerController> logger)
        {
            _transformers = transformers;
            _logger = logger;
        }
        
        [HttpGet]
        public ActionResult<IEnumerable<TransformerInfo>> GetTransformers()
        {
            var transformers = _transformers.Select(t => new TransformerInfo
            {
                Type = t.Type,
                DisplayName = GetDisplayName(t.Type),
                Description = GetDescription(t.Type),
                SupportedOperations = GetSupportedOperations(t.Type)
            });
            
            return Ok(transformers);
        }
        
        [HttpGet("{type}")]
        public ActionResult<TransformerInfo> GetTransformer(string type)
        {
            var transformer = _transformers.FirstOrDefault(t => t.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            
            if (transformer == null)
            {
                return NotFound();
            }
            
            var transformerInfo = new TransformerInfo
            {
                Type = transformer.Type,
                DisplayName = GetDisplayName(transformer.Type),
                Description = GetDescription(transformer.Type),
                SupportedOperations = GetSupportedOperations(transformer.Type)
            };
            
            return Ok(transformerInfo);
        }
        
        [HttpPost("transform")]
        public async Task<IActionResult> Transform([FromBody] TransformRequest request)
        {
            try
            {
                var transformer = _transformers.FirstOrDefault(t => t.Type.Equals(request.TransformerType, StringComparison.OrdinalIgnoreCase));
                
                if (transformer == null)
                {
                    return NotFound($"Transformer of type '{request.TransformerType}' not found");
                }
                
                var result = await transformer.TransformAsync(request.Input, request.Configuration, request.Source);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming data");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        
        private string GetDisplayName(string type)
        {
            return type switch
            {
                "Json" => "JSON Transformer",
                "Csv" => "CSV Transformer",
                "Xml" => "XML Transformer",
                _ => type
            };
        }
        
        private string GetDescription(string type)
        {
            return type switch
            {
                "Json" => "Transform JSON data with filtering, mapping, and aggregation operations",
                "Csv" => "Transform CSV data with parsing, filtering, mapping, and aggregation operations",
                "Xml" => "Transform XML data with XPath support for handling complex XML structures",
                _ => $"Transform data using {type}"
            };
        }
        
        private List<TransformerOperation> GetSupportedOperations(string type)
        {
            var operations = new List<TransformerOperation>();
            
            switch (type.ToLowerInvariant())
            {
                case "json":
                    operations.Add(new TransformerOperation
                    {
                        Name = "filter",
                        DisplayName = "Filter",
                        Description = "Filter records based on conditions",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "filterConditions", DisplayName = "Filter Conditions", Type = "object", IsRequired = true }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "map",
                        DisplayName = "Map Fields",
                        Description = "Map fields from source to target",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "fieldMappings", DisplayName = "Field Mappings", Type = "object", IsRequired = true },
                            new OperationParameter { Name = "includeUnmappedFields", DisplayName = "Include Unmapped Fields", Type = "boolean", IsRequired = false }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "flatten",
                        DisplayName = "Flatten Nested Objects",
                        Description = "Flatten nested JSON objects",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "separator", DisplayName = "Separator", Type = "string", IsRequired = false },
                            new OperationParameter { Name = "maxDepth", DisplayName = "Max Depth", Type = "integer", IsRequired = false }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "aggregate",
                        DisplayName = "Aggregate",
                        Description = "Aggregate records by group",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "groupBy", DisplayName = "Group By", Type = "array", IsRequired = true },
                            new OperationParameter { Name = "aggregations", DisplayName = "Aggregations", Type = "object", IsRequired = true }
                        }
                    });
                    break;
                
                case "csv":
                    operations.Add(new TransformerOperation
                    {
                        Name = "toCsv",
                        DisplayName = "Convert to CSV",
                        Description = "Convert records to CSV format",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "delimiter", DisplayName = "Delimiter", Type = "string", IsRequired = false },
                            new OperationParameter { Name = "includeHeader", DisplayName = "Include Header", Type = "boolean", IsRequired = false },
                            new OperationParameter { Name = "fields", DisplayName = "Fields", Type = "array", IsRequired = false }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "filter",
                        DisplayName = "Filter",
                        Description = "Filter records based on conditions",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "filterConditions", DisplayName = "Filter Conditions", Type = "object", IsRequired = true }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "map",
                        DisplayName = "Map Fields",
                        Description = "Map fields from source to target",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "fieldMappings", DisplayName = "Field Mappings", Type = "object", IsRequired = true },
                            new OperationParameter { Name = "includeUnmappedFields", DisplayName = "Include Unmapped Fields", Type = "boolean", IsRequired = false }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "aggregate",
                        DisplayName = "Aggregate",
                        Description = "Aggregate records by group",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "groupBy", DisplayName = "Group By", Type = "array", IsRequired = true },
                            new OperationParameter { Name = "aggregations", DisplayName = "Aggregations", Type = "object", IsRequired = true }
                        }
                    });
                    break;
                
                case "xml":
                    operations.Add(new TransformerOperation
                    {
                        Name = "toXml",
                        DisplayName = "Convert to XML",
                        Description = "Convert records to XML format",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "rootElement", DisplayName = "Root Element", Type = "string", IsRequired = false },
                            new OperationParameter { Name = "recordElement", DisplayName = "Record Element", Type = "string", IsRequired = false },
                            new OperationParameter { Name = "includeDeclaration", DisplayName = "Include Declaration", Type = "boolean", IsRequired = false },
                            new OperationParameter { Name = "indent", DisplayName = "Indent", Type = "boolean", IsRequired = false },
                            new OperationParameter { Name = "fields", DisplayName = "Fields", Type = "array", IsRequired = false }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "filter",
                        DisplayName = "Filter",
                        Description = "Filter records based on conditions",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "filterConditions", DisplayName = "Filter Conditions", Type = "object", IsRequired = true }
                        }
                    });
                    operations.Add(new TransformerOperation
                    {
                        Name = "map",
                        DisplayName = "Map Fields",
                        Description = "Map fields from source to target",
                        Parameters = new List<OperationParameter>
                        {
                            new OperationParameter { Name = "fieldMappings", DisplayName = "Field Mappings", Type = "object", IsRequired = true },
                            new OperationParameter { Name = "includeUnmappedFields", DisplayName = "Include Unmapped Fields", Type = "boolean", IsRequired = false }
                        }
                    });
                    break;
            }
            
            return operations;
        }
    }
    
    public class TransformerInfo
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<TransformerOperation> SupportedOperations { get; set; }
    }
    
    public class TransformerOperation
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<OperationParameter> Parameters { get; set; }
    }
    
    public class OperationParameter
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
    }
    
    public class TransformRequest
    {
        public string TransformerType { get; set; }
        public object Input { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public DataSourceDefinition Source { get; set; }
    }
}
