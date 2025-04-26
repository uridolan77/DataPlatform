using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using GenericDataPlatform.Common.Resilience;
using GenericDataPlatform.Security.Models.DataCatalog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Implementation of the catalog service using SQL Server
    /// </summary>
    public class CatalogService : ICatalogService
    {
        private readonly string _connectionString;
        private readonly ILogger<CatalogService> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly JsonSerializerOptions _jsonOptions;

        public CatalogService(
            IOptions<SecurityOptions> options,
            ILogger<CatalogService> logger,
            IAsyncPolicy resiliencePolicy)
        {
            _connectionString = options.Value.ConnectionStrings?.SqlServer 
                ?? throw new ArgumentNullException(nameof(options.Value.ConnectionStrings.SqlServer), 
                    "SQL Server connection string is required for CatalogService");
            _logger = logger;
            _resiliencePolicy = resiliencePolicy;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Ensure database tables exist
            EnsureTablesExistAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureTablesExistAsync()
        {
            try
            {
                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Check if DataAssets table exists
                    var tableExists = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataAssets'");

                    if (tableExists == 0)
                    {
                        _logger.LogInformation("Creating catalog tables");
                        
                        // Read SQL script from embedded resource
                        var assembly = typeof(CatalogService).Assembly;
                        var resourceName = "GenericDataPlatform.Security.Database.Scripts.CreateCatalogTables.sql";
                        
                        using var stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
                        }
                        
                        using var reader = new System.IO.StreamReader(stream);
                        var sql = await reader.ReadToEndAsync();
                        
                        // Execute script
                        await connection.ExecuteAsync(sql);
                        
                        _logger.LogInformation("Catalog tables created successfully");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring catalog tables exist");
                throw;
            }
        }

        /// <summary>
        /// Adds a data asset to the catalog
        /// </summary>
        public async Task<string> AddDataAssetAsync(DataAsset asset)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    asset.Id ??= Guid.NewGuid().ToString();
                    
                    // Set timestamps
                    asset.CreatedAt = DateTime.UtcNow;
                    asset.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO DataAssets (
                            Id, Name, Description, Type, Format, Location, Owner, Steward, 
                            Tags, Schema, Metadata, QualityMetrics, SensitivityClassification, 
                            RetentionPolicy, CreatedAt, UpdatedAt, LastAccessedAt
                        )
                        VALUES (
                            @Id, @Name, @Description, @Type, @Format, @Location, @Owner, @Steward, 
                            @Tags, @Schema, @Metadata, @QualityMetrics, @SensitivityClassification, 
                            @RetentionPolicy, @CreatedAt, @UpdatedAt, @LastAccessedAt
                        )";

                    await connection.ExecuteAsync(sql, new
                    {
                        asset.Id,
                        asset.Name,
                        asset.Description,
                        asset.Type,
                        asset.Format,
                        asset.Location,
                        asset.Owner,
                        asset.Steward,
                        Tags = asset.Tags != null ? JsonSerializer.Serialize(asset.Tags, _jsonOptions) : null,
                        Schema = asset.Schema != null ? JsonSerializer.Serialize(asset.Schema, _jsonOptions) : null,
                        Metadata = asset.Metadata != null ? JsonSerializer.Serialize(asset.Metadata, _jsonOptions) : null,
                        QualityMetrics = asset.QualityMetrics != null ? JsonSerializer.Serialize(asset.QualityMetrics, _jsonOptions) : null,
                        asset.SensitivityClassification,
                        asset.RetentionPolicy,
                        asset.CreatedAt,
                        asset.UpdatedAt,
                        asset.LastAccessedAt
                    });
                    
                    _logger.LogInformation("Added data asset {AssetId} of type {AssetType}", asset.Id, asset.Type);
                    
                    return asset.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding data asset {AssetName}", asset.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates a data asset in the catalog
        /// </summary>
        public async Task<bool> UpdateDataAssetAsync(DataAsset asset)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Set updated timestamp
                    asset.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        UPDATE DataAssets
                        SET Name = @Name,
                            Description = @Description,
                            Type = @Type,
                            Format = @Format,
                            Location = @Location,
                            Owner = @Owner,
                            Steward = @Steward,
                            Tags = @Tags,
                            Schema = @Schema,
                            Metadata = @Metadata,
                            QualityMetrics = @QualityMetrics,
                            SensitivityClassification = @SensitivityClassification,
                            RetentionPolicy = @RetentionPolicy,
                            UpdatedAt = @UpdatedAt,
                            LastAccessedAt = @LastAccessedAt
                        WHERE Id = @Id";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        asset.Id,
                        asset.Name,
                        asset.Description,
                        asset.Type,
                        asset.Format,
                        asset.Location,
                        asset.Owner,
                        asset.Steward,
                        Tags = asset.Tags != null ? JsonSerializer.Serialize(asset.Tags, _jsonOptions) : null,
                        Schema = asset.Schema != null ? JsonSerializer.Serialize(asset.Schema, _jsonOptions) : null,
                        Metadata = asset.Metadata != null ? JsonSerializer.Serialize(asset.Metadata, _jsonOptions) : null,
                        QualityMetrics = asset.QualityMetrics != null ? JsonSerializer.Serialize(asset.QualityMetrics, _jsonOptions) : null,
                        asset.SensitivityClassification,
                        asset.RetentionPolicy,
                        asset.UpdatedAt,
                        asset.LastAccessedAt
                    });
                    
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Data asset {AssetId} not found for update", asset.Id);
                        return false;
                    }
                    
                    _logger.LogInformation("Updated data asset {AssetId}", asset.Id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data asset {AssetId}", asset.Id);
                return false;
            }
        }

        /// <summary>
        /// Gets a data asset by ID
        /// </summary>
        public async Task<DataAsset> GetDataAssetAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DataAssets WHERE Id = @Id";
                    var asset = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
                    
                    if (asset == null)
                    {
                        _logger.LogWarning("Data asset {AssetId} not found", id);
                        return null;
                    }
                    
                    return MapToDataAsset(asset);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data asset {AssetId}", id);
                return null;
            }
        }

        /// <summary>
        /// Searches for data assets by name
        /// </summary>
        public async Task<List<DataAsset>> SearchDataAssetsByNameAsync(string name, int limit = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM DataAssets WHERE Name LIKE @NamePattern ORDER BY Name";
                    var assets = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, NamePattern = $"%{name}%" });
                    
                    return assets.Select(MapToDataAsset).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by name {Name}", name);
                return new List<DataAsset>();
            }
        }

        /// <summary>
        /// Searches for data assets by tag
        /// </summary>
        public async Task<List<DataAsset>> SearchDataAssetsByTagAsync(string tag, int limit = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Note: This is a simple implementation that searches for the tag in the JSON string
                    // A more sophisticated implementation would use JSON functions specific to the database
                    var sql = "SELECT TOP (@Limit) * FROM DataAssets WHERE Tags LIKE @TagPattern ORDER BY Name";
                    var assets = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, TagPattern = $"%{tag}%" });
                    
                    // Filter results to ensure the tag is actually in the tags array
                    var filteredAssets = new List<DataAsset>();
                    foreach (var asset in assets)
                    {
                        var dataAsset = MapToDataAsset(asset);
                        if (dataAsset.Tags != null && dataAsset.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                        {
                            filteredAssets.Add(dataAsset);
                        }
                    }
                    
                    return filteredAssets;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by tag {Tag}", tag);
                return new List<DataAsset>();
            }
        }

        /// <summary>
        /// Searches for data assets by type
        /// </summary>
        public async Task<List<DataAsset>> SearchDataAssetsByTypeAsync(string type, int limit = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM DataAssets WHERE Type = @Type ORDER BY Name";
                    var assets = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, Type = type });
                    
                    return assets.Select(MapToDataAsset).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by type {Type}", type);
                return new List<DataAsset>();
            }
        }

        /// <summary>
        /// Searches for data assets by owner
        /// </summary>
        public async Task<List<DataAsset>> SearchDataAssetsByOwnerAsync(string owner, int limit = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM DataAssets WHERE Owner = @Owner ORDER BY Name";
                    var assets = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, Owner = owner });
                    
                    return assets.Select(MapToDataAsset).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for data assets by owner {Owner}", owner);
                return new List<DataAsset>();
            }
        }

        /// <summary>
        /// Adds a tag to a data asset
        /// </summary>
        public async Task<bool> AddTagToDataAssetAsync(string assetId, string tag)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Get the asset
                    var asset = await GetDataAssetAsync(assetId);
                    if (asset == null)
                    {
                        return false;
                    }
                    
                    // Add the tag if it doesn't already exist
                    if (asset.Tags == null)
                    {
                        asset.Tags = new List<string>();
                    }
                    
                    if (!asset.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        asset.Tags.Add(tag);
                        
                        // Update the asset
                        var sql = "UPDATE DataAssets SET Tags = @Tags, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                        var rowsAffected = await connection.ExecuteAsync(sql, new
                        {
                            Id = assetId,
                            Tags = JsonSerializer.Serialize(asset.Tags, _jsonOptions),
                            UpdatedAt = DateTime.UtcNow
                        });
                        
                        return rowsAffected > 0;
                    }
                    
                    return true; // Tag already exists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag {Tag} to data asset {AssetId}", tag, assetId);
                return false;
            }
        }

        /// <summary>
        /// Removes a tag from a data asset
        /// </summary>
        public async Task<bool> RemoveTagFromDataAssetAsync(string assetId, string tag)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    // Get the asset
                    var asset = await GetDataAssetAsync(assetId);
                    if (asset == null || asset.Tags == null)
                    {
                        return false;
                    }
                    
                    // Remove the tag if it exists
                    if (asset.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        asset.Tags.Remove(tag);
                        
                        // Update the asset
                        var sql = "UPDATE DataAssets SET Tags = @Tags, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                        var rowsAffected = await connection.ExecuteAsync(sql, new
                        {
                            Id = assetId,
                            Tags = JsonSerializer.Serialize(asset.Tags, _jsonOptions),
                            UpdatedAt = DateTime.UtcNow
                        });
                        
                        return rowsAffected > 0;
                    }
                    
                    return true; // Tag doesn't exist
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {Tag} from data asset {AssetId}", tag, assetId);
                return false;
            }
        }

        /// <summary>
        /// Adds a term to the glossary
        /// </summary>
        public async Task<string> AddGlossaryTermAsync(GlossaryTerm term)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Ensure ID is set
                    term.Id ??= Guid.NewGuid().ToString();
                    
                    // Set timestamps
                    term.CreatedAt = DateTime.UtcNow;
                    term.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        INSERT INTO GlossaryTerms (
                            Id, Name, Definition, Category, Abbreviation, Synonyms, 
                            RelatedTerms, Examples, Owner, Steward, Status, CreatedAt, UpdatedAt
                        )
                        VALUES (
                            @Id, @Name, @Definition, @Category, @Abbreviation, @Synonyms, 
                            @RelatedTerms, @Examples, @Owner, @Steward, @Status, @CreatedAt, @UpdatedAt
                        )";

                    await connection.ExecuteAsync(sql, new
                    {
                        term.Id,
                        term.Name,
                        term.Definition,
                        term.Category,
                        term.Abbreviation,
                        Synonyms = term.Synonyms != null ? JsonSerializer.Serialize(term.Synonyms, _jsonOptions) : null,
                        RelatedTerms = term.RelatedTerms != null ? JsonSerializer.Serialize(term.RelatedTerms, _jsonOptions) : null,
                        Examples = term.Examples != null ? JsonSerializer.Serialize(term.Examples, _jsonOptions) : null,
                        term.Owner,
                        term.Steward,
                        term.Status,
                        term.CreatedAt,
                        term.UpdatedAt
                    });
                    
                    _logger.LogInformation("Added glossary term {TermId} with name {TermName}", term.Id, term.Name);
                    
                    return term.Id;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding glossary term {TermName}", term.Name);
                throw;
            }
        }

        /// <summary>
        /// Updates a term in the glossary
        /// </summary>
        public async Task<bool> UpdateGlossaryTermAsync(GlossaryTerm term)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Set updated timestamp
                    term.UpdatedAt = DateTime.UtcNow;
                    
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = @"
                        UPDATE GlossaryTerms
                        SET Name = @Name,
                            Definition = @Definition,
                            Category = @Category,
                            Abbreviation = @Abbreviation,
                            Synonyms = @Synonyms,
                            RelatedTerms = @RelatedTerms,
                            Examples = @Examples,
                            Owner = @Owner,
                            Steward = @Steward,
                            Status = @Status,
                            UpdatedAt = @UpdatedAt
                        WHERE Id = @Id";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        term.Id,
                        term.Name,
                        term.Definition,
                        term.Category,
                        term.Abbreviation,
                        Synonyms = term.Synonyms != null ? JsonSerializer.Serialize(term.Synonyms, _jsonOptions) : null,
                        RelatedTerms = term.RelatedTerms != null ? JsonSerializer.Serialize(term.RelatedTerms, _jsonOptions) : null,
                        Examples = term.Examples != null ? JsonSerializer.Serialize(term.Examples, _jsonOptions) : null,
                        term.Owner,
                        term.Steward,
                        term.Status,
                        term.UpdatedAt
                    });
                    
                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Glossary term {TermId} not found for update", term.Id);
                        return false;
                    }
                    
                    _logger.LogInformation("Updated glossary term {TermId}", term.Id);
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating glossary term {TermId}", term.Id);
                return false;
            }
        }

        /// <summary>
        /// Gets a term from the glossary by ID
        /// </summary>
        public async Task<GlossaryTerm> GetGlossaryTermAsync(string id)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM GlossaryTerms WHERE Id = @Id";
                    var term = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
                    
                    if (term == null)
                    {
                        _logger.LogWarning("Glossary term {TermId} not found", id);
                        return null;
                    }
                    
                    return MapToGlossaryTerm(term);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting glossary term {TermId}", id);
                return null;
            }
        }

        /// <summary>
        /// Searches for glossary terms by name
        /// </summary>
        public async Task<List<GlossaryTerm>> SearchGlossaryTermsByNameAsync(string name, int limit = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM GlossaryTerms WHERE Name LIKE @NamePattern ORDER BY Name";
                    var terms = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, NamePattern = $"%{name}%" });
                    
                    return terms.Select(MapToGlossaryTerm).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for glossary terms by name {Name}", name);
                return new List<GlossaryTerm>();
            }
        }

        /// <summary>
        /// Searches for glossary terms by category
        /// </summary>
        public async Task<List<GlossaryTerm>> SearchGlossaryTermsByCategoryAsync(string category, int limit = 100)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT TOP (@Limit) * FROM GlossaryTerms WHERE Category = @Category ORDER BY Name";
                    var terms = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, Category = category });
                    
                    return terms.Select(MapToGlossaryTerm).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for glossary terms by category {Category}", category);
                return new List<GlossaryTerm>();
            }
        }

        #region Helper Methods

        private DataAsset MapToDataAsset(dynamic asset)
        {
            var dataAsset = new DataAsset
            {
                Id = asset.Id,
                Name = asset.Name,
                Description = asset.Description,
                Type = asset.Type,
                Format = asset.Format,
                Location = asset.Location,
                Owner = asset.Owner,
                Steward = asset.Steward,
                SensitivityClassification = asset.SensitivityClassification,
                RetentionPolicy = asset.RetentionPolicy,
                CreatedAt = asset.CreatedAt,
                UpdatedAt = asset.UpdatedAt,
                LastAccessedAt = asset.LastAccessedAt
            };
            
            // Deserialize Tags
            if (!string.IsNullOrEmpty(asset.Tags))
            {
                dataAsset.Tags = JsonSerializer.Deserialize<List<string>>(asset.Tags, _jsonOptions);
            }
            
            // Deserialize Schema
            if (!string.IsNullOrEmpty(asset.Schema))
            {
                dataAsset.Schema = JsonSerializer.Deserialize<List<DataField>>(asset.Schema, _jsonOptions);
            }
            
            // Deserialize Metadata
            if (!string.IsNullOrEmpty(asset.Metadata))
            {
                dataAsset.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(asset.Metadata, _jsonOptions);
            }
            
            // Deserialize QualityMetrics
            if (!string.IsNullOrEmpty(asset.QualityMetrics))
            {
                dataAsset.QualityMetrics = JsonSerializer.Deserialize<Dictionary<string, double>>(asset.QualityMetrics, _jsonOptions);
            }
            
            return dataAsset;
        }

        private GlossaryTerm MapToGlossaryTerm(dynamic term)
        {
            var glossaryTerm = new GlossaryTerm
            {
                Id = term.Id,
                Name = term.Name,
                Definition = term.Definition,
                Category = term.Category,
                Abbreviation = term.Abbreviation,
                Owner = term.Owner,
                Steward = term.Steward,
                Status = term.Status,
                CreatedAt = term.CreatedAt,
                UpdatedAt = term.UpdatedAt
            };
            
            // Deserialize Synonyms
            if (!string.IsNullOrEmpty(term.Synonyms))
            {
                glossaryTerm.Synonyms = JsonSerializer.Deserialize<List<string>>(term.Synonyms, _jsonOptions);
            }
            
            // Deserialize RelatedTerms
            if (!string.IsNullOrEmpty(term.RelatedTerms))
            {
                glossaryTerm.RelatedTerms = JsonSerializer.Deserialize<List<string>>(term.RelatedTerms, _jsonOptions);
            }
            
            // Deserialize Examples
            if (!string.IsNullOrEmpty(term.Examples))
            {
                glossaryTerm.Examples = JsonSerializer.Deserialize<List<string>>(term.Examples, _jsonOptions);
            }
            
            return glossaryTerm;
        }

        #endregion
    }
}
