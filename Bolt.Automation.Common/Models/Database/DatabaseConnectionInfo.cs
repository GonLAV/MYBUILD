using System.Runtime.InteropServices;
using System.Text;

namespace Bolt.Automation.Common.Models.Database
{
    /// <summary>
    /// Represents database connection configuration that automatically resolves
    /// to the appropriate connection string format based on the operating system.
    ///
    /// Connection string resolution (handled by DatabaseTestDataCollection):
    /// 1. Secrets file (if BOLT_SECRETS_PATH is set)
    /// 2. Windows SSPI (Integrated Security) when SupportsSspi is true
    /// 3. Fallback to UserId/Password properties
    /// </summary>
    public class DatabaseConnectionInfo
    {
        public required string Server { get; init; }
        public required string Database { get; init; }

        /// <summary>
        /// Whether this connection supports Windows Integrated Security (SSPI).
        /// Default is true. Set to false to force SQL auth on all platforms.
        /// </summary>
        public bool SupportsSspi { get; init; } = true;

        /// <summary>
        /// SQL Authentication user ID. Used as fallback when secrets file is not available
        /// and SSPI is not supported.
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// SQL Authentication password. Used as fallback when secrets file is not available
        /// and SSPI is not supported.
        /// </summary>
        public string? Password { get; init; }

        public bool Encrypt { get; init; } = true;
        public bool TrustServerCertificate { get; init; } = true;

        /// <summary>
        /// Builds connection string appropriate for the current operating system.
        /// Note: This method is only called when secrets file lookup fails or is not available.
        /// Windows: uses SSPI if SupportsSspi is true
        /// Linux/Docker: requires UserId and Password properties to be set
        /// </summary>
        /// <param name="environment">Optional environment parameter (kept for API compatibility)</param>
        public string ToConnectionString(Environment? environment = null)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var useSspi = isWindows && SupportsSspi;

            // If not using SSPI, require credentials
            if (!useSspi && (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Password)))
            {
                throw new InvalidOperationException(
                    $"SQL authentication credentials required for database '{Database}'. " +
                    $"Set BOLT_SECRETS_PATH environment variable pointing to secrets JSON file, " +
                    $"or provide UserId and Password in DatabaseConnectionInfo.");
            }

            var builder = new StringBuilder();
            builder.Append($"Server={Server};Database={Database};");

            if (useSspi)
            {
                builder.Append("Integrated Security=SSPI;");
                // Explicitly set ServerSPN to prevent Kerberos SPN mismatch errors
                // ("The target principal name is incorrect. Cannot generate SSPI context.")
                // when the server DNS name doesn't match the SPN registered in Active Directory.
                builder.Append($"ServerSPN=MSSQLSvc/{Server}:1433;");
            }
            else
            {
                builder.Append($"User Id={UserId};Password={Password};");
            }

            builder.Append($"Encrypt={Encrypt};TrustServerCertificate={TrustServerCertificate};");
            builder.Append("Persist Security Info=True;");

            return builder.ToString();
        }
    }
}
