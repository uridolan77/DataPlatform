using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace GenericDataPlatform.IngestionService.Connectors.Database
{
    public class MySqlConnector : BaseDatabaseConnector
    {
        public MySqlConnector(ILogger<MySqlConnector> logger) : base(logger)
        {
        }

        protected override IDbConnection CreateConnection(DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("connectionString", out var connectionString))
            {
                // Try to build connection string from individual properties
                var builder = new MySqlConnectionStringBuilder();
                
                if (source.ConnectionProperties.TryGetValue("server", out var server))
                {
                    builder.Server = server;
                }
                
                if (source.ConnectionProperties.TryGetValue("port", out var port) && int.TryParse(port, out var portNumber))
                {
                    builder.Port = (uint)portNumber;
                }
                
                if (source.ConnectionProperties.TryGetValue("database", out var database))
                {
                    builder.Database = database;
                }
                
                if (source.ConnectionProperties.TryGetValue("username", out var username))
                {
                    builder.UserID = username;
                }
                
                if (source.ConnectionProperties.TryGetValue("password", out var password))
                {
                    builder.Password = password;
                }
                
                if (source.ConnectionProperties.TryGetValue("sslMode", out var sslMode))
                {
                    builder.SslMode = Enum.Parse<MySqlSslMode>(sslMode, true);
                }
                
                connectionString = builder.ConnectionString;
            }
            
            return new MySqlConnection(connectionString);
        }

        protected override string GetFullTableName(string table, string schema)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return $"`{table}`";
            }
            
            return $"`{schema}`.`{table}`";
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
                query += $" LIMIT {limit}";
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
            string databaseName = null;
            string tableNameOnly = tableName;
            
            if (tableName.Contains("."))
            {
                var parts = tableName.Split('.');
                databaseName = parts[0].Trim('`');
                tableNameOnly = parts[1].Trim('`');
            }
            else
            {
                tableNameOnly = tableName.Trim('`');
            }
            
            // If database name is not specified, use the current database
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = connection.Database;
            }
            
            // Query to get column information
            var query = @"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE,
                    IS_NULLABLE,
                    COLUMN_COMMENT
                FROM 
                    INFORMATION_SCHEMA.COLUMNS
                WHERE 
                    TABLE_SCHEMA = @DatabaseName AND TABLE_NAME = @TableName
                ORDER BY 
                    ORDINAL_POSITION";
            
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = "@DatabaseName";
            dbParam.Value = databaseName;
            command.Parameters.Add(dbParam);
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@TableName";
            tableParam.Value = tableNameOnly;
            command.Parameters.Add(tableParam);
            
            using var reader = await ExecuteReaderAsync(command);
            
            while (await reader.ReadAsync())
            {
                var columnName = reader["COLUMN_NAME"].ToString();
                var dataType = reader["DATA_TYPE"].ToString();
                var maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value ? 
                    Convert.ToInt64(reader["CHARACTER_MAXIMUM_LENGTH"]) : 0;
                var precision = reader["NUMERIC_PRECISION"] != DBNull.Value ? 
                    Convert.ToInt32(reader["NUMERIC_PRECISION"]) : 0;
                var scale = reader["NUMERIC_SCALE"] != DBNull.Value ? 
                    Convert.ToInt32(reader["NUMERIC_SCALE"]) : 0;
                var isNullable = reader["IS_NULLABLE"].ToString() == "YES";
                var comment = reader["COLUMN_COMMENT"].ToString();
                
                var field = new SchemaField
                {
                    Name = columnName,
                    Description = string.IsNullOrEmpty(comment) ? $"Column {columnName}" : comment,
                    IsRequired = !isNullable,
                    Type = MapMySqlTypeToFieldType(dataType),
                    Validation = CreateValidationRules(dataType, maxLength, precision, scale)
                };
                
                schema.Fields.Add(field);
            }
            
            return schema;
        }
        
        private FieldType MapMySqlTypeToFieldType(string sqlType)
        {
            switch (sqlType.ToLowerInvariant())
            {
                case "char":
                case "varchar":
                case "text":
                case "tinytext":
                case "mediumtext":
                case "longtext":
                case "enum":
                case "set":
                    return FieldType.String;
                
                case "tinyint":
                    return FieldType.Boolean; // Assuming tinyint(1) is used for boolean
                
                case "smallint":
                case "mediumint":
                case "int":
                case "bigint":
                    return FieldType.Integer;
                
                case "decimal":
                case "numeric":
                case "float":
                case "double":
                    return FieldType.Decimal;
                
                case "date":
                case "datetime":
                case "timestamp":
                case "time":
                case "year":
                    return FieldType.DateTime;
                
                case "json":
                    return FieldType.Json;
                
                case "binary":
                case "varbinary":
                case "blob":
                case "tinyblob":
                case "mediumblob":
                case "longblob":
                    return FieldType.Binary;
                
                case "geometry":
                case "point":
                case "linestring":
                case "polygon":
                case "multipoint":
                case "multilinestring":
                case "multipolygon":
                case "geometrycollection":
                    return FieldType.Geometry;
                
                default:
                    return FieldType.String;
            }
        }
        
        private ValidationRules CreateValidationRules(string sqlType, long maxLength, int precision, int scale)
        {
            var rules = new ValidationRules();
            
            switch (sqlType.ToLowerInvariant())
            {
                case "char":
                case "varchar":
                    if (maxLength > 0)
                    {
                        rules.MaxLength = (int)maxLength;
                    }
                    break;
                
                case "decimal":
                case "numeric":
                    if (precision > 0)
                    {
                        rules.Precision = precision;
                        rules.Scale = scale;
                    }
                    break;
            }
            
            return rules;
        }
        
        protected override async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            if (command is MySqlCommand mySqlCommand)
            {
                return await mySqlCommand.ExecuteReaderAsync();
            }
            
            return await base.ExecuteReaderAsync(command);
        }
    }
}
