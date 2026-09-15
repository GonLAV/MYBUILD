using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.InternalServices.Database.Services.Connections
{
    /// <summary>
    /// Service for retrieving database connection strings from the current test context.
    /// The connection strings are automatically resolved for the current OS by DatabaseTestDataCollection.
    /// </summary>
    public class DbConnectionService(IScopeContext scopeContext) : IDbConnectionService
    {
        private readonly IScopeContext _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));

        public string GetConnectionString(DatabaseType dbType)
        {
            var collection = _scopeContext.Data.DatabaseCollection;

            if (collection == null)
            {
                throw new InvalidOperationException(
                    "Database collection is not configured in the test context. Ensure tenant and environment are set.");
            }

            // DatabaseTestDataCollection.indexer automatically returns OS-appropriate connection string
            return collection[dbType];
        }
    }
}
