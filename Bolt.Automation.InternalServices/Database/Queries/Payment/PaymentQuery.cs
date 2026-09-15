using Bolt.Automation.InternalServices.Database.Contexts;

namespace Bolt.Automation.InternalServices.Database.Queries.Payment
{
    public abstract class PaymentQuery
    {
        protected readonly PaymentDbContext Db;
        protected PaymentQuery(PaymentDbContext db)
        {
            Db = db;
        }
    }
}