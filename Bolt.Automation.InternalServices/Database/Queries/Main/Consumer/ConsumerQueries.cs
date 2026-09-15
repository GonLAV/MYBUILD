using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Contexts;
using LinqToDB;
using LinqToDB.Async;

namespace Bolt.Automation.InternalServices.Database.Queries.Main.Consumer
{
    public class ConsumerQueries(MainDbContext db, IAutomationLogger logger) : MainQuery(db)
    {
        private readonly IAutomationLogger _logger = logger;

        public async Task<Entities.Main.Consumer?> GetConsumerDataByExternalIdAsync(string externalId)
        {
            return await Db.Consumers
                .FirstOrDefaultAsync(c => c.ExternalId == externalId);
        }

        public async Task<bool> IsPolicyLinkedToCorrectConsumerAsync(string consumerEmail, string friendlyId)
        {
            var result = await (from c in Db.Consumers
                where c.Email == consumerEmail
                      && c.Status == 2
                      && c.Id == Db.Policies
                          .Where(p => p.FriendlyId == friendlyId)
                          .Select(p => p.ConsumerId)
                          .FirstOrDefault()
                select c).AnyAsync();

            return result;
        }

        public async Task<Entities.Main.Consumer> GetConsumerDataByFriendlyIdAsync(string friendlyId)
        {
            var consumer = await (from c in Db.Consumers
                where c.Id == Db.Policies
                    .Where(p => p.FriendlyId == friendlyId)
                    .Select(p => p.ConsumerId)
                    .FirstOrDefault()
                select c).FirstOrDefaultAsync();

            if (consumer != null)
                return consumer;

            throw new TestSetupException("No consumer data returned after db query");
        }

        public async Task<Entities.Main.Consumer?> GetFirstConsumerAsync()
        {
            return await Db.Consumers.FirstOrDefaultAsync();
        }
    }
}