using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Middleware;
using Ocelot.Multiplexer;

namespace GenericDataPlatform.Gateway.Aggregators
{
    /// <summary>
    /// Aggregator for combining data summary and lineage information
    /// </summary>
    public class DataCompleteAggregator : IDefinedAggregator
    {
        private readonly ILogger<DataCompleteAggregator> _logger;

        public DataCompleteAggregator(ILogger<DataCompleteAggregator> logger)
        {
            _logger = logger;
        }

        public async Task<DownstreamResponse> Aggregate(List<HttpResponseMessage> responses)
        {
            _logger.LogInformation("Aggregating {Count} responses", responses.Count);

            // Create the aggregated response
            var aggregatedResponse = new Dictionary<string, object>();

            foreach (var response in responses)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("One of the downstream requests failed with status code {StatusCode}", response.StatusCode);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                try
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    
                    // Add all properties from the response to the aggregated response
                    foreach (var kvp in data)
                    {
                        aggregatedResponse[kvp.Key] = kvp.Value;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing response content");
                }
            }

            // Add metadata
            aggregatedResponse["_metadata"] = new
            {
                aggregated = true,
                responseCount = responses.Count,
                timestamp = System.DateTime.UtcNow
            };

            // Serialize the aggregated response
            var json = JsonConvert.SerializeObject(aggregatedResponse);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            return new DownstreamResponse(
                stringContent,
                HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>
                {
                    new KeyValuePair<string, IEnumerable<string>>("Content-Type", new[] { "application/json" })
                },
                "OK");
        }
    }
}
