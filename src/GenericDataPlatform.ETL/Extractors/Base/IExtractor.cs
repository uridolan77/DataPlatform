using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.ETL.Extractors.Base
{
    public interface IExtractor
    {
        string Type { get; }
        Task<object> ExtractAsync(Dictionary<string, object> configuration, DataSourceDefinition source, Dictionary<string, object> parameters = null);
    }
}
