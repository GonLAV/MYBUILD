using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Contexts;
using Bolt.Automation.InternalServices.Database.Extensions;
using LinqToDB;
using LinqToDB.Async;
using static Bolt.Automation.InternalServices.Database.DBHelpers.Case.CaseHelper;

namespace Bolt.Automation.InternalServices.Database.Queries.Main.Case
{
    public class CaseQueries(MainDbContext db, IAutomationLogger logger) : MainQuery(db)
    {

        private readonly IAutomationLogger _logger = logger;

        public async Task<CaseInfo?> GetCaseInfoByExternalIdAsync(string externalId)
        {
            var query = Db.Cases
                .Where(p => p.ExternalId == externalId)
                .Select(p => new CaseInfo
                {
                    Id = p.Id,
                    DateCreated = p.DateCreated,
                    DateUpdated = p.DateUpdated,
                    Tenant = p.Tenant,
                    EntityData = p.EntityData,
                    IsActive = p.IsActive,
                    EX_CaseNumber = p.EX_CaseNumber,
                    EX_Status = p.EX_Status,
                    CreatedByUserId = p.CreatedByUserId,
                    UpdatedByUserId = p.UpdatedByUserId,
                    AssignedToUserId = p.AssignedToUserId,
                    CreatedByGroupId = p.CreatedByGroupId,
                    OwnedByGroupId = p.OwnedByGroupId,
                    IsSLAMet = p.IsSLAMet,
                    BusinessFlowStatus = p.BusinessFlowStatus,
                    DueDate = p.DueDate,
                    Product = p.Product,
                    BusinessType = p.BusinessType,
                    Originated = p.Originated,
                    NeedHelp = p.NeedHelp,
                    Source = p.Source,
                    CaseType = p.CaseType,
                    SubType = p.SubType,
                    Severity = p.Severity,
                    ChangeReason = p.ChangeReason,
                    AddressTimeZone = p.AddressTimeZone,
                    SubCaseData = p.SubCaseData,
                    SubCaseType = p.SubCaseType,
                    ConsumerId = p.ConsumerId,
                    ApplicationId = p.ApplicationId,
                    PolicyBinderId = p.PolicyBinderId,
                    ExternalId = p.ExternalId,
                    IsSubmitted = p.IsSubmitted,
                    CmCaseId = p.CmCaseId,
                    Bundle = p.Bundle
                })
                .FirstOrDefaultAsync();

            var caseInfo = await query.ExecuteWithSqlLoggingOrFailAsync(
                Db,
                _logger,
                $"GetCaseInfoByExternalIdAsync({externalId})",
                result => result == null);

            return caseInfo;
        }

        public async Task<CaseInfo?> GetCaseInfoByIdAsync(string caseId)
        {
            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                _logger.Info($"Invalid case ID format: {caseId}");
                return null;
            }

            var query = Db.Cases
                .Where(p => p.Id == caseGuid)
                .Select(p => new CaseInfo
                {
                    Id = p.Id,
                    DateCreated = p.DateCreated,
                    DateUpdated = p.DateUpdated,
                    Tenant = p.Tenant,
                    EntityData = p.EntityData,
                    IsActive = p.IsActive,
                    EX_CaseNumber = p.EX_CaseNumber,
                    EX_Status = p.EX_Status,
                    CreatedByUserId = p.CreatedByUserId,
                    UpdatedByUserId = p.UpdatedByUserId,
                    AssignedToUserId = p.AssignedToUserId,
                    CreatedByGroupId = p.CreatedByGroupId,
                    OwnedByGroupId = p.OwnedByGroupId,
                    IsSLAMet = p.IsSLAMet,
                    BusinessFlowStatus = p.BusinessFlowStatus,
                    DueDate = p.DueDate,
                    Product = p.Product,
                    BusinessType = p.BusinessType,
                    Originated = p.Originated,
                    NeedHelp = p.NeedHelp,
                    Source = p.Source,
                    CaseType = p.CaseType,
                    SubType = p.SubType,
                    Severity = p.Severity,
                    ChangeReason = p.ChangeReason,
                    AddressTimeZone = p.AddressTimeZone,
                    SubCaseData = p.SubCaseData,
                    SubCaseType = p.SubCaseType,
                    ConsumerId = p.ConsumerId,
                    ApplicationId = p.ApplicationId,
                    PolicyBinderId = p.PolicyBinderId,
                    ExternalId = p.ExternalId,
                    IsSubmitted = p.IsSubmitted,
                    CmCaseId = p.CmCaseId,
                    Bundle = p.Bundle
                })
                .FirstOrDefaultAsync();

            var caseInfo = await query.ExecuteWithSqlLoggingOrFailAsync(
                Db,
                _logger,
                $"GetCaseInfoByIdAsync({caseId})",
                result => result == null);

            return caseInfo;
        }

        public async Task<CaseInfo?> GetCaseInfoByApplicationIdAsync(Guid? applicationId)
        {
            var query = Db.Cases
                .Where(p => p.ApplicationId == applicationId)
                .Select(p => new CaseInfo
                {
                    Id = p.Id,
                    DateCreated = p.DateCreated,
                    DateUpdated = p.DateUpdated,
                    Tenant = p.Tenant,
                    EntityData = p.EntityData,
                    IsActive = p.IsActive,
                    EX_CaseNumber = p.EX_CaseNumber,
                    EX_Status = p.EX_Status,
                    CreatedByUserId = p.CreatedByUserId,
                    UpdatedByUserId = p.UpdatedByUserId,
                    AssignedToUserId = p.AssignedToUserId,
                    CreatedByGroupId = p.CreatedByGroupId,
                    OwnedByGroupId = p.OwnedByGroupId,
                    IsSLAMet = p.IsSLAMet,
                    BusinessFlowStatus = p.BusinessFlowStatus,
                    DueDate = p.DueDate,
                    Product = p.Product,
                    BusinessType = p.BusinessType,
                    Originated = p.Originated,
                    NeedHelp = p.NeedHelp,
                    Source = p.Source,
                    CaseType = p.CaseType,
                    SubType = p.SubType,
                    Severity = p.Severity,
                    ChangeReason = p.ChangeReason,
                    AddressTimeZone = p.AddressTimeZone,
                    SubCaseData = p.SubCaseData,
                    SubCaseType = p.SubCaseType,
                    ConsumerId = p.ConsumerId,
                    ApplicationId = p.ApplicationId,
                    PolicyBinderId = p.PolicyBinderId,
                    ExternalId = p.ExternalId,
                    IsSubmitted = p.IsSubmitted,
                    CmCaseId = p.CmCaseId,
                    Bundle = p.Bundle
                })
                .FirstOrDefaultAsync();

            var caseInfo = await query.ExecuteWithSqlLoggingOrFailAsync(
                Db,
                _logger,
                $"GetCaseInfoByApplicationIdAsync({applicationId})",
                result => result == null);

            return caseInfo;
        }
    }
}
