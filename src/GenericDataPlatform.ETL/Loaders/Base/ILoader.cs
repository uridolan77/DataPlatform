using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.ETL.Loaders.Base
{
    public interface ILoader
    {
        string Type { get; }
        Task<object> LoadAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source);
    }
}
