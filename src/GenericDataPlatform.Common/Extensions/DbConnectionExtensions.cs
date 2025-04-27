using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace GenericDataPlatform.Common.Extensions
{
    /// <summary>
    /// Extension methods for IDbConnection
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// Opens a database connection asynchronously
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task OpenAsync(this IDbConnection connection)
        {
            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync();
            }
            else
            {
                // Fall back to synchronous open for non-DbConnection implementations
                connection.Open();
            }
        }
    }
}
