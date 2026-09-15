using Bolt.Automation.Common.Models.TestData.Data;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using Account = Bolt.Automation.TestDataProvider.TestData.CommonTestData.Account;

namespace Bolt.Automation.TestDataProvider.Providers.CommonDataProvider
{
    public class AccountDataProvider
    {
        public static Account GetAccountData(Address? address = null)
        {
            var personalInfo = PersonalInfo.GetRandomPersonalInfo();
            var data = new Account()
            {
                FirstName = personalInfo.FirstName ?? string.Empty,
                LastName = personalInfo.LastName ?? string.Empty,
                Email = personalInfo.Email ?? string.Empty,
                PhoneNumber = personalInfo.PrimaryPhoneNumber ?? string.Empty,
                MailingAddress1 = address?.AddressLine1 ?? string.Empty,
                State = address?.State ?? string.Empty,
                City = address?.City ?? string.Empty,
                ZipCode = address?.ZipCode ?? string.Empty,
                LineOfBusiness = "Personal"
            };
            return data;
        }
    }
}


