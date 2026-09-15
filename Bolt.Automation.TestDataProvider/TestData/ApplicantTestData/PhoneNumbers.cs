using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicantTestData
{
    public class PhoneNumbers
    {
        public static readonly PhoneNumber numbers = new()
        {
            Number = "2811550999",
            Type = "",
            IsDefault = false,   
        };

        public static readonly PhoneNumber NumberUpdate = new()
        {
            Number = "2811550910",
            Type = null,
            IsDefault = false,
        };
    }
}
