using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData
{
    public static partial class QuoteStartRequestTestData
    {
        public static class Applicants
        {
            // Single test applicant used by standard/full QuoteStart requests.
            // (Model itself not modified – only core fields populated.)
            public static ApplicantDetails Credipulse = new()
            {
                GivenName = "AA" + RandomManager.GetRandomString(3),
                OtherGivenName = "A",
                Surname = "Creditpulse" + RandomManager.GetRandomString(2),
                PhoneNumber = "564-645-6455",
                EmailAddr = RandomManager.GetRandomEmail(),
                BirthDt = DateOnly.Parse("1980-09-09"),
                MaritalStatusCd = "S"
            };
        }
    }
}
