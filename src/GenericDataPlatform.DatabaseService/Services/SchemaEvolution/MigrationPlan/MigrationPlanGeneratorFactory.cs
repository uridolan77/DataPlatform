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
                    // Not implemented yet
                    throw new NotImplementedException($"Migration plan generator for {databaseType} is not implemented yet");

                case DatabaseType.SQLite:
                    // Not implemented yet
                    throw new NotImplementedException($"Migration plan generator for {databaseType} is not implemented yet");

                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }
        }
    }
}
