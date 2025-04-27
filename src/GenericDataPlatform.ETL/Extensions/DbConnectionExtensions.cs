using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GenericDataPlatform.ETL.Extensions
{
    /// <summary>
    /// Extension methods for IDbConnection
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// Opens a database connection asynchronously
        /// </summary>
        public static Task OpenAsync(this IDbConnection connection)
        {
            return OpenAsync(connection, CancellationToken.None);
        }
        
        /// <summary>
        /// Opens a database connection asynchronously with cancellation support
        /// </summary>
        public static Task OpenAsync(this IDbConnection connection, CancellationToken cancellationToken)
        {
            // Check if the connection is already open
            if (connection.State == ConnectionState.Open)
            {
                return Task.CompletedTask;
            }
            
            // Open the connection synchronously but wrap it in a task
            return Task.Run(() =>
            {
                connection.Open();
            }, cancellationToken);
        }
        
        /// <summary>
        /// Closes a database connection asynchronously
        /// </summary>
        public static Task CloseAsync(this IDbConnection connection)
        {
            return CloseAsync(connection, CancellationToken.None);
        }
        
        /// <summary>
        /// Closes a database connection asynchronously with cancellation support
        /// </summary>
        public static Task CloseAsync(this IDbConnection connection, CancellationToken cancellationToken)
        {
            // Check if the connection is already closed
            if (connection.State == ConnectionState.Closed)
            {
                return Task.CompletedTask;
            }
            
            // Close the connection synchronously but wrap it in a task
            return Task.Run(() =>
            {
                connection.Close();
            }, cancellationToken);
        }
    }
}
