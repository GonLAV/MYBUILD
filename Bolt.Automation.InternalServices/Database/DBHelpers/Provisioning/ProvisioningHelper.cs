using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.InternalServices.Database.Queries.Main.Provisioning;

namespace Bolt.Automation.InternalServices.Database.DBHelpers.Provisioning
{
    public sealed class ProvisioningHelper(ProvisioningQueries provisioningQueries, IAutomationLogger logger, IPollyRetryService pollyRetryService)
    {
        public class ProvisioningActionInfo
        {
            public string EntityId { get; set; } = string.Empty;
            public string EntityType { get; set; } = string.Empty; // "Group" or "User"
            public string Status { get; set; } = string.Empty;
            public string? RetryData { get; set; }
            public bool IsSuccessful => Status == "Success";
            public bool IsFailed => Status == "Failed";
            public bool IsPending => !IsSuccessful && !IsFailed;
        }

        public async Task<ProvisioningActionInfo> GetGroupProvisioningActionAsync(string groupExternalId)
        {
            var (status, retryData) = await provisioningQueries.GetProvisioningActionByGroupExternalId(groupExternalId);
            return new ProvisioningActionInfo
            {
                EntityId = groupExternalId,
                EntityType = "Group",
                Status = status,
                RetryData = retryData
            };
        }

        public async Task<ProvisioningActionInfo> GetUserProvisioningActionAsync(string userExternalId)
        {
            var (status, retryData) = await provisioningQueries.GetProvisioningActionByUserExternalId(userExternalId);
            return new ProvisioningActionInfo
            {
                EntityId = userExternalId,
                EntityType = "User",
                Status = status,
                RetryData = retryData
            };
        }

        /// <summary>
        /// Waits for multiple provisioning actions to complete using the centralized PollyRetryService.
        /// </summary>
        /// <param name="groupExternalIds">List of group external IDs to monitor</param>
        /// <param name="userExternalIds">List of user external IDs to monitor</param>
        /// <param name="timeoutSeconds">Total timeout in seconds (default: 70)</param>
        /// <returns>List of all provisioning action results</returns>
        /// <exception cref="Exception">Thrown when any action fails or not all complete within timeout</exception>
        public async Task<List<ProvisioningActionInfo>> WaitForProvisioningActionsAsync(
            IEnumerable<string> groupExternalIds,
            IEnumerable<string> userExternalIds,
            int timeoutSeconds = 70)
        {
            var groupIds = groupExternalIds.ToList();
            var userIds = userExternalIds.ToList();

            logger.Info($"Waiting for provisioning actions to complete - Groups: {string.Join(", ", groupIds)}, Users: {string.Join(", ", userIds)}");

            List<ProvisioningActionInfo> finalResults = [];
            List<ProvisioningActionInfo> lastKnownActions = [];

            try
            {
                await pollyRetryService.ExecuteWithExceptionAsync(async () =>
                {
                    var allActions = new List<ProvisioningActionInfo>();

                    foreach (var groupId in groupIds)
                    {
                        var groupAction = await GetGroupProvisioningActionAsync(groupId);
                        allActions.Add(groupAction);
                        logger.Debug($"Group {groupId}: Status = {groupAction.Status}");
                    }

                    foreach (var userId in userIds)
                    {
                        var userAction = await GetUserProvisioningActionAsync(userId);
                        allActions.Add(userAction);
                        logger.Debug($"User {userId}: Status = {userAction.Status}");
                    }

                    var failedActions = allActions.Where(a => a.IsFailed).ToList();
                    if (failedActions.Count != 0)
                    {
                        var failedDetails = string.Join(", ", failedActions.Select(f => $"{f.EntityType} {f.EntityId}: {f.RetryData}"));
                        throw new Exception($"Provisioning failed for: {failedDetails}");
                    }

                    var successful = allActions.Count(a => a.IsSuccessful);
                    var total = allActions.Count;
                    logger.Info($"Provisioning check - {successful}/{total} actions completed successfully");

                    lastKnownActions = allActions;
                    finalResults = allActions;

                    return successful == total;
                }, timeoutSeconds, $"Provisioning actions did not complete within {timeoutSeconds} seconds.");
            }
            catch (Exception ex) when (ex.Message.StartsWith("Provisioning actions did not complete"))
            {
                var pendingActions = lastKnownActions.Where(a => a.IsPending).ToList();
                var pendingSummary = pendingActions.Count > 0
                    ? string.Join(", ", pendingActions.Select(a => $"{a.EntityType} {a.EntityId} (Status: {a.Status})"))
                    : "unknown (no poll data captured)";

                throw new Exception(
                    $"Provisioning actions did not complete within {timeoutSeconds} seconds. " +
                    $"Pending ({pendingActions.Count}/{lastKnownActions.Count}): {pendingSummary}", ex);
            }

            logger.Info("All provisioning actions completed successfully");
            return finalResults;
        }

        /// <summary>
        /// Convenience method for waiting on provisioning actions when you have the entity lists from the test.
        /// </summary>
        /// <param name="provisioningEntities">Collection of (ExternalId, EntityType) tuples</param>
        /// <param name="timeoutSeconds">Total timeout in seconds (default: 70)</param>
        /// <returns>List of all provisioning action results</returns>
        public async Task<List<ProvisioningActionInfo>> WaitForProvisioningActionsAsync(
            IEnumerable<(string ExternalId, string EntityType)> provisioningEntities,
            int timeoutSeconds = 70)
        {
            var entities = provisioningEntities.ToList();
            var groupIds = entities.Where(e => e.EntityType.Equals("Group", StringComparison.OrdinalIgnoreCase))
                                   .Select(e => e.ExternalId);
            var userIds = entities.Where(e => e.EntityType.Equals("User", StringComparison.OrdinalIgnoreCase))
                                  .Select(e => e.ExternalId);

            return await WaitForProvisioningActionsAsync(groupIds, userIds, timeoutSeconds);
        }
    }
}