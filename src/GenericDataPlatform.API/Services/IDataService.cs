using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.API.Models;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.API.Services
{
    public interface IDataService
    {
        Task<IEnumerable<DataSourceDefinition>> GetDataSourcesAsync();
        Task<DataSourceDefinition> GetDataSourceAsync(string id);
        Task<DataSourceDefinition> CreateDataSourceAsync(DataSourceDefinition source);
        Task<DataSourceDefinition> UpdateDataSourceAsync(DataSourceDefinition source);
        Task<bool> DeleteDataSourceAsync(string id);
        
        Task<DataSchema> GetSchemaAsync(string sourceId);
        Task<DataSchema> UpdateSchemaAsync(string sourceId, DataSchema schema);
        
        Task<PagedResult<DataRecord>> GetRecordsAsync(string sourceId, Dictionary<string, string> filters = null, int page = 1, int pageSize = 50);
        Task<DataRecord> GetRecordAsync(string id);
        Task<string> CreateRecordAsync(string sourceId, DataRecord record);
        Task<bool> UpdateRecordAsync(DataRecord record);
        Task<bool> DeleteRecordAsync(string id);
        
        Task<QueryResult> QueryAsync(DataQuery query);
    }
}
