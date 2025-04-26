using System.Collections.Generic;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;

namespace GenericDataPlatform.ETL.Transformers.Base
{
    public interface ITransformer
    {
        string Type { get; }
        Task<object> TransformAsync(object input, Dictionary<string, object> configuration, DataSourceDefinition source);
    }
}
