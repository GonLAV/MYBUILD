using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.InternalServices.Database.Queries.Main.Renewals;

namespace Bolt.Automation.InternalServices.Database.DBHelpers.Renewals
{
    public sealed class RenewalsHelper(RenewalsQueries renewalsQueries, IAutomationLogger logger)
    {
        public async Task<RenewalsRequoteDetailsDto?> GetRenewalsRequoteDetailsWithRetryAsync(
        IPollyRetryService pollyRetry,
        Guid policyBinderId,
        string requiredStatus = "Info Required")
        {
            RenewalsRequoteDetailsDto? details = null;
            await pollyRetry.ExecuteWithRetryAsync(async () =>
            {
                details = await renewalsQueries.GetRenewalsRequoteDetailsByPolicyBinderIdAsync(policyBinderId);
                return details != null && details.Status == requiredStatus;
            });
            return details;
        }

        public class RenewalsRequoteDetailsDto
        {
            public Guid PolicyBinderId { get; set; }
            public string? Status { get; set; }
            public Guid? NewQuoteId { get; set; } 
            public Guid OriginalQuoteId { get; set; } 

        }
    }
}
