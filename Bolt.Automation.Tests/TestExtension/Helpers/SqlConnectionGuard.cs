using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestExtension.Helpers;

/// <summary>
/// Static guard that checks SQL Server connectivity once per connection string
/// and caches the result for the lifetime of the process.
/// Use with NUnit's <see cref="Assert.Inconclusive(string)"/> to gracefully skip
/// tests when the database is unreachable (e.g., Kerberos/SSPI failures).
/// </summary>
public static class SqlConnectionGuard
{
    private static readonly ConcurrentDictionary<string, (bool IsAvailable, string? Error)> _cache = new();
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Checks whether the SQL Server behind <paramref name="connectionString"/> is reachable.
    /// The result is cached per connection string for the lifetime of the process.
    /// </summary>
    public static (bool IsAvailable, string? Error) Check(string connectionString)
    {
        return _cache.GetOrAdd(connectionString, cs =>
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(cs)
                {
                    ConnectTimeout = (int)ConnectionTimeout.TotalSeconds
                };

                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.CommandTimeout = (int)ConnectionTimeout.TotalSeconds;
                cmd.ExecuteScalar();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, BuildDiagnosticMessage(cs, ex));
            }
        });
    }

    /// <summary>
    /// Calls <see cref="Assert.Inconclusive(string)"/> if the database for the given
    /// <paramref name="connectionString"/> is not reachable. Logs connectivity check
    /// start and result via <paramref name="logger"/> when provided.
    /// </summary>
    public static void SkipIfUnavailable(string connectionString, DatabaseType dbType, IAutomationLogger? logger = null)
    {
        logger?.Info($"Checking SQL Server ({dbType}) connectivity...");
        var (isAvailable, error) = Check(connectionString);
        if (!isAvailable)
        {
            logger?.Warning($"SQL Server ({dbType}) is unavailable. Diagnostics:\n{error}");
            Assert.Inconclusive($"SQL Server ({dbType}) is unavailable — skipping test. Error: {error}");
        }
        else
        {
            logger?.Info($"SQL Server ({dbType}) connectivity check passed.");
        }
    }

    /// <summary>
    /// Builds a detailed diagnostic message for a failed connection attempt, including
    /// SQL error codes, authentication mode, OS platform, and relevant environment variables.
    /// </summary>
    private static string BuildDiagnosticMessage(string connectionString, Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ex.Message);
        sb.AppendLine($"  Exception type: {ex.GetType().Name}");

        if (ex is SqlException sqlEx)
        {
            sb.AppendLine($"  SQL Error Number: {sqlEx.Number}, State: {sqlEx.State}, Severity: {sqlEx.Class}");
            if (sqlEx.Errors.Count > 1)
            {
                foreach (SqlError sqlError in sqlEx.Errors)
                    sb.AppendLine($"  SQL Error [{sqlError.Number}] State {sqlError.State} Severity {sqlError.Class}: {sqlError.Message}");
            }
        }

        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.AppendLine($"  Inner [{inner.GetType().Name}]: {inner.Message}");
            inner = inner.InnerException;
        }

        try
        {
            var csb = new SqlConnectionStringBuilder(connectionString);
            sb.AppendLine($"  Server: {csb.DataSource}, Database: {csb.InitialCatalog}");
            var authType = !string.IsNullOrEmpty(csb.UserID)
                ? $"SQL authentication (user: {csb.UserID})"
                : csb.IntegratedSecurity ? "Windows Integrated Security (SSPI)"
                : "unknown";
            sb.AppendLine($"  Auth mode: {authType}");
            sb.AppendLine($"  Connect timeout: {ConnectionTimeout.TotalSeconds}s");
        }
        catch
        {
            // ignore connection string parse errors
        }

        sb.AppendLine($"  OS: {RuntimeInformation.OSDescription}");
        var secretsPath = Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH");
        sb.AppendLine($"  BOLT_SECRETS_PATH: {(string.IsNullOrWhiteSpace(secretsPath) ? "(not set)" : secretsPath)}");

        return sb.ToString().TrimEnd();
    }
}
