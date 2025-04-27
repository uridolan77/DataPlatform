using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GenericDataPlatform.Common.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenericDataPlatform.IngestionService.Connectors.Database
{
    public class PostgreSqlConnector : BaseDatabaseConnector
    {
        public PostgreSqlConnector(ILogger<PostgreSqlConnector> logger) : base(logger)
        {
        }

        protected override IDbConnection CreateConnection(DataSourceDefinition source)
        {
            if (!source.ConnectionProperties.TryGetValue("connectionString", out var connectionString))
            {
                // Try to build connection string from individual properties
                var builder = new NpgsqlConnectionStringBuilder();

                if (source.ConnectionProperties.TryGetValue("host", out var host))
                {
                    builder.Host = host;
                }

                if (source.ConnectionProperties.TryGetValue("port", out var port) && int.TryParse(port, out var portNumber))
                {
                    builder.Port = portNumber;
                }

                if (source.ConnectionProperties.TryGetValue("database", out var database))
                {
                    builder.Database = database;
                }

                if (source.ConnectionProperties.TryGetValue("username", out var username))
                {
                    builder.Username = username;
                }

                if (source.ConnectionProperties.TryGetValue("password", out var password))
                {
                    builder.Password = password;
                }

                if (source.ConnectionProperties.TryGetValue("sslMode", out var sslMode))
                {
                    builder.SslMode = Enum.Parse<SslMode>(sslMode, true);
                }

                connectionString = builder.ConnectionString;
            }

            return new NpgsqlConnection(connectionString);
        }

        protected override string GetFullTableName(string table, string schema)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return $"\"{table}\"";
            }

            return $"\"{schema}\".\"{table}\"";
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
                Type = GenericDataPlatform.Common.Models.SchemaType.Strict,
                Fields = new List<SchemaField>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Parse the table name to extract schema and table
            string schemaName = "public";
            string tableNameOnly = tableName;

            if (tableName.Contains("."))
            {
                var parts = tableName.Split('.');
                schemaName = parts[0].Trim('"');
                tableNameOnly = parts[1].Trim('"');
            }
            else
            {
                tableNameOnly = tableName.Trim('"');
            }

            // Query to get column information
            var query = @"
                SELECT
                    a.attname AS column_name,
                    pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type,
                    CASE
                        WHEN a.atttypmod = -1 THEN NULL
                        WHEN a.atttypid IN (1042, 1043) THEN a.atttypmod - 4
                        ELSE NULL
                    END AS character_maximum_length,
                    CASE
                        WHEN a.atttypid IN (1700) THEN ((a.atttypmod - 4) >> 16) & 65535
                        ELSE NULL
                    END AS numeric_precision,
                    CASE
                        WHEN a.atttypid IN (1700) THEN (a.atttypmod - 4) & 65535
                        ELSE NULL
                    END AS numeric_scale,
                    CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END AS is_nullable,
                    pg_catalog.col_description(a.attrelid, a.attnum) AS column_comment
                FROM
                    pg_catalog.pg_attribute a
                JOIN
                    pg_catalog.pg_class c ON a.attrelid = c.oid
                JOIN
                    pg_catalog.pg_namespace n ON c.relnamespace = n.oid
                WHERE
                    c.relname = @TableName
                    AND n.nspname = @SchemaName
                    AND a.attnum > 0
                    AND NOT a.attisdropped
                ORDER BY
                    a.attnum";

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

            // Use synchronous Read() since IDataReader doesn't have ReadAsync
            while (reader.Read())
            {
                var columnName = reader["column_name"].ToString();
                var dataType = reader["data_type"].ToString();
                var maxLength = reader["character_maximum_length"] != DBNull.Value ?
                    Convert.ToInt32(reader["character_maximum_length"]) : 0;
                var precision = reader["numeric_precision"] != DBNull.Value ?
                    Convert.ToInt32(reader["numeric_precision"]) : 0;
                var scale = reader["numeric_scale"] != DBNull.Value ?
                    Convert.ToInt32(reader["numeric_scale"]) : 0;
                var isNullable = reader["is_nullable"].ToString() == "YES";
                var comment = reader["column_comment"] != DBNull.Value ?
                    reader["column_comment"].ToString() : string.Empty;

                var field = new SchemaField
                {
                    Name = columnName,
                    Description = string.IsNullOrEmpty(comment) ? $"Column {columnName}" : comment,
                    IsRequired = !isNullable,
                    Type = MapPostgreSqlTypeToFieldType(dataType),
                    Validation = CreateValidationRules(dataType, maxLength, precision, scale)
                };

                schema.Fields.Add(field);
            }

            return schema;
        }

        private FieldType MapPostgreSqlTypeToFieldType(string sqlType)
        {
            // Extract the base type from types like character varying(255)
            var baseType = sqlType.Split('(')[0].Trim().ToLowerInvariant();

            switch (baseType)
            {
                case "character varying":
                case "varchar":
                case "character":
                case "char":
                case "text":
                case "name":
                case "uuid":
                case "citext":
                    return FieldType.String;

                case "boolean":
                case "bool":
                    return FieldType.Boolean;

                case "smallint":
                case "integer":
                case "bigint":
                case "smallserial":
                case "serial":
                case "bigserial":
                    return FieldType.Integer;

                case "decimal":
                case "numeric":
                case "real":
                case "double precision":
                case "money":
                    return FieldType.Decimal;

                case "date":
                case "time":
                case "timestamp":
                case "timestamptz":
                case "interval":
                    return FieldType.DateTime;

                case "json":
                case "jsonb":
                    return FieldType.Json;

                case "bytea":
                    return FieldType.Binary;

                case "point":
                case "line":
                case "lseg":
                case "box":
                case "path":
                case "polygon":
                case "circle":
                case "geometry":
                case "geography":
                    return FieldType.Geometry;

                case "array":
                    return FieldType.Array;

                default:
                    // Check if it's an array type (ends with [])
                    if (sqlType.EndsWith("[]"))
                    {
                        return FieldType.Array;
                    }

                    return FieldType.String;
            }
        }

        private ValidationRules CreateValidationRules(string sqlType, int maxLength, int precision, int scale)
        {
            var rules = new ValidationRules();

            // Extract the base type from types like character varying(255)
            var baseType = sqlType.Split('(')[0].Trim().ToLowerInvariant();

            switch (baseType)
            {
                case "character varying":
                case "varchar":
                case "character":
                case "char":
                    if (maxLength > 0)
                    {
                        rules.MaxLength = maxLength;
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
            if (command is NpgsqlCommand npgsqlCommand)
            {
                return await npgsqlCommand.ExecuteReaderAsync();
            }

            return await base.ExecuteReaderAsync(command);
        }
    }
}
