using Bolt.Automation.InternalServices.Database.Contexts;

namespace Bolt.Automation.InternalServices.Database.Queries.Main
{
    public abstract class MainQuery(MainDbContext db)
    {
        protected readonly MainDbContext Db = db;
    }
}
