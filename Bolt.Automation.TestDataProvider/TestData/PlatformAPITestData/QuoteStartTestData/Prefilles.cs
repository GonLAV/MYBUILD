
namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData
{
    public static partial class QuoteStartRequestTestData
    {
        public static class Prefills
        {
            public static readonly Dictionary<string, string> PrefillData = new()
            {
                ["PolicyData/CustomFields/ABTest-new-d2c"] = "1917B",
                ["PolicyData/CustomFields/ABTest-name-and-dob-pilot"] = "1214B",
                ["PolicyData/CustomFields/ABTest-d2c-optional-phone-number"] = "1966B",
                ["PolicyData/CustomFields/ABTest-optional-owner-questions"] = "1972B",
                ["PolicyData/CustomFields/ABTest-d2c-long-form"] = "1960C",
                ["PolicyData/CustomFields/ABTest-hqx-chatbot-cd"] = "1970B"
            };
        }
    }
}
