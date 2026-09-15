using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.CreateCase;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.UpdateCase;
using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider
{
    public class CaseDataProvider
    {
        public static CreateCaseRequest CreateCaseData(string externalId, string lob, string cmCaseId,string accountExternalId)
        {
            return new CreateCaseRequest
            {
                CaseType = "Bind Request",
                CmCaseId = cmCaseId,
                Lob = lob,
                Originated = "sales",
                ProducerExternalId = externalId,
                AccountExternalId = accountExternalId
            };
        }

        public static CreateCaseRequest CreateServiceCaseData(string externalId, string policyExternalId)
        {
            return new CreateCaseRequest
            {
                CaseType = "Other",
                CmCaseId = RandomManager.GetRandomDigits(7),
                Originated = "service",
                ProducerExternalId = externalId,
                PolicyExternalId = policyExternalId
            };
        }

        public static UpdateCaseRequest UpdateServiceCaseData(string caseExternalId, string cmCaseId, string caseWorkflow)
        {
            return new UpdateCaseRequest
            {
                CaseExternalId = caseExternalId,
                CmCaseId = cmCaseId,
                CaseStatus = "Open",
                CaseWorkflow = caseWorkflow
            };
        }
    }
}
