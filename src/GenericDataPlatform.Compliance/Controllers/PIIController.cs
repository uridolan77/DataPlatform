using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenericDataPlatform.Compliance.Models;
using GenericDataPlatform.Compliance.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Compliance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PIIController : ControllerBase
    {
        private readonly IPIIDetectionService _piiDetectionService;
        private readonly ILogger<PIIController> _logger;

        public PIIController(IPIIDetectionService piiDetectionService, ILogger<PIIController> logger)
        {
            _piiDetectionService = piiDetectionService;
            _logger = logger;
        }

        /// <summary>
        /// Detects PII in text
        /// </summary>
        [HttpPost("detect/text")]
        public IActionResult DetectPIIInText([FromBody] TextDetectionRequest request)
        {
            try
            {
                var detections = _piiDetectionService.DetectPII(request.Text).ToList();
                
                return Ok(new
                {
                    ContainsPII = detections.Count > 0,
                    Detections = detections,
                    PIITypes = detections.GroupBy(d => d.Type).Select(g => new { Type = g.Key, Count = g.Count() })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting PII in text");
                return StatusCode(500, "Error detecting PII");
            }
        }

        /// <summary>
        /// Detects PII in a dictionary of values
        /// </summary>
        [HttpPost("detect/data")]
        public IActionResult DetectPIIInData([FromBody] Dictionary<string, object> data)
        {
            try
            {
                var detections = _piiDetectionService.DetectPII(data).ToList();
                
                return Ok(new
                {
                    ContainsPII = detections.Count > 0,
                    Detections = detections,
                    PIITypes = detections.GroupBy(d => d.Type).Select(g => new { Type = g.Key, Count = g.Count() })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting PII in data");
                return StatusCode(500, "Error detecting PII");
            }
        }

        /// <summary>
        /// Masks PII in text
        /// </summary>
        [HttpPost("mask/text")]
        public IActionResult MaskPIIInText([FromBody] TextMaskingRequest request)
        {
            try
            {
                var maskingOptions = new PIIMaskingOptions
                {
                    MaskingType = request.MaskingType
                };
                
                var maskedText = _piiDetectionService.MaskPII(request.Text, maskingOptions);
                
                return Ok(new
                {
                    OriginalText = request.Text,
                    MaskedText = maskedText
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error masking PII in text");
                return StatusCode(500, "Error masking PII");
            }
        }

        /// <summary>
        /// Masks PII in a dictionary of values
        /// </summary>
        [HttpPost("mask/data")]
        public IActionResult MaskPIIInData([FromBody] DataMaskingRequest request)
        {
            try
            {
                var maskingOptions = new PIIMaskingOptions
                {
                    MaskingType = request.MaskingType
                };
                
                var maskedData = _piiDetectionService.MaskPII(request.Data, maskingOptions);
                
                return Ok(new
                {
                    OriginalData = request.Data,
                    MaskedData = maskedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error masking PII in data");
                return StatusCode(500, "Error masking PII");
            }
        }

        /// <summary>
        /// Scans data for PII and returns a comprehensive report
        /// </summary>
        [HttpPost("scan")]
        [Authorize(Roles = "Admin,DataSteward")]
        public IActionResult ScanForPII([FromBody] Dictionary<string, object> data)
        {
            try
            {
                var detections = _piiDetectionService.DetectPII(data).ToList();
                var maskedData = _piiDetectionService.MaskPII(data);
                
                var result = new PIIScanResult
                {
                    Detections = detections,
                    OriginalData = data,
                    MaskedData = maskedData
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for PII");
                return StatusCode(500, "Error scanning for PII");
            }
        }
    }

    /// <summary>
    /// Request model for text PII detection
    /// </summary>
    public class TextDetectionRequest
    {
        public string Text { get; set; }
    }

    /// <summary>
    /// Request model for text PII masking
    /// </summary>
    public class TextMaskingRequest
    {
        public string Text { get; set; }
        public PIIMaskingType MaskingType { get; set; } = PIIMaskingType.PartialMask;
    }

    /// <summary>
    /// Request model for data PII masking
    /// </summary>
    public class DataMaskingRequest
    {
        public Dictionary<string, object> Data { get; set; }
        public PIIMaskingType MaskingType { get; set; } = PIIMaskingType.PartialMask;
    }
}
