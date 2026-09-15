using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.InternalServices.Database.Services.Connections
{
    public interface IDbConnectionService
    {
        string GetConnectionString(DatabaseType dbType);
    }
}