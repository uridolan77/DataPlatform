using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GenericDataPlatform.ML.Services.Infrastructure
{
    /// <summary>
    /// Interface for storage service
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Loads data from a data source
        /// </summary>
        /// <param name="dataSourceId">ID of the data source</param>
        /// <param name="query">Optional query to filter data</param>
        /// <returns>List of data records</returns>
        Task<List<Dictionary<string, object>>> LoadDataAsync(string dataSourceId, string query = null);
        
        /// <summary>
        /// Saves data to a location
        /// </summary>
        /// <param name="data">Data to save</param>
        /// <param name="locationId">ID of the location to save to</param>
        /// <param name="path">Path within the location</param>
        /// <returns>Path to the saved data</returns>
        Task<string> SaveDataAsync(IEnumerable<Dictionary<string, object>> data, string locationId, string path);
        
        /// <summary>
        /// Loads a file from storage
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>File stream</returns>
        Task<Stream> LoadFileAsync(string path);
        
        /// <summary>
        /// Saves a file to storage
        /// </summary>
        /// <param name="stream">File stream</param>
        /// <param name="path">Path to save the file</param>
        /// <returns>Path to the saved file</returns>
        Task<string> SaveFileAsync(Stream stream, string path);
    }
}
