using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.IngestionService.Connectors.Database
{
    public class SqlServerConnector : BaseDatabaseConnector
    {
        public SqlServerConnector(ILogger<SqlServerConnector> logger) : base(logger)
        {
        }

        protected override IDbConnection CreateConnection(DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("connectionString", out var connectionString))
            {
                // Try to build connection string from individual properties
                var builder = new SqlConnectionStringBuilder();
                
                if (source.ConnectionProperties.TryGetValue("server", out var server))
                {
                    builder.DataSource = server;
                }
                
                if (source.ConnectionProperties.TryGetValue("database", out var database))
                {
                    builder.InitialCatalog = database;
                }
                
                if (source.ConnectionProperties.TryGetValue("username", out var username) && 
                    source.ConnectionProperties.TryGetValue("password", out var password))
                {
                    builder.UserID = username;
                    builder.Password = password;
                }
                else
                {
                    builder.IntegratedSecurity = true;
                }
                
                if (source.ConnectionProperties.TryGetValue("trustServerCertificate", out var trustServerCertificate) && 
                    bool.TryParse(trustServerCertificate, out var trustServerCertificateBool))
                {
                    builder.TrustServerCertificate = trustServerCertificateBool;
                }
                
                connectionString = builder.ConnectionString;
            }
            
            return new SqlConnection(connectionString);
        }

        protected override string GetFullTableName(string table, string schema)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return $"[{table}]";
            }
            
            return $"[{schema}].[{table}]";
        }

        protected override string BuildSelectQuery(string tableName, string whereClause, int limit)
        {
            var query = $"SELECT * FROM {tableName}";
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                query += $" {whereClause}";
            }
            
            if (limit > 0)
            {
                query += $" ORDER BY (SELECT NULL) OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY";
            }
            
            return query;
        }

        protected override async Task<DataSchema> GetTableSchemaAsync(IDbConnection connection, string tableName, DataSourceDefinition source)
        {
            var schema = new DataSchema
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{source.Name} Schema",
                Description = $"Schema for {source.Name}",
                Type = SchemaType.Strict,
                Fields = new List<SchemaField>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Parse the table name to extract schema and table
            string schemaName = "dbo";
            string tableNameOnly = tableName;
            
            if (tableName.Contains("."))
            {
                var parts = tableName.Split('.');
                schemaName = parts[0].Trim('[', ']');
                tableNameOnly = parts[1].Trim('[', ']');
            }
            else
            {
                tableNameOnly = tableName.Trim('[', ']');
            }
            
            // Query to get column information
            var query = @"
                SELECT 
                    c.name AS ColumnName,
                    t.name AS DataType,
                    c.max_length AS MaxLength,
                    c.precision AS Precision,
                    c.scale AS Scale,
                    c.is_nullable AS IsNullable,
                    ISNULL(ep.value, '') AS Description
                FROM 
                    sys.columns c
                INNER JOIN 
                    sys.types t ON c.user_type_id = t.user_type_id
                INNER JOIN 
                    sys.tables tbl ON c.object_id = tbl.object_id
                INNER JOIN 
                    sys.schemas s ON tbl.schema_id = s.schema_id
                LEFT JOIN 
                    sys.extended_properties ep ON c.object_id = ep.major_id AND c.column_id = ep.minor_id AND ep.name = 'MS_Description'
                WHERE 
                    tbl.name = @TableName AND s.name = @SchemaName
                ORDER BY 
                    c.column_id";
            
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@TableName";
            tableParam.Value = tableNameOnly;
            command.Parameters.Add(tableParam);
            
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@SchemaName";
            schemaParam.Value = schemaName;
            command.Parameters.Add(schemaParam);
            
            using var reader = await ExecuteReaderAsync(command);
            
            while (await reader.ReadAsync())
            {
                var columnName = reader["ColumnName"].ToString();
                var dataType = reader["DataType"].ToString();
                var maxLength = Convert.ToInt32(reader["MaxLength"]);
                var precision = Convert.ToByte(reader["Precision"]);
                var scale = Convert.ToByte(reader["Scale"]);
                var isNullable = Convert.ToBoolean(reader["IsNullable"]);
                var description = reader["Description"].ToString();
                
                var field = new SchemaField
                {
                    Name = columnName,
                    Description = string.IsNullOrEmpty(description) ? $"Column {columnName}" : description,
                    IsRequired = !isNullable,
                    Type = MapSqlServerTypeToFieldType(dataType, maxLength, precision, scale),
                    Validation = CreateValidationRules(dataType, maxLength, precision, scale)
                };
                
                schema.Fields.Add(field);
            }
            
            return schema;
        }
        
        private FieldType MapSqlServerTypeToFieldType(string sqlType, int maxLength, byte precision, byte scale)
        {
            switch (sqlType.ToLowerInvariant())
            {
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                case "text":
                case "ntext":
                    return FieldType.String;
                
                case "bit":
                    return FieldType.Boolean;
                
                case "tinyint":
                case "smallint":
                case "int":
                case "bigint":
                    return FieldType.Integer;
                
                case "decimal":
                case "numeric":
                case "float":
                case "real":
                case "money":
                case "smallmoney":
                    return FieldType.Decimal;
                
                case "date":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "datetimeoffset":
                case "time":
                    return FieldType.DateTime;
                
                case "xml":
                    return FieldType.Complex;
                
                case "json":
                    return FieldType.Json;
                
                case "binary":
                case "varbinary":
                case "image":
                    return FieldType.Binary;
                
                case "geography":
                case "geometry":
                    return FieldType.Geometry;
                
                default:
                    return FieldType.String;
            }
        }
        
        private ValidationRules CreateValidationRules(string sqlType, int maxLength, byte precision, byte scale)
        {
            var rules = new ValidationRules();
            
            switch (sqlType.ToLowerInvariant())
            {
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    if (maxLength != -1) // -1 means MAX
                    {
                        rules.MaxLength = maxLength;
                        if (sqlType.StartsWith("n", StringComparison.OrdinalIgnoreCase))
                        {
                            rules.MaxLength = maxLength / 2; // Unicode characters take 2 bytes
                        }
                    }
                    break;
                
                case "decimal":
                case "numeric":
                    rules.Precision = precision;
                    rules.Scale = scale;
                    break;
            }
            
            return rules;
        }
        
        protected override async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            if (command is SqlCommand sqlCommand)
            {
                return await sqlCommand.ExecuteReaderAsync();
            }
            
            return await base.ExecuteReaderAsync(command);
        }
    }
}
