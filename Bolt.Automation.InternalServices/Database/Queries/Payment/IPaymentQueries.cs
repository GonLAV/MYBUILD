using Bolt.Automation.InternalServices.Database.Queries.Payment.Transaction;

namespace Bolt.Automation.InternalServices.Database.Queries.Payment
{
    public interface IPaymentQueries
    {
        TransactionQueries Transaction { get; }
    }
}