using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Contexts;
using Bolt.Automation.InternalServices.Database.Extensions;
using LinqToDB;
using LinqToDB.Async;

using static Bolt.Automation.InternalServices.Database.DBHelpers.Renewals.RenewalsHelper;

namespace Bolt.Automation.InternalServices.Database.Queries.Main.Renewals
{
    public class RenewalsQueries(MainDbContext db, IAutomationLogger logger) : MainQuery(db)
    {
        private readonly IAutomationLogger _logger = logger;

        public async Task<RenewalsRequoteDetailsDto?> GetRenewalsRequoteDetailsByPolicyBinderIdAsync(Guid policyBinderId)
        {
            return await Db.RenewalsRequoteDetails
                .Where(pb => pb.PolicyBinderId == policyBinderId)
                .Select(pb => new RenewalsRequoteDetailsDto
                {
                    PolicyBinderId = pb.PolicyBinderId,
                    Status = pb.Status,
                    NewQuoteId = pb.NewQuoteId,
                    OriginalQuoteId = pb.OriginalQuoteId,
                })
                .FirstOrDefaultAsync()
                .ExecuteWithSqlLoggingOrFailAsync(Db, _logger, "Get Renewals Requote Details By PolicyBinderId");
        }

    }
}
