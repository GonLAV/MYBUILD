using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Services.Secrets;

namespace Bolt.Automation.Common.Models.Database
{
    /// <summary>
    /// Collection of database connections that automatically resolves connection strings
    /// based on the current operating system and secrets configuration.
    ///
    /// Connection string resolution priority:
    /// 1. Secrets file (if BOLT_SECRETS_PATH is set)
    /// 2. Windows SSPI (Integrated Security)
    /// 3. Fallback to UserId/Password from DatabaseConnectionInfo
    /// </summary>
    public class DatabaseTestDataCollection
    {
        private readonly Dictionary<DatabaseType, DatabaseConnectionInfo> _connections;
        private readonly Environment? _environment;
        private readonly Tenant? _tenant;

        /// <summary>
        /// Creates a new DatabaseTestDataCollection.
        /// </summary>
        /// <param name="connections">Dictionary of database connections by type</param>
        /// <param name="environment">Environment for connection string resolution</param>
        /// <param name="tenant">Tenant for secrets file lookup</param>
        public DatabaseTestDataCollection(
            Dictionary<DatabaseType, DatabaseConnectionInfo> connections,
            Environment? environment = null,
            Tenant? tenant = null)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
            _environment = environment;
            _tenant = tenant;
        }

        /// <summary>
        /// Gets the connection string for the specified database type.
        /// Priority: Secrets file → SSPI (Windows) → UserId/Password fallback
        /// </summary>
        public string this[DatabaseType dbType]
        {
            get
            {
                // Priority 1: Try secrets file if available (BOLT_SECRETS_PATH is set)
                if (_environment.HasValue && _tenant.HasValue && SecretsStore.Instance.IsLoaded)
                {
                    var secretsConnectionString = SecretsStore.Instance
                        .GetDatabaseConnectionString(_tenant.Value, _environment.Value, dbType);

                    if (!string.IsNullOrWhiteSpace(secretsConnectionString))
                    {
                        return secretsConnectionString;
                    }
                }

                // Priority 2/3: Fall back to DatabaseConnectionInfo resolution (SSPI or UserId/Password)
                if (!_connections.TryGetValue(dbType, out var info))
                {
                    throw new KeyNotFoundException($"No connection configuration found for database type '{dbType}'.");
                }
                return info.ToConnectionString(_environment);
            }
        }

        /// <summary>
        /// Gets the raw connection info for inspection or testing purposes.
        /// </summary>
        public DatabaseConnectionInfo GetConnectionInfo(DatabaseType dbType)
        {
            if (!_connections.TryGetValue(dbType, out var info))
            {
                throw new KeyNotFoundException($"No connection configuration found for database type '{dbType}'.");
            }
            return info;
        }

        /// <summary>
        /// Checks if a connection configuration exists for the specified database type.
        /// </summary>
        public bool HasConnection(DatabaseType dbType) => _connections.ContainsKey(dbType);
    }
}
