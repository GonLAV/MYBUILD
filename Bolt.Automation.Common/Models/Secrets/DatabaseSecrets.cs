namespace Bolt.Automation.Common.Models.Secrets
{
    /// <summary>
    /// Root model for the automation secrets JSON file structure.
    /// </summary>
    public class AutomationSecretsStore
    {
        public Dictionary<string, object>? Common { get; set; }

        /// <summary>
        /// Environment-specific secrets keyed by environment name (qa, uat, production, etc.)
        /// </summary>
        public Dictionary<string, EnvironmentSecrets>? Environments { get; set; }
    }

    /// <summary>
    /// Secrets for a specific environment (qa, uat, production, etc.)
    /// </summary>
    public class EnvironmentSecrets
    {
        public Dictionary<string, object>? Common { get; set; }

        /// <summary>
        /// Tenant-specific database connection strings.
        /// Keys are tenant names in uppercase (BOLTAG, USAA, etc.)
        /// </summary>
        public Dictionary<string, TenantDatabaseSecrets>? Tenants { get; set; }

        /// <summary>
        /// Tenant + role keyed user credentials (Tier-2 migration). Structural user data stays in
        /// <c>UserDataStore</c>; only the credential fields live here. Outer key is the tenant name in
        /// uppercase (BOLTAG, USAA, ...); inner key is the <c>UserTestDataCollection</c> property name
        /// (ServiceAgent, Agent, ConsumerOrganicPL, ...).
        /// </summary>
        public Dictionary<string, Dictionary<string, UserSecretFields>>? UserSecrets { get; set; }

        /// <summary>
        /// Twilio credentials (Tier-2 migration). Keys are the tenant name in uppercase for the default
        /// (empty) subtenant, or <c>TENANT:subtenant</c> when a (tenant, subtenant) pair diverges.
        /// </summary>
        public Dictionary<string, TwilioSecretFields>? Twilio { get; set; }
    }

    /// <summary>
    /// Credential fields for a single user, moved out of <c>UserDataStore</c> into the bundle.
    /// A null field means "not in the bundle" — hydration leaves the in-code value untouched.
    /// </summary>
    public class UserSecretFields
    {
        public string? Password { get; set; }
        public string? ApiKey { get; set; }
        public string? OAuthToken { get; set; }
        public string? AgentIdentity { get; set; }
        public string? ApiSource { get; set; }
    }

    /// <summary>
    /// Credential fields for a single Twilio (tenant, environment, subtenant) entry.
    /// Structural fields (phone numbers) stay in <c>TwilioDataStore</c>.
    /// </summary>
    public class TwilioSecretFields
    {
        public string? AuthToken { get; set; }
        public string? AccountSid { get; set; }
    }

    /// <summary>
    /// Database connection strings for a specific tenant.
    /// Contains full connection strings including credentials.
    /// </summary>
    public class TenantDatabaseSecrets
    {
        /// <summary>
        /// Full connection string for the Audit database.
        /// Maps to DatabaseType.AuditDB.
        /// </summary>
        public string? AuditSqlConnectionString { get; set; }

        /// <summary>
        /// Full connection string for the Payment database.
        /// Maps to DatabaseType.PaymentDB.
        /// </summary>
        public string? PaymentSqlConnectionString { get; set; }

        /// <summary>
        /// Full connection string for the Platform/Main database.
        /// Maps to DatabaseType.MainDB.
        /// </summary>
        public string? PlatformSqlConnectionString { get; set; }
    }
}
