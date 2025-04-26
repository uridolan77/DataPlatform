using System.Threading.Tasks;

namespace GenericDataPlatform.IngestionService.Checkpoints
{
    /// <summary>
    /// Interface for checkpoint storage
    /// </summary>
    public interface ICheckpointStorage
    {
        /// <summary>
        /// Gets a value by key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value, or null if not found</returns>
        Task<string> GetValueAsync(string key);

        /// <summary>
        /// Sets a value by key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        Task SetValueAsync(string key, string value);
    }
}
