using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Secrets;

namespace Bolt.Automation.Common.Services.Secrets
{
    /// <summary>
    /// Interface for accessing secrets from external secrets store.
    /// </summary>
    public interface ISecretsStore
    {
        /// <summary>
        /// Gets the full connection string for a specific tenant, environment, and database type.
        /// Returns null if not found in secrets file.
        /// </summary>
        /// <param name="tenant">The tenant (BOLTAG, USAA, etc.)</param>
        /// <param name="environment">The environment (Qa, Uat, etc.)</param>
        /// <param name="dbType">The database type (MainDB, AuditDB, PaymentDB)</param>
        /// <returns>Full connection string or null if not found</returns>
        string? GetDatabaseConnectionString(Tenant tenant, Environment environment, DatabaseType dbType);

        /// <summary>
        /// Gets the role-keyed user credentials for a tenant + environment. The returned map is keyed
        /// by the <c>UserTestDataCollection</c> property name (ServiceAgent, Agent, ...). Returns null
        /// when the bundle is not loaded or has no userSecrets for this tenant/environment.
        /// </summary>
        IReadOnlyDictionary<string, UserSecretFields>? GetUserSecrets(Tenant tenant, Environment environment);

        /// <summary>
        /// Gets the Twilio credentials for a tenant + environment (+ optional subtenant).
        /// Returns null when the bundle is not loaded or has no Twilio entry for this key.
        /// </summary>
        TwilioSecretFields? GetTwilioSecrets(Tenant tenant, Environment environment, string subtenant = "");

        /// <summary>
        /// Indicates whether the secrets file is loaded and available.
        /// </summary>
        bool IsLoaded { get; }
    }
}
