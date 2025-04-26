using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.DataCatalog;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Interface for the catalog service
    /// </summary>
    public interface ICatalogService
    {
        /// <summary>
        /// Adds a data asset to the catalog
        /// </summary>
        Task<string> AddDataAssetAsync(DataAsset asset);
        
        /// <summary>
        /// Updates a data asset in the catalog
        /// </summary>
        Task<bool> UpdateDataAssetAsync(DataAsset asset);
        
        /// <summary>
        /// Gets a data asset by ID
        /// </summary>
        Task<DataAsset> GetDataAssetAsync(string id);
        
        /// <summary>
        /// Searches for data assets by name
        /// </summary>
        Task<List<DataAsset>> SearchDataAssetsByNameAsync(string name, int limit = 100);
        
        /// <summary>
        /// Searches for data assets by tag
        /// </summary>
        Task<List<DataAsset>> SearchDataAssetsByTagAsync(string tag, int limit = 100);
        
        /// <summary>
        /// Searches for data assets by type
        /// </summary>
        Task<List<DataAsset>> SearchDataAssetsByTypeAsync(string type, int limit = 100);
        
        /// <summary>
        /// Searches for data assets by owner
        /// </summary>
        Task<List<DataAsset>> SearchDataAssetsByOwnerAsync(string owner, int limit = 100);
        
        /// <summary>
        /// Adds a tag to a data asset
        /// </summary>
        Task<bool> AddTagToDataAssetAsync(string assetId, string tag);
        
        /// <summary>
        /// Removes a tag from a data asset
        /// </summary>
        Task<bool> RemoveTagFromDataAssetAsync(string assetId, string tag);
        
        /// <summary>
        /// Adds a term to the glossary
        /// </summary>
        Task<string> AddGlossaryTermAsync(GlossaryTerm term);
        
        /// <summary>
        /// Updates a term in the glossary
        /// </summary>
        Task<bool> UpdateGlossaryTermAsync(GlossaryTerm term);
        
        /// <summary>
        /// Gets a term from the glossary by ID
        /// </summary>
        Task<GlossaryTerm> GetGlossaryTermAsync(string id);
        
        /// <summary>
        /// Searches for glossary terms by name
        /// </summary>
        Task<List<GlossaryTerm>> SearchGlossaryTermsByNameAsync(string name, int limit = 100);
        
        /// <summary>
        /// Searches for glossary terms by category
        /// </summary>
        Task<List<GlossaryTerm>> SearchGlossaryTermsByCategoryAsync(string category, int limit = 100);
    }
}
