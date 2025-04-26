using GenericDataPlatform.DatabaseService.Services.SchemaEvolution;

namespace GenericDataPlatform.DatabaseService
{
    public class DatabaseOptions
    {
        public string PostgresConnectionString { get; set; } = "Host=localhost;Database=genericdataplatform;Username=postgres;Password=postgres";
        public string SqlServerConnectionString { get; set; } = "Server=localhost;Database=genericdataplatform;User Id=sa;Password=P@ssw0rd;";
        public string MySqlConnectionString { get; set; } = "Server=localhost;Database=genericdataplatform;User=root;Password=password;";
        public DatabaseType DefaultDatabaseType { get; set; } = DatabaseType.PostgreSQL;
        public bool EnableAutoMigration { get; set; } = false;
        public bool RequireValidation { get; set; } = true;
        public int MaxHistoryVersions { get; set; } = 10;
    }
}
