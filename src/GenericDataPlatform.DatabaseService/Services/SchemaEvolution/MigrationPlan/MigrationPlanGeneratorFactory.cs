using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericDataPlatform.DatabaseService.Services.SchemaEvolution.MigrationPlan
{
    public class MigrationPlanGeneratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MigrationPlanGeneratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public BaseMigrationPlanGenerator CreateGenerator(DatabaseType databaseType)
        {
            var schemaComparer = _serviceProvider.GetRequiredService<SchemaComparer>();
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return new PostgreSqlMigrationPlanGenerator(
                        schemaComparer,
                        loggerFactory.CreateLogger<PostgreSqlMigrationPlanGenerator>());

                case DatabaseType.SQLServer:
                    return new SqlServerMigrationPlanGenerator(
                        schemaComparer,
                        loggerFactory.CreateLogger<SqlServerMigrationPlanGenerator>());

                case DatabaseType.MySQL:
                    return new MySqlMigrationPlanGenerator(
                        schemaComparer,
                        loggerFactory.CreateLogger<MySqlMigrationPlanGenerator>());

                case DatabaseType.Oracle:
                    return new OracleMigrationPlanGenerator(
                        schemaComparer,
                        loggerFactory.CreateLogger<OracleMigrationPlanGenerator>());

                case DatabaseType.SQLite:
                    return new SqliteMigrationPlanGenerator(
                        schemaComparer,
                        loggerFactory.CreateLogger<SqliteMigrationPlanGenerator>());

                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }
        }
    }
}
