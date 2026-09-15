using Bolt.Automation.InternalServices.Database.Contexts;
using Bolt.Automation.InternalServices.Database.Queries.Payment.Transaction;


namespace Bolt.Automation.InternalServices.Database.Queries.Payment
{
    public class PaymentQueries : IPaymentQueries
    {
        private readonly TransactionQueries _transaction;

        public PaymentQueries(PaymentDbContext context)
        {
            _transaction = new TransactionQueries(context);
        }

        public TransactionQueries Transaction => _transaction;

    }
}