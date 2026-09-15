using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class PersonalInfo
        {
            /// <summary>
            /// Builds a random <see cref="PersonalInfoModel"/>.
            /// </summary>
            public static PersonalInfoModel GetRandomPersonalInfo()
            {
                return new PersonalInfoModel
                {
                    FirstName = NameSelector.GetFirstName(),
                    LastName = NameSelector.GetLastName(),
                    DateOfBirth = "1985-11-11",
                    PrimaryPhoneNumber = "512-123-4567",
                    Email = "Creditpulse" + RandomManager.GetRandomString(5) + "@epos.com",
                    PersonalLineGender = "Male",
                    MaritalStatus = "Single",
                    OccupationStr = "Actor",
                    EmploymentIndustry = "Art/Design/Media",

                };
            }

            public static PersonalInfoModel GetRandomPersonalInfoPGR()
            {
                return new PersonalInfoModel
                {
                    FirstName = "AaNexus" + RandomManager.GetRandomString(6),
                    LastName = "Creditpulse" + RandomManager.GetRandomString(4),
                    DateOfBirth = "1985-11-11",
                    PrimaryPhoneNumber = "512-123-4567",
                    Email = "Creditpulse" + RandomManager.GetRandomString(5) + "@epos.com",
                    PersonalLineGender = "Male",
                    MaritalStatus = "Single",
                    OccupationStr = "Actor",
                    EmploymentIndustry = "Art/Design/Media",

                };
            }
        }
    }
}
