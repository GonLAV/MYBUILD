using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Contexts;
using Bolt.Automation.InternalServices.Database.Models;
using LinqToDB;
using LinqToDB.Async;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bolt.Automation.InternalServices.Database.Queries.Main.Policy
{
    public class PolicyQueries(MainDbContext db, IAutomationLogger logger) : MainQuery(db)
    {
        private readonly IAutomationLogger _logger = logger;

        public async Task<string?> GetFriendlyIdByExternalIdAsync(string externalId)
        {
            return await Db.Policies
                .Where(p => p.ExternalId == externalId)
                .OrderByDescending(p => p.DateCreated)
                .Select(p => p.FriendlyId)
                .FirstOrDefaultAsync();
        }

        public async Task<Entities.Main.Policy?> GetPolicyByFriendlyIdAsync(string friendlyId)
        {
            return await Db.Policies
                .Where(p => p.FriendlyId == friendlyId)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetPolicyIdByFriendlyIdAsync(string friendlyId)
        {
            return await Db.Policies
                .Where(p => p.FriendlyId == friendlyId)
                .Select(p => p.Id.ToString())
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetLeadIdByExternalIdAsync(string externalId)
        {
            return await Db.Policies
                .Where(p => p.ExternalId == externalId)
                .Select(p => p.LeadId.ToString())
                .FirstOrDefaultAsync();
        }

        public async Task<short> GetCurrentStageByFriendlyIdAsync(string friendlyId)
        {
            return await Db.Policies
                .Where(p => p.FriendlyId == friendlyId)
                .Select(p => p.CurrentStage)
                .FirstOrDefaultAsync();
        }

        public async Task<short> GetLastVisitedStageByFriendlyIdAsync(string friendlyId)
        {
            return await Db.Policies
                .Where(p => p.FriendlyId == friendlyId)
                .Select(p => p.LastVisitedStage)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetKickOutReasonByFriendlyIdAsync(string friendlyId)
        {
            var json = await GetPolicyDataRawAsync(friendlyId);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var j = JObject.Parse(json);
                return j.SelectToken("KickOutReason")?.ToString(); // ✅ Adjusted path
            }
            catch (JsonException)
            {
                // Log or handle JSON parse error if needed
                return null;
            }
        }


        public async Task<string?> GetPolicyDataRawAsync(string friendlyId)
        {
            return await Db.Policies
                .Where(p => p.FriendlyId == friendlyId)
                .OrderByDescending(p => p.DateCreated)
                .Select(p => p.PolicyData)
                .FirstOrDefaultAsync();
        }
        public async Task<Entities.Main.Policy?> GetFirstPolicyAsync()
        {
            return await Db.Policies
                .OrderBy(p => p.DateCreated)
                .FirstOrDefaultAsync();
        }
        public async Task<string?> GetFirstFriendlyIdWithAttachmentAsync()
        {
            return await (
                    from po in Db.Policies
                    join pa in Db.PolicyAttachments on po.Id equals pa.PolicyId
                    orderby pa.Datecreated // or po.DateCreated if preferred
                    select po.FriendlyId
                )
                .FirstOrDefaultAsync();
        }

        public async Task<Guid> GetPolicyBinderIdByExPolicyNumberAsync(string exPolicyNumber)
        {
            var policyBinderId = await Db.PolicyBinders
                .Where(pb => pb.EX_PolicyNumber == exPolicyNumber)
                .Select(pb => pb.Id)
                .FirstOrDefaultAsync();
            return policyBinderId;
        }

        public async Task<List<Entities.Main.PolicyAttachment>> GetPolicyAttachmentsByExternalIdAsync(string externalId)
        {
            return await (
                from pa in Db.PolicyAttachments
                join p in Db.Policies on pa.PolicyId equals p.Id
                where p.ExternalId == externalId
                orderby pa.Datecreated descending
                select pa
            ).ToListAsync();
        }

        public async Task<List<Entities.Main.PolicyAttachment>> GetPolicyAttachmentsByExternalIdAndTypeAsync(string externalId, short attachmentType)
        {
            return await (
                from pa in Db.PolicyAttachments
                join p in Db.Policies on pa.PolicyId equals p.Id
                where p.ExternalId == externalId
                      && pa.AttachmentType == attachmentType
                orderby pa.Datecreated descending
                select pa
            ).ToListAsync();
        }

        public async Task<List<PolicyAndAttachmentData>> GetPolicyAndAttachmentsByFriendlyIdAsync(string friendlyId)
        {
            return await (
                from po in Db.Policies
                join pa in Db.PolicyAttachments on po.Id equals pa.PolicyId
                where po.FriendlyId == friendlyId
                orderby pa.Datecreated descending
                select new PolicyAndAttachmentData
                {
                    // Policy fields
                    Id = po.Id,
                    FriendlyId = po.FriendlyId,
                    ExternalId = po.ExternalId,
                    DateCreated = po.DateCreated,
                    ConsumerId = po.ConsumerId,
                    CurrentStage = po.CurrentStage,
                    LastVisitedStage = po.LastVisitedStage,
                    PolicyData = po.PolicyData,

                    // Attachment fields
                    AttachmentId = pa.Id,
                    PolicyId = pa.PolicyId,
                    AttachmentDateCreated = pa.Datecreated,
                    IsActive = pa.IsActive,
                    DateUpdated = pa.DateUpdated,
                    Content = pa.Content,
                    AttachmentType = pa.AttachmentType
                }
            ).Take(10).ToListAsync();
        }
    }
}