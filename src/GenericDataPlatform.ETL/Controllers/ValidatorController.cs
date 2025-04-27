using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using GenericDataPlatform.ETL.Models;
using GenericDataPlatform.ETL.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.ETL.Controllers
{
    [ApiController]
    [Route("api/validators")]
    public class ValidatorController : ControllerBase
    {
        private readonly IEnumerable<IValidator> _validators;
        private readonly ILogger<ValidatorController> _logger;

        public ValidatorController(IEnumerable<IValidator> validators, ILogger<ValidatorController> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ValidatorInfo>> GetValidators()
        {
            var validators = _validators.Select(v => new ValidatorInfo
            {
                Type = v.Type,
                DisplayName = GetDisplayName(v.Type),
                Description = GetDescription(v.Type),
                SupportedRules = GetSupportedRules(v.Type)
            });

            return Ok(validators);
        }

        [HttpGet("{type}")]
        public ActionResult<ValidatorInfo> GetValidator(string type)
        {
            var validator = _validators.FirstOrDefault(v => v.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (validator == null)
            {
                return NotFound();
            }

            var validatorInfo = new ValidatorInfo
            {
                Type = validator.Type,
                DisplayName = GetDisplayName(validator.Type),
                Description = GetDescription(validator.Type),
                SupportedRules = GetSupportedRules(validator.Type)
            };

            return Ok(validatorInfo);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateRequest request)
        {
            try
            {
                var validator = _validators.FirstOrDefault(v => v.Type.Equals(request.ValidatorType, StringComparison.OrdinalIgnoreCase));

                if (validator == null)
                {
                    return NotFound($"Validator of type '{request.ValidatorType}' not found");
                }

                var result = await validator.ValidateAsync(request.Input, request.Configuration, request.Source);

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed");
                return BadRequest(new { Error = ex.Message, ValidationResult = ex.ValidationResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating data");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private string GetDisplayName(string type)
        {
            return type switch
            {
                "Schema" => "Schema Validator",
                "DataQuality" => "Data Quality Validator",
                _ => type
            };
        }

        private string GetDescription(string type)
        {
            return type switch
            {
                "Schema" => "Validate data against schema definitions with type checking, required fields, and custom validation rules",
                "DataQuality" => "Validate data quality with checks for nulls, patterns, ranges, uniqueness, and more",
                _ => $"Validate data using {type}"
            };
        }

        private List<ValidatorRule> GetSupportedRules(string type)
        {
            var rules = new List<ValidatorRule>();

            switch (type.ToLowerInvariant())
            {
                case "schema":
                    rules.Add(new ValidatorRule
                    {
                        Name = "typeCheck",
                        DisplayName = "Type Check",
                        Description = "Validate field types against schema",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "schema", DisplayName = "Schema", Type = "object", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "requiredFields",
                        DisplayName = "Required Fields",
                        Description = "Validate required fields are present and non-null",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "schema", DisplayName = "Schema", Type = "object", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "stringLength",
                        DisplayName = "String Length",
                        Description = "Validate string length constraints",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "schema", DisplayName = "Schema", Type = "object", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "numericRange",
                        DisplayName = "Numeric Range",
                        Description = "Validate numeric range constraints",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "schema", DisplayName = "Schema", Type = "object", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "pattern",
                        DisplayName = "Pattern",
                        Description = "Validate string pattern constraints",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "schema", DisplayName = "Schema", Type = "object", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "enumValues",
                        DisplayName = "Enum Values",
                        Description = "Validate enum value constraints",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "schema", DisplayName = "Schema", Type = "object", IsRequired = true }
                        }
                    });
                    break;

                case "dataquality":
                    rules.Add(new ValidatorRule
                    {
                        Name = "nullCheck",
                        DisplayName = "Null Check",
                        Description = "Check for null values",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "field", DisplayName = "Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "allowNull", DisplayName = "Allow Null", Type = "boolean", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "pattern",
                        DisplayName = "Pattern",
                        Description = "Check if values match a pattern",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "field", DisplayName = "Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "pattern", DisplayName = "Pattern", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "range",
                        DisplayName = "Range",
                        Description = "Check if numeric values are within a range",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "field", DisplayName = "Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "min", DisplayName = "Minimum", Type = "number", IsRequired = false },
                            new RuleParameter { Name = "max", DisplayName = "Maximum", Type = "number", IsRequired = false }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "length",
                        DisplayName = "Length",
                        Description = "Check if string length is within constraints",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "field", DisplayName = "Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "minLength", DisplayName = "Minimum Length", Type = "integer", IsRequired = false },
                            new RuleParameter { Name = "maxLength", DisplayName = "Maximum Length", Type = "integer", IsRequired = false }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "format",
                        DisplayName = "Format",
                        Description = "Check if values match a specific format",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "field", DisplayName = "Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "format", DisplayName = "Format", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "uniqueness",
                        DisplayName = "Uniqueness",
                        Description = "Check if values are unique",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "fields", DisplayName = "Fields", Type = "array", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "referentialIntegrity",
                        DisplayName = "Referential Integrity",
                        Description = "Check referential integrity against a reference dataset",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "sourceField", DisplayName = "Source Field", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "referenceDataset", DisplayName = "Reference Dataset", Type = "string", IsRequired = true },
                            new RuleParameter { Name = "referenceField", DisplayName = "Reference Field", Type = "string", IsRequired = true }
                        }
                    });
                    rules.Add(new ValidatorRule
                    {
                        Name = "consistency",
                        DisplayName = "Consistency",
                        Description = "Check consistency between fields",
                        Parameters = new List<RuleParameter>
                        {
                            new RuleParameter { Name = "conditions", DisplayName = "Conditions", Type = "array", IsRequired = true }
                        }
                    });
                    break;
            }

            return rules;
        }
    }

    public class ValidatorInfo
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<ValidatorRule> SupportedRules { get; set; }
    }

    public class ValidatorRule
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<RuleParameter> Parameters { get; set; }
    }



    public class ValidateRequest
    {
        public string ValidatorType { get; set; }
        public object Input { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public DataSourceDefinition Source { get; set; }
    }
}
