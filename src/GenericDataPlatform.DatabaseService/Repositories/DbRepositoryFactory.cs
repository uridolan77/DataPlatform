using System;
using GenericDataPlatform.DatabaseService.Services.SchemaEvolution;
using Microsoft.Extensions.DependencyInjection;

namespace GenericDataPlatform.DatabaseService.Repositories
{
    public class DbRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;
        
        public DbRepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public IDbRepository CreateRepository(DatabaseType databaseType)
        {
            switch (databaseType)
            {
                case DatabaseType.PostgreSQL:
                    return _serviceProvider.GetRequiredService<PostgresRepository>();
                
                case DatabaseType.SQLServer:
                    return _serviceProvider.GetRequiredService<SqlServerRepository>();
                
                case DatabaseType.MySQL:
                    return _serviceProvider.GetRequiredService<MySqlRepository>();
                
                default:
                    throw new ArgumentException($"Unsupported database type: {databaseType}");
            }
        }
    }
}
