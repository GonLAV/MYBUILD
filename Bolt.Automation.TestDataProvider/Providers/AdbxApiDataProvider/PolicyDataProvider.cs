using Bolt.Automation.ApiClients.AdbxApi.Entities.Policy;
using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.Providers.AdbxApiDataProvider
{
    public class PolicyDataProvider
    {
        public static UnderWriterPolicyCaseCreateModel CreateUnderwiterPolicyCaseData(string assignToId)
        {
            return new UnderWriterPolicyCaseCreateModel
            {
                CaseType = "Endorsement",
                AssignTo = Guid.Parse(assignToId),
                Notes = "Underwriter Policy Case Data Auto" + RandomManager.GetRandomString(5),
                //TimeZone = "Pacific Standard Time",
                ChangeTypesData = new ChangeTypesData
                {
                    ChangeType = "Add Additional Interest",
                    Data = new Dictionary<string, string>
                    {
                        { "AddressLine1", "123 New St" },
                        { "City", "Newcity" },
                        { "State", "CA" },
                        { "ZipCode", "90001" }
                    }
                }
            };
        }
    }
}
