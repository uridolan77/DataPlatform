using Microsoft.AspNetCore.Builder;

namespace GenericDataPlatform.IngestionService.Middleware
{
    /// <summary>
    /// Extension methods for registering custom middleware
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds the global exception handling middleware to the application pipeline
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
