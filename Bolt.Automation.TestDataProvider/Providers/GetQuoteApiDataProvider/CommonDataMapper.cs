using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider
{
    public static class CommonDataMapper
    {
        public static void MapAddress(dynamic data, Address? address)
        {
            data.PropertyAddress = address;
            // If PreviousAddress is needed, set it here or in the specific mapper
        }

        public static void MapPersonalInfo(dynamic data, PersonalInfoModel personalInfo)
        {
            data.FirstName = personalInfo.FirstName;
            data.LastName = personalInfo.LastName;
            data.DateOfBirth = personalInfo.DateOfBirth;
            data.PrimaryPhoneNumber = personalInfo.PrimaryPhoneNumber;
            data.Email = personalInfo.Email;
            data.MaritalStatus = personalInfo.MaritalStatus;
            data.PersonalLineGender = personalInfo.PersonalLineGender;
            data.OccupationStr = personalInfo.OccupationStr;
            data.EmploymentIndustry = personalInfo.EmploymentIndustry;
        }
    }
}
