using System.Text.Json;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Secrets;

namespace Bolt.Automation.Common.Services.Secrets
{
    /// <summary>
    /// Loads and provides access to secrets from JSON file specified by BOLT_SECRETS_PATH environment variable.
    /// Thread-safe singleton that loads secrets once on first access.
    ///
    /// Usage:
    /// - CI/CD: Set BOLT_SECRETS_PATH to path of secrets JSON file
    /// - Local Windows: Leave unset to use SSPI, or set for local Docker testing
    /// </summary>
    public class SecretsStore : ISecretsStore
    {
        private static readonly Lazy<SecretsStore> _instance = new(() => new SecretsStore());

        /// <summary>
        /// Gets the singleton instance of the SecretsStore.
        /// </summary>
        public static SecretsStore Instance => _instance.Value;

        private readonly AutomationSecretsStore? _secrets;
        private readonly bool _isLoaded;
        private readonly string? _loadError;

        private SecretsStore()
        {
            var secretsPath = System.Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH");

            Console.WriteLine($"[SecretsStore] BOLT_SECRETS_PATH = '{secretsPath ?? "(not set)"}'");

            if (string.IsNullOrWhiteSpace(secretsPath))
            {
                Console.WriteLine("[SecretsStore] No secrets path configured, will use SSPI/fallback");
                _isLoaded = false;
                return;
            }

            // If path is a directory, append the default filename
            if (Directory.Exists(secretsPath))
            {
                secretsPath = SecretsBundle.ResolvePath(secretsPath);
                Console.WriteLine($"[SecretsStore] Path was directory, using: {secretsPath}");
            }

            if (!File.Exists(secretsPath))
            {
                _loadError = $"Secrets file not found at path specified by BOLT_SECRETS_PATH: {secretsPath}";
                Console.WriteLine($"[SecretsStore] ERROR: {_loadError}");
                throw new FileNotFoundException(_loadError, secretsPath);
            }

            try
            {
                Console.WriteLine($"[SecretsStore] Loading secrets from: {secretsPath}");
                var json = File.ReadAllText(secretsPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _secrets = JsonSerializer.Deserialize<AutomationSecretsStore>(json, options);
                _isLoaded = _secrets != null;

                if (_isLoaded)
                {
                    var envCount = _secrets?.Environments?.Count ?? 0;
                    Console.WriteLine($"[SecretsStore] Successfully loaded secrets file with {envCount} environments");
                }
                else
                {
                    Console.WriteLine("[SecretsStore] WARNING: Secrets file parsed but resulted in null");
                }
            }
            catch (JsonException ex)
            {
                _loadError = $"Failed to parse secrets file at {secretsPath}: {ex.Message}";
                Console.WriteLine($"[SecretsStore] ERROR: {_loadError}");
                throw new InvalidOperationException(_loadError, ex);
            }
        }

        /// <inheritdoc />
        public bool IsLoaded => _isLoaded;

        /// <inheritdoc />
        public string? GetDatabaseConnectionString(Tenant tenant, Environment environment, DatabaseType dbType)
        {
            // Environment name in JSON is lowercase (qa, uat, dev, etc.).
            // Shared helper guarantees the same key the app-secrets provider uses.
            var envSecrets = ResolveEnvNode(environment);

            if (envSecrets?.Tenants == null)
            {
                return null;
            }

            // Tenant name in JSON is uppercase (BOLTAG, USAA, etc.)
            var tenantKey = tenant.ToString().ToUpperInvariant();

            if (!envSecrets.Tenants.TryGetValue(tenantKey, out var tenantSecrets))
            {
                return null;
            }

            // Map DatabaseType to JSON property
            var connectionString = dbType switch
            {
                DatabaseType.MainDB => tenantSecrets.PlatformSqlConnectionString,
                DatabaseType.AuditDB => tenantSecrets.AuditSqlConnectionString,
                DatabaseType.PaymentDB => tenantSecrets.PaymentSqlConnectionString,
                DatabaseType.AmsDB => null, // Not yet in secrets file
                _ => null
            };

            // Return null for empty strings to allow fallback
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            // Ensure TrustServerCertificate is set for Linux/Docker environments
            connectionString = EnsureTrustServerCertificate(connectionString);

            return connectionString;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, UserSecretFields>? GetUserSecrets(Tenant tenant, Environment environment)
        {
            var env = ResolveEnvNode(environment);
            if (env?.UserSecrets == null)
            {
                return null;
            }

            var tenantKey = tenant.ToString().ToUpperInvariant();
            return env.UserSecrets.TryGetValue(tenantKey, out var userSecrets) ? userSecrets : null;
        }

        /// <inheritdoc />
        public TwilioSecretFields? GetTwilioSecrets(Tenant tenant, Environment environment, string subtenant = "")
        {
            var env = ResolveEnvNode(environment);
            if (env?.Twilio == null)
            {
                return null;
            }

            // Default (empty) subtenant → TENANT; a diverging pair → TENANT:subtenant.
            var tenantKey = tenant.ToString().ToUpperInvariant();
            var key = string.IsNullOrEmpty(subtenant) ? tenantKey : $"{tenantKey}:{subtenant}";
            return env.Twilio.TryGetValue(key, out var twilio) ? twilio : null;
        }

        /// <summary>
        /// Resolves the environment node for the given environment using the shared key helper,
        /// or null when the bundle is not loaded / the environment is absent.
        /// </summary>
        private EnvironmentSecrets? ResolveEnvNode(Environment environment)
        {
            if (!_isLoaded || _secrets?.Environments == null)
            {
                return null;
            }

            var envKey = SecretsBundle.ResolveEnvironmentKey(environment);
            return _secrets.Environments.TryGetValue(envKey, out var envSecrets) ? envSecrets : null;
        }

        /// <summary>
        /// Ensures TrustServerCertificate=true is present in the connection string.
        /// Required for Linux/Docker environments with self-signed certificates.
        /// </summary>
        private static string EnsureTrustServerCertificate(string connectionString)
        {
            if (connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }

            // Append TrustServerCertificate=true
            var separator = connectionString.TrimEnd().EndsWith(";") ? "" : ";";
            return $"{connectionString}{separator}TrustServerCertificate=true;";
        }
    }
}
