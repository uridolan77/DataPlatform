using System.Collections.Generic;

namespace GenericDataPlatform.API.Models
{
    public class DataQuery
    {
        public string SourceId { get; set; }
        public List<string> Fields { get; set; }
        public List<FilterCondition> Filters { get; set; }
        public List<SortField> Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string GroupBy { get; set; }
        public List<AggregateField> Aggregates { get; set; }
        public string CustomQuery { get; set; }
    }

    public class FilterCondition
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public List<FilterCondition> NestedConditions { get; set; }
        public string LogicalOperator { get; set; } = "AND";
    }

    public class SortField
    {
        public string Field { get; set; }
        public bool Descending { get; set; }
    }

    public class AggregateField
    {
        public string Field { get; set; }
        public string Function { get; set; }
        public string Alias { get; set; }
    }

    public class QueryResult
    {
        public IEnumerable<Dictionary<string, object>> Records { get; set; }
        public int TotalCount { get; set; }
        public Dictionary<string, object> Aggregates { get; set; }
    }
}
