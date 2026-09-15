using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Database;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    /// <summary>
    /// Centralized store for database connection configurations.
    /// Contains server/database metadata for each tenant/environment combination.
    ///
    /// Connection string resolution on Linux/Docker:
    /// 1. First checks for DB_CONNECTIONSTRING_{ENVIRONMENT} environment variable
    ///    (e.g., DB_CONNECTIONSTRING_QA with {database} placeholder)
    /// 2. Falls back to hardcoded credentials if env var not set
    ///
    /// Windows always uses SSPI (Integrated Security).
    /// </summary>
    public static class DatabaseConnectionsDataStore
    {
        /// <summary>
        /// Database connections keyed by (Tenant, Environment).
        /// Server and Database are always required.
        /// UserId/Password are fallback for Linux when environment variable is not set.
        ///
        /// For Kubernetes/CI: Set DB_CONNECTIONSTRING_QA or DB_CONNECTIONSTRING_UAT
        /// environment variables instead of using hardcoded credentials.
        /// </summary>
        public static readonly Dictionary<(Tenant, Environment), Dictionary<DatabaseType, DatabaseConnectionInfo>> Connections = new()
        {
            // BOLTAG QA Environment
            [(Tenant.BOLTAG, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_BOLTAG"
                },
                [DatabaseType.AuditDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Audit_BOLTAG"
                },
                [DatabaseType.PaymentDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "PaymentExpand_BOLTAG"
                },
            },

            // BOLTAG UAT Environment
            [(Tenant.BOLTAG, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_BOLTAG"
                },
                [DatabaseType.AuditDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Audit_BOLTAG"
                },
                [DatabaseType.PaymentDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "PaymentExpand_BOLTAG"
                },
            },

            // BOLTAG Production Environment
            [(Tenant.BOLTAG, Environment.Production)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_BOLTAG"
                },
            },

            // BOLTACCESS QA Environment
            [(Tenant.BOLTACCESS, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_boltaccess"
                },
            },

            // BOLTACCESS UAT Environment
            [(Tenant.BOLTACCESS, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_boltaccess"
                },
            },

            // KRAFTLAKEX QA Environment
            [(Tenant.KRAFTLAKEX, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_KRAFTLAKEX"
                },
            },

            // KRAFTLAKEX UAT Environment
            [(Tenant.KRAFTLAKEX, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_KRAFTLAKEX"
                },
            },

            // LIBERTYX QA Environment
            [(Tenant.LIBERTYX, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_LIBERTYX"
                },
            },

            // LIBERTYX UAT Environment
            [(Tenant.LIBERTYX, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_LIBERTYX"
                },
            },

            // UNIFY QA Environment
            [(Tenant.UNIFY, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_unify"
                },
            },

            // UNIFY UAT Environment
            [(Tenant.UNIFY, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_unify"
                },
            },

            // USAA QA Environment
            [(Tenant.USAA, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_USAA"
                },
            },

            // USAA UAT Environment
            [(Tenant.USAA, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_USAA"
                },
            },

            // USAA production Environment
            [(Tenant.USAA, Environment.Production)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "",
                    Database = "Espresso_USAA"
                },
            },
            // COMPARION QA Environment
            [(Tenant.COMPARION, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Espresso_COMPARION"
                },
            },
            // COMPARION QA Environment
            [(Tenant.COMPARION, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Espresso_COMPARION"
                },
            },

            // PROGRESSIVEPL QA Environment
            [(Tenant.PROGRESSIVEPL, Environment.Qa)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.qa.boltx.us",
                    Database = "Epos_ProgressivePL"
                },
            },
            // PROGRESSIVEPL UAT Environment
            [(Tenant.PROGRESSIVEPL, Environment.Uat)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "mssql.uat.boltx.us",
                    Database = "Epos_ProgressivePL"
                },
            },
            // PROGRESSIVEPL UAT Environment
            [(Tenant.PROGRESSIVEPL, Environment.Production)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "",
                    Database = ""
                },
            },

            // BOLTAG Staging Environment
            [(Tenant.BOLTAG, Environment.Staging)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "STGDB01",
                    Database = "Espresso_boltag"
                },
                [DatabaseType.PaymentDB] = new DatabaseConnectionInfo
                {
                    Server = "STGDB01",
                    Database = "PaymentExpand_BOLTAG"
                },
            },

            // KRAFTLAKEX Staging Environment
            [(Tenant.KRAFTLAKEX, Environment.Staging)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "STGDB01",
                    Database = "Espresso_KRAFTLAKEX"
                },
            },

            // PROGRESSIVEPL Staging Environment
            // Password intentionally absent — sourced from BOLT_SECRETS_PATH bundle at runtime.
            // ToConnectionString() throws with a clear message when the bundle is not mounted.
            [(Tenant.PROGRESSIVEPL, Environment.Staging)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "stgdb01.ciosus.com",
                    Database = "Epos",
                    SupportsSspi = false,
                    UserId = "boltautomation",
                },
            },

            // UNIFY Staging Environment
            [(Tenant.UNIFY, Environment.Staging)] = new Dictionary<DatabaseType, DatabaseConnectionInfo>
            {
                [DatabaseType.MainDB] = new DatabaseConnectionInfo
                {
                    Server = "STGDB01",
                    Database = "Espresso_unify"
                },
                [DatabaseType.PaymentDB] = new DatabaseConnectionInfo
                {
                    Server = "STGDB01",
                    Database = "PaymentExpand_UNIFY"
                },
            },
        };

        /// <summary>
        /// Resolves connection string for the specified tenant/environment/database type.
        /// On Linux: checks DB_CONNECTIONSTRING_{ENVIRONMENT} env var first, then falls back.
        /// On Windows: uses SSPI (Integrated Security).
        /// </summary>
        public static string ResolveConnectionString(Tenant tenant, Environment environment, DatabaseType dbType)
        {
            if (!Connections.TryGetValue((tenant, environment), out var dbMap))
            {
                throw new KeyNotFoundException($"No database configuration found for {tenant}/{environment}.");
            }

            if (!dbMap.TryGetValue(dbType, out var info))
            {
                throw new KeyNotFoundException($"No {dbType} configuration found for {tenant}/{environment}.");
            }

            return info.ToConnectionString(environment);
        }
    }
}
