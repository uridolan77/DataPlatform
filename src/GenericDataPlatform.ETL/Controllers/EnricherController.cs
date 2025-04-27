using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Enrichers;
using GenericDataPlatform.ETL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/enrichers")]
    public class EnricherController : ControllerBase
    {
        private readonly IEnumerable<IEnricher> _enrichers;
        private readonly ILogger<EnricherController> _logger;

        public EnricherController(IEnumerable<IEnricher> enrichers, ILogger<EnricherController> logger)
        {
            _enrichers = enrichers;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<EnricherInfo>> GetEnrichers()
        {
            var enrichers = _enrichers.Select(e => new EnricherInfo
            {
                Type = e.Type,
                DisplayName = GetDisplayName(e.Type),
                Description = GetDescription(e.Type),
                SupportedRules = GetSupportedRules(e.Type)
            });

            return Ok(enrichers);
        }

        [HttpGet("{type}")]
        public ActionResult<EnricherInfo> GetEnricher(string type)
        {
            var enricher = _enrichers.FirstOrDefault(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (enricher == null)
            {
                return NotFound();
            }

            var enricherInfo = new EnricherInfo
            {
                Type = enricher.Type,
                DisplayName = GetDisplayName(enricher.Type),
                Description = GetDescription(enricher.Type),
                SupportedRules = GetSupportedRules(enricher.Type)
            };

            return Ok(enricherInfo);
        }

        [HttpPost("enrich")]
        public async Task<IActionResult> Enrich([FromBody] EnrichRequest request)
        {
            try
            {
                var enricher = _enrichers.FirstOrDefault(e => e.Type.Equals(request.EnricherType, StringComparison.OrdinalIgnoreCase));

                if (enricher == null)
                {
                    return NotFound($"Enricher of type '{request.EnricherType}' not found");
                }

                var result = await enricher.EnrichAsync(request.Input, request.Configuration, request.Source);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching data");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private string GetDisplayName(string type)
        {
            return type switch
            {
                "Data" => "Data Enricher",
                "Lookup" => "Lookup Enricher",
                _ => type
            };
        }

        private string GetDescription(string type)
        {
            return type switch
            {
                "Data" => "Enrich data with derived fields, transformations, and calculated values",
                "Lookup" => "Enrich data with values from reference datasets",
                _ => $"Enrich data using {type}"
            };
        }

        private List<EnricherRule> GetSupportedRules(string type)
        {
            var rules = new List<EnricherRule>();

            switch (type.ToLowerInvariant())
            {
                case "data":
                    rules.Add(new EnricherRule
                    {
                        Name = "derived",
                        DisplayName = "Derived Field",
                        Description = "Create a derived field based on an expression",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "expression", DisplayName = "Expression", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "transform",
                        DisplayName = "Transform",
                        Description = "Apply transformations to field values",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "transformType", DisplayName = "Transform Type", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "combine",
                        DisplayName = "Combine Fields",
                        Description = "Combine multiple fields into one",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceFields", DisplayName = "Source Fields", Type = "array", IsRequired = true },
                            new RuleParameter { Name = "separator", DisplayName = "Separator", Type = "string", IsRequired = false }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "split",
                        DisplayName = "Split Field",
                        Description = "Split a field into multiple parts",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "delimiter", DisplayName = "Delimiter", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "index", DisplayName = "Index", Type = "integer", IsRequired = true }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "format",
                        DisplayName = "Format",
                        Description = "Format a field value",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "format", DisplayName = "Format", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "default",
                        DisplayName = "Default Value",
                        Description = "Set a default value if the source field is null or missing",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "defaultValue", DisplayName = "Default Value", Type = "object", IsRequired = true }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "extract",
                        DisplayName = "Extract",
                        Description = "Extract a part of a field value",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "extractType", DisplayName = "Extract Type", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new EnricherRule
                    {
                        Name = "calculate",
                        DisplayName = "Calculate",
                        Description = "Calculate a value based on an expression",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "targetField", DisplayName = "Target Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "expression", DisplayName = "Expression", Type = "string", IsRequired = true }
                        }
                    });
                    break;

                case "lookup":
                    rules.Add(new EnricherRule
                    {
                        Name = "lookup",
                        DisplayName = "Lookup",
                        Description = "Lookup values from a reference dataset",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "lookupSource", DisplayName = "Lookup Source", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "lookupType", DisplayName = "Lookup Type", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "lookupField", DisplayName = "Lookup Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "targetFields", DisplayName = "Target Fields", Type = "object", IsRequired = true },
                            new RuleParameter { Name = "defaultValues", DisplayName = "Default Values", Type = "object", IsRequired = false }
                        }
                    });
                    break;
            }

            return rules;
        }
    }

    public class EnricherInfo
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<EnricherRule> SupportedRules { get; set; }
    }

    public class EnricherRule
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<RuleParameter> Parameters { get; set; }
    }



    public class EnrichRequest
    {
        public string EnricherType { get; set; }
        public object Input { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public DataSourceDefinition Source { get; set; }
    }
}
