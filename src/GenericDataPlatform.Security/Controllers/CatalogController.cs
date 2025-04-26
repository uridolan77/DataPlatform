using System;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.DataCatalog;
using GenericDataPlatform.Security.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.Security.Controllers
{
    [ApiController]
    [Route("api/security/catalog")]
    [Authorize(Roles = "Admin,DataEngineer,DataScientist")]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalogService;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
        {
            _catalogService = catalogService;
            _logger = logger;
        }

        /// <summary>
        /// Adds a data asset to the catalog
        /// </summary>
        [HttpPost("assets")]
        public async Task<IActionResult> AddDataAsset([FromBody] DataAsset asset)
        {
            try
            {
                _logger.LogInformation("Adding data asset {AssetName} of type {AssetType}", asset.Name, asset.Type);
                
                var assetId = await _catalogService.AddDataAssetAsync(asset);
                
                return Ok(new { id = assetId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding data asset {AssetName}", asset.Name);
                return StatusCode(500, new { error = "Error adding data asset", detail = ex.Message });
            }
        }

        /// <summary>
        /// Updates a data asset in the catalog
        /// </summary>
        [HttpPut("assets/{id}")]
        public async Task<IActionResult> UpdateDataAsset(string id, [FromBody] DataAsset asset)
        {
            try
            {
                // Ensure ID matches
                if (asset.Id != id)
                {
                    asset.Id = id;
                }
                
                _logger.LogInformation("Updating data asset {AssetId}", id);
                
                var success = await _catalogService.UpdateDataAssetAsync(asset);
                
                if (!success)
                {
                    return NotFound(new { error = "Data asset not found" });
                }
                
                return Ok(new { id = asset.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data asset {AssetId}", id);
                return StatusCode(500, new { error = "Error updating data asset", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets a data asset by ID
        /// </summary>
        [HttpGet("assets/{id}")]
        public async Task<IActionResult> GetDataAsset(string id)
        {
            try
            {
                _logger.LogInformation("Getting data asset {AssetId}", id);
                
                var asset = await _catalogService.GetDataAssetAsync(id);
                
                if (asset == null)
                {
                    return NotFound(new { error = "Data asset not found" });
                }
                
                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data asset {AssetId}", id);
                return StatusCode(500, new { error = "Error getting data asset", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for data assets by name
        /// </summary>
        [HttpGet("assets/search/name")]
        public async Task<IActionResult> SearchDataAssetsByName([FromQuery] string name, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for data assets by name {Name}", name);
                
                var assets = await _catalogService.SearchDataAssetsByNameAsync(name, limit);
                
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by name {Name}", name);
                return StatusCode(500, new { error = "Error searching for data assets", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for data assets by tag
        /// </summary>
        [HttpGet("assets/search/tag")]
        public async Task<IActionResult> SearchDataAssetsByTag([FromQuery] string tag, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for data assets by tag {Tag}", tag);
                
                var assets = await _catalogService.SearchDataAssetsByTagAsync(tag, limit);
                
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by tag {Tag}", tag);
                return StatusCode(500, new { error = "Error searching for data assets", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for data assets by type
        /// </summary>
        [HttpGet("assets/search/type")]
        public async Task<IActionResult> SearchDataAssetsByType([FromQuery] string type, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for data assets by type {Type}", type);
                
                var assets = await _catalogService.SearchDataAssetsByTypeAsync(type, limit);
                
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by type {Type}", type);
                return StatusCode(500, new { error = "Error searching for data assets", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for data assets by owner
        /// </summary>
        [HttpGet("assets/search/owner")]
        public async Task<IActionResult> SearchDataAssetsByOwner([FromQuery] string owner, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for data assets by owner {Owner}", owner);
                
                var assets = await _catalogService.SearchDataAssetsByOwnerAsync(owner, limit);
                
                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by owner {Owner}", owner);
                return StatusCode(500, new { error = "Error searching for data assets", detail = ex.Message });
            }
        }

        /// <summary>
        /// Adds a tag to a data asset
        /// </summary>
        [HttpPost("assets/{id}/tags")]
        public async Task<IActionResult> AddTagToDataAsset(string id, [FromBody] TagRequest request)
        {
            try
            {
                _logger.LogInformation("Adding tag {Tag} to data asset {AssetId}", request.Tag, id);
                
                var success = await _catalogService.AddTagToDataAssetAsync(id, request.Tag);
                
                if (!success)
                {
                    return NotFound(new { error = "Data asset not found" });
                }
                
                return Ok(new { id = id, tag = request.Tag });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag {Tag} to data asset {AssetId}", request.Tag, id);
                return StatusCode(500, new { error = "Error adding tag to data asset", detail = ex.Message });
            }
        }

        /// <summary>
        /// Removes a tag from a data asset
        /// </summary>
        [HttpDelete("assets/{id}/tags/{tag}")]
        public async Task<IActionResult> RemoveTagFromDataAsset(string id, string tag)
        {
            try
            {
                _logger.LogInformation("Removing tag {Tag} from data asset {AssetId}", tag, id);
                
                var success = await _catalogService.RemoveTagFromDataAssetAsync(id, tag);
                
                if (!success)
                {
                    return NotFound(new { error = "Data asset or tag not found" });
                }
                
                return Ok(new { id = id, tag = tag });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {Tag} from data asset {AssetId}", tag, id);
                return StatusCode(500, new { error = "Error removing tag from data asset", detail = ex.Message });
            }
        }

        /// <summary>
        /// Adds a term to the glossary
        /// </summary>
        [HttpPost("glossary/terms")]
        public async Task<IActionResult> AddGlossaryTerm([FromBody] GlossaryTerm term)
        {
            try
            {
                _logger.LogInformation("Adding glossary term {TermName}", term.Name);
                
                var termId = await _catalogService.AddGlossaryTermAsync(term);
                
                return Ok(new { id = termId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding glossary term {TermName}", term.Name);
                return StatusCode(500, new { error = "Error adding glossary term", detail = ex.Message });
            }
        }

        /// <summary>
        /// Updates a term in the glossary
        /// </summary>
        [HttpPut("glossary/terms/{id}")]
        public async Task<IActionResult> UpdateGlossaryTerm(string id, [FromBody] GlossaryTerm term)
        {
            try
            {
                // Ensure ID matches
                if (term.Id != id)
                {
                    term.Id = id;
                }
                
                _logger.LogInformation("Updating glossary term {TermId}", id);
                
                var success = await _catalogService.UpdateGlossaryTermAsync(term);
                
                if (!success)
                {
                    return NotFound(new { error = "Glossary term not found" });
                }
                
                return Ok(new { id = term.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating glossary term {TermId}", id);
                return StatusCode(500, new { error = "Error updating glossary term", detail = ex.Message });
            }
        }

        /// <summary>
        /// Gets a term from the glossary by ID
        /// </summary>
        [HttpGet("glossary/terms/{id}")]
        public async Task<IActionResult> GetGlossaryTerm(string id)
        {
            try
            {
                _logger.LogInformation("Getting glossary term {TermId}", id);
                
                var term = await _catalogService.GetGlossaryTermAsync(id);
                
                if (term == null)
                {
                    return NotFound(new { error = "Glossary term not found" });
                }
                
                return Ok(term);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting glossary term {TermId}", id);
                return StatusCode(500, new { error = "Error getting glossary term", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for glossary terms by name
        /// </summary>
        [HttpGet("glossary/terms/search/name")]
        public async Task<IActionResult> SearchGlossaryTermsByName([FromQuery] string name, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for glossary terms by name {Name}", name);
                
                var terms = await _catalogService.SearchGlossaryTermsByNameAsync(name, limit);
                
                return Ok(terms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for glossary terms by name {Name}", name);
                return StatusCode(500, new { error = "Error searching for glossary terms", detail = ex.Message });
            }
        }

        /// <summary>
        /// Searches for glossary terms by category
        /// </summary>
        [HttpGet("glossary/terms/search/category")]
        public async Task<IActionResult> SearchGlossaryTermsByCategory([FromQuery] string category, [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Searching for glossary terms by category {Category}", category);
                
                var terms = await _catalogService.SearchGlossaryTermsByCategoryAsync(category, limit);
                
                return Ok(terms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for glossary terms by category {Category}", category);
                return StatusCode(500, new { error = "Error searching for glossary terms", detail = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for adding a tag to a data asset
    /// </summary>
    public class TagRequest
    {
        public string Tag { get; set; }
    }
}
