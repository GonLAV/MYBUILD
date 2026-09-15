using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.CreateConsumer;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Policy.Create;
using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider
{
    public static class CreatePolicyBinderDataProvider
    {
        public static string PolicyEffectiveDate => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static string PolicyExpirationDate => DateTime.Today.AddMonths(12).ToString("yyyy-MM-dd HH:mm:ss.fff");
        public static CreatePolicyRequest CreatePolicyBinderData(string quoteExternalId, string lob, ConsumerLine consumerLine = ConsumerLine.Personal)
        {
            return new CreatePolicyRequest
            {
                QuoteExternalId = quoteExternalId,
                Carrier = "SAFECO",
                Lob = lob,
                Premium = 155.0,
                PolicyNumber = "PolicyTest" + RandomManager.GetRandomDigits(5),
                Status = "Bound",
                ApptType = "Indirect",
                ExpirationDate = DateTime.Parse(PolicyExpirationDate),
                EffectiveDate = DateTime.Parse(PolicyEffectiveDate)
            };
        }
    }
}
