using System.Data;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Contexts;
using Bolt.Automation.InternalServices.Database.Extensions;
using LinqToDB;
using LinqToDB.Async;

namespace Bolt.Automation.InternalServices.Database.Queries.Main.Provisioning
{

    public class ProvisioningQueries(MainDbContext db, IAutomationLogger logger) : MainQuery(db)
    {
        private readonly IAutomationLogger _logger = logger;

        public async Task<(string Status, string? RetryData)> GetProvisioningActionByUserExternalId(string userExternalId)
        {
            var query = from pa in Db.ProvisioningActions
                        join u in Db.Users on pa.EntityId equals u.Id
                        where u.ExternalId == userExternalId
                        select new
                        {
                            pa.Status,
                            pa.RetryData
                        };

            _logger.Info($"Getting provisioning action by user external ID: {userExternalId}. Query: {query}");

            var result = await query.FirstOrDefaultAsync()
                .ExecuteWithSqlLoggingOrFailAsync(
                    Db,
                    _logger,
                    $"GetProvisioningActionByUserExternalIdAsync({userExternalId})",
                    result => result == null);

            return result != null ? (result.Status, result.RetryData) : (string.Empty, null);
        }

        public async Task<(string? groupName, string? groupExternalId)?> GetDataAfterProvisionByGroupExternalId(string aorGroupExternalId,
            string districtGroupEternalId)
        {
            var query = from gr in Db.GroupRelations
                        join gs in Db.Groups on gr.SourceId equals gs.Id
                        join gd in Db.Groups on gr.DestinationId equals gd.Id
                        where gs.ExternalId == aorGroupExternalId
                              && gd.ExternalId == districtGroupEternalId
                              && gr.RelationType == 2
                        select new
                        {
                            gs.Name,
                            gs.ExternalId
                        };

            var result = await query.FirstOrDefaultAsync()
                .ExecuteWithSqlLoggingOrFailAsync(
                    Db,
                    _logger,
                    $"GetDataAfterProvisionByGroupExternalId({aorGroupExternalId}, {districtGroupEternalId})",
                     result => result == null);

            return result != null ? (result.Name, result.ExternalId) : null;
        }

        public async Task<string?> GetGroupTreeByGroupName(string groupName)
        {
            var query = Db.vGroupTree
                .Where(e => e.GroupName == groupName)
                .Select(p => p.GRP_PATH)
                .FirstOrDefaultAsync();
            _logger.Info($"Getting group path by query:{query}");

            var groupPath = await query
                .ExecuteWithSqlLoggingOrFailAsync(
                    Db,
                    _logger,
                    $"GetGroupTreeByGroupName({groupName})",
                    result => result == string.Empty);

            return groupPath != string.Empty ? groupPath : string.Empty;
        }

        public async Task<(IReadOnlyList<string> userExternalIds, IReadOnlyList<string> groupExternalIds)> GetUserAccessGroup(string groupExternalId)
        {
            var query = from ugr in Db.UserGroupRoles
                        join u in Db.Users on ugr.UserId equals u.Id
                        join g in Db.Groups on ugr.GroupId equals g.Id
                        where g.ExternalId == groupExternalId
                              && ugr.IsActive == true
                              && ugr.LinkType == "Associated"
                        select new
                        {
                            UserExternalId = u.ExternalId,
                            GroupExternalId = g.ExternalId
                        };

            var limitedQuery = query.Take(10);

            _logger.Info($"Getting user access group data by group external ID: {groupExternalId}. Query: {limitedQuery}");

            var result = await limitedQuery.ToListAsync()
                .ExecuteWithSqlLoggingOrFailAsync(
                    Db,
                    _logger,
                    $"GetUserAccessGroup({groupExternalId})",
                     result => result == null);

            if (result == null || !result.Any())
            {
                return (new List<string>().AsReadOnly(), new List<string>().AsReadOnly());
            }

            var userExternalIds = result.Select(r => r.UserExternalId).ToList().AsReadOnly();
            var groupExternalIds = result.Select(r => r.GroupExternalId).ToList().AsReadOnly();

            return (userExternalIds, groupExternalIds);
        }

        public async Task<(string Status, string? RetryData)> GetProvisioningActionByGroupExternalId(string groupExternalId)
        {
            var query = from pa in Db.ProvisioningActions
                        join g in Db.Groups on pa.EntityId equals g.Id
                        where g.ExternalId == groupExternalId
                        select new
                        {
                            pa.Status,
                            pa.RetryData
                        };

            _logger.Info($"Getting provisioning action by group external ID: {groupExternalId}. Query: {query}");

            var result = await query.FirstOrDefaultAsync()
                .ExecuteWithSqlLoggingOrFailAsync(
                    Db,
                    _logger,
                    $"GetProvisioningActionByGroupExternalIdAsync({groupExternalId})",
                     result => result == null);
            return result != null ? (result.Status, result.RetryData) : (string.Empty, null);
        }
    }
}