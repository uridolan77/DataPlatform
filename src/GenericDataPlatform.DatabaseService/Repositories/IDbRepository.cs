using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.DatabaseService.Repositories
{
    public interface IDbRepository
    {
        Task<IEnumerable<DataRecord>> GetRecordsAsync(string sourceId, Dictionary<string, string> filters = null, int page = 1, int pageSize = 50);
        Task<DataRecord> GetRecordAsync(string id);
        Task<string> InsertRecordAsync(DataRecord record);
        Task<bool> UpdateRecordAsync(DataRecord record);
        Task<bool> DeleteRecordAsync(string id);
        Task<long> CountRecordsAsync(string sourceId, Dictionary<string, string> filters = null);
        Task<IEnumerable<DataRecord>> QueryAsync(string query, Dictionary<string, object> parameters = null);
        Task<bool> CreateTableAsync(string sourceId, DataSchema schema);
        Task<bool> UpdateTableAsync(string sourceId, DataSchema schema);
        Task<bool> DeleteTableAsync(string sourceId);
    }
}
