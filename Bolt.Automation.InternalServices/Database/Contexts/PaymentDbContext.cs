using Bolt.Automation.InternalServices.Database.Entities.Payment;
using LinqToDB;
using LinqToDB.Data;

namespace Bolt.Automation.InternalServices.Database.Contexts
{
    public class PaymentDbContext : DataConnection
    {
        public PaymentDbContext(DataOptions options) : base(options)
        {
        }

        public ITable<Transaction> Transactions => this.GetTable<Transaction>();
    }
}