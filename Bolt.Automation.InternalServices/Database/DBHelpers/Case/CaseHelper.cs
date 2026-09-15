using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.InternalServices.Database.Queries.Main.Case;

namespace Bolt.Automation.InternalServices.Database.DBHelpers.Case
{
    public sealed class CaseHelper(CaseQueries caseQueries, IAutomationLogger logger, IPollyRetryService pollyRetryService)
    {
        public class CaseInfo
        {
            public Guid? Id { get; set; }
            public DateTime DateCreated { get; set; }
            public DateTime DateUpdated { get; set; }
            public string? Tenant { get; set; }
            public string? EntityData { get; set; }
            public bool IsActive { get; set; }
            public string? EX_CaseNumber { get; set; }
            public string? EX_Status { get; set; }
            public Guid? CreatedByUserId { get; set; }
            public Guid? UpdatedByUserId { get; set; }
            public Guid? AssignedToUserId { get; set; }
            public Guid? CreatedByGroupId { get; set; }
            public Guid? OwnedByGroupId { get; set; }
            public bool? IsSLAMet { get; set; }
            public string? BusinessFlowStatus { get; set; }
            public DateTime? DueDate { get; set; }
            public string? Product { get; set; }
            public string? BusinessType { get; set; }
            public string? Originated { get; set; }
            public bool? NeedHelp { get; set; }
            public string? Source { get; set; }
            public string? CaseType { get; set; }
            public string? SubType { get; set; }
            public string? Severity { get; set; }
            public string? ChangeReason { get; set; }
            public string? AddressTimeZone { get; set; }
            public string? SubCaseData { get; set; }
            public string? SubCaseType { get; set; }
            public Guid? ConsumerId { get; set; }
            public Guid? ApplicationId { get; set; }
            public Guid? PolicyBinderId { get; set; }
            public string? ExternalId { get; set; }
            public bool? IsSubmitted { get; set; }
            public string? CmCaseId { get; set; }
            public bool Bundle { get; set; }
        }

        public async Task<string?> GetCaseIdByExternalIdAsync(string externalId)
        {
            var caseInfo = await caseQueries.GetCaseInfoByExternalIdAsync(externalId);
            return caseInfo?.Id.ToString();
        }

        public async Task<bool?> GetIsSubmittedByExternalIdAsync(string externalId)
        {
            var caseInfo = await caseQueries.GetCaseInfoByExternalIdAsync(externalId);
            return caseInfo?.IsSubmitted;
        }

        public async Task<string?> GetCaseCmIdByExternalIdAsync(string externalId)
        {
            var caseInfo = await caseQueries.GetCaseInfoByExternalIdAsync(externalId);
            return caseInfo?.CmCaseId;
        }

        public async Task<string?> GetExternalIdByCaseIdAsync(string caseId)
        {
            var caseInfo = await caseQueries.GetCaseInfoByIdAsync(caseId);
            return caseInfo?.ExternalId;
        }

        public async Task<string?> GetCmCaseIdByCaseIdAsync(string caseId)
        {
            var caseInfo = await caseQueries.GetCaseInfoByIdAsync(caseId);
            return caseInfo?.CmCaseId;
        }

        /// <summary>
        /// Waits for the CM Case ID to be populated in the database using the centralized PollyRetryService.
        /// </summary>
        /// <param name="caseId">The case ID to query for</param>
        /// <param name="timeoutSeconds">Total timeout in seconds (default: 70)</param>
        /// <returns>The populated case information</returns>
        /// <exception cref="Exception">Thrown when case is not found or CM Case ID is not populated within timeout</exception>
        public async Task<CaseInfo> WaitForCmCaseIdAsync(string caseId, int timeoutSeconds = 70)
        {
            logger.Info($"Waiting for CM Case ID to be populated for case {caseId}");

            await pollyRetryService.ExecuteWithExceptionAsync(async () =>
            {
                var caseData = await caseQueries.GetCaseInfoByIdAsync(caseId);

                if (caseData == null)
                {
                    logger.Error($"Case with ID {caseId} not found in database");
                    return false;
                }

                if (string.IsNullOrEmpty(caseData.CmCaseId))
                {
                    logger.Debug($"Case {caseId} found but CM Case ID is still null/empty - will retry");
                    return false;
                }

                logger.Info($"CM Case ID '{caseData.CmCaseId}' successfully found for case {caseId}");
                return true;
            }, timeoutSeconds, $"CM Case ID was not populated for case {caseId} within {timeoutSeconds} seconds");

            var finalCaseData = await caseQueries.GetCaseInfoByIdAsync(caseId);
            return finalCaseData!;
        }
    }
}
