using System;
using Polly;

namespace GenericDataPlatform.Common.Resilience
{
    /// <summary>
    /// Extension methods for Polly Context
    /// </summary>
    public static class ContextExtensions
    {
        /// <summary>
        /// Gets a value from the context, or returns the default if not found
        /// </summary>
        public static T GetOrDefault<T>(this Context context, string key, T defaultValue = default)
        {
            if (context == null)
            {
                return defaultValue;
            }

            if (context.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a string value from the context, or returns the default if not found
        /// </summary>
        public static string GetOrDefault(this Context context, string key, string defaultValue = "")
        {
            if (context == null)
            {
                return defaultValue;
            }

            if (context.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }

            return defaultValue;
        }
    }
}