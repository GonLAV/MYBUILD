using Bolt.Automation.InternalServices.Database.Contexts;
using LinqToDB;
using LinqToDB.Async;



namespace Bolt.Automation.InternalServices.Database.Queries.Payment.Transaction
{
    public class TransactionQueries : PaymentQuery
    {
        public TransactionQueries(PaymentDbContext db) : base(db)
        {
        }
        
        public async Task<List<Entities.Payment.Transaction>> GetTransactionDataByExternalIdAsync(
            string externalTransactionId)
        {
            return await Db.Transactions
                .Where(t => t.ExternalId == externalTransactionId)
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();
        }
        
        public async Task<Entities.Payment.Transaction?> GetFirstTransactionAsync()
        {
            return await Db.Transactions
                .OrderBy(t => t.DateCreated)
                .FirstOrDefaultAsync();
        }
    }
}