using Bolt.Automation.Common;
using Bolt.Automation.Common.Models.Twilio;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    // Structural Twilio data only (phone numbers). Credential fields (AuthToken, AccountSid) live in
    // the secrets bundle under environments.<env>.twilio.<TENANT> and are overlaid at read-time by
    // TestContextAccessor.CurrentTwilioData. See Documentation/Secrets-Tier2-UserTwilio-Migration-Spec.md.
    public static class TwilioDataStore
    {
        private static readonly Dictionary<(Tenant, Environment, string), TwilioTestData> All = new()
        {
            [(Tenant.BOLTAG, Environment.Qa, "")] = new TwilioTestData
            {
                AuthToken          = "",
                AccountSid         = "",
                ToPhoneNumber      = "+16508816385",
                FromPhoneNumber    = "+972528988650",
                CaseToPhoneNumber  = "+11877461273",
                LeadToPhoneNumber  = "+16508816385"
            },

            [(Tenant.BOLTAG, Environment.Uat, "")] = new TwilioTestData
            {
                AuthToken          = "",
                AccountSid         = "",
                ToPhoneNumber      = "+16508816385",
                FromPhoneNumber    = "+972528988650",
                CaseToPhoneNumber  = "+11877461273",
                LeadToPhoneNumber  = "+16508816385"
            },
        };

        public static TwilioTestData? Get(Tenant tenant, Environment environment, string subtenant = "")
            => All.GetValueOrDefault((tenant, environment, subtenant));
    }
}
