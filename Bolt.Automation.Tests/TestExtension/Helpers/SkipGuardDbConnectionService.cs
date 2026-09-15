using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Services.Connections;
using NUnit.Framework;

namespace Bolt.Automation.Tests.TestExtension.Helpers;

/// <summary>
/// Decorator for <see cref="IDbConnectionService"/> that proactively checks SQL Server
/// connectivity before returning a connection string. If the database is unreachable
/// (e.g., Kerberos/SSPI failures, VPN issues), the test is automatically skipped
/// via <see cref="Assert.Inconclusive(string)"/> instead of failing with a cryptic exception.
/// Connectivity results are cached per connection string for the lifetime of the process.
/// Diagnostic details (SQL error codes, auth mode, OS, env vars) are logged via the
/// provided <see cref="IAutomationLogger"/>.
/// </summary>
internal sealed class SkipGuardDbConnectionService(IDbConnectionService inner, IAutomationLogger logger) : IDbConnectionService
{
    public string GetConnectionString(DatabaseType dbType)
    {
        var connectionString = inner.GetConnectionString(dbType);
        SqlConnectionGuard.SkipIfUnavailable(connectionString, dbType, logger);
        return connectionString;
    }
}
