using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class Drivers
        {
            public static readonly DriverModel BobSmith = new()//first driver data
            {
                Id = "60267f2c-b3c8-11eb-8529-0242ac130003",
                DriverEducation = "HighSchoolDiploma",
                DriverStateLicensed = "CA",
                LastName = "Smith",
                FirstName = "Bob",
                DriverOccupationStr = "Other",
                HasViolations = false,
                DOB = "1996-12-25",
                DriversLicenseBeenSuspendedOrRevoked = false,
                DriverEmploymentIndustry = "Other",
                SequenceNum = 0,
                HasAutoLosses = false,
                MaritalStatus = "Married",
                Gender = "Male",
                DriverDateLicensed = "2015-12-25",
                DriverLicenseStatus = "Valid",
                HasAccidents = false,
                SR22OrFinancialResponsibilityStatement = false,
                DriverLicenseNumber = "B12345678",
                IsDefensiveDriver = false
            };

            public static readonly DriverModel KateSmith = new()//second driver data
            {
                Id = "5740a098-74eb-11eb-9439-0242ac130002",
                DriverEducation = "HighSchoolDiploma",
                DriverStateLicensed = "CA",
                LastName = "Smith",
                FirstName = "Kate",
                DriverOccupationStr = "Other",
                HasViolations = false,
                DOB = "1980-12-25",
                DriversLicenseBeenSuspendedOrRevoked = false,
                DriverRelationshipToDriver1 = "DomesticPartner",
                DriverEmploymentIndustry = "Other",
                SequenceNum = 1,
                IsDrv2RegisterOwner = false,
                HasAutoLosses = false,
                MaritalStatus = "Married",
                Gender = "Female",
                DriverDateLicensed = "2015-05-11",
                DriverLicenseStatus = "Valid",
                HasAccidents = false,
                SR22OrFinancialResponsibilityStatement = false,
                DriverLicenseNumber = "95682541",
                IsDefensiveDriver = false
            };

            public static readonly DriverModel UsaaTestDriver = new()
            {
                Id = "2a1385ad-9b8e-49d6-8ad4-b9e7b9694bb9",
                LastName = "Test",
                FirstName = "Test",
                DriverOccupationStr = "Other",
                DOB = "1985-11-11",
                MaritalStatus = "Single",
                Gender = "Male",
                HasAutoLosses = false,
                HasAccidents = false,
                HasViolations = false
            };

            // Bristol West FQ (TC 237026) primary driver.
            public static readonly DriverModel D2CDriver = new()
            {
                Id = "5740a098-74eb-11eb-9439-0242ac130002",
                SequenceNum = 0,
                FirstName = "WFA",
                LastName = "TEST",
                MaritalStatus = "Single",
                Gender = "Female",
                DOB = "1999-05-11",
                DriverLicenseStatus = "Valid",
                HasAutoLosses = false,
                HasAccidents = false,
                HasViolations = false
            };

            public static readonly DriverModel UsaaTestFirst = new()
            {
                SequenceNum = 0,
                Id = "5740a098-74eb-11eb-9439-0242ac130002",
                MaritalStatus = "Single",
                LastName = "Test",
                FirstName = "TestFirst",
                DOB = "1955-03-08",
                Gender = "Male",
                DriverEducation = "HighSchoolDiploma",
                DriverLicenseStatus = "Valid",
                DriverEmploymentIndustry = "Art/Design/Media",
                DriverOccupationStr = "Actor"
            };

            public static readonly DriverModel UsaaTestSecWithLoss = new()
            {
                SequenceNum = 1,
                Id = "9940a098-74eb-11eb-9439-0242ac130003",
                LastName = "TestQ",
                FirstName = "TestSec",
                HasAutoLosses = true,
                Losses = new List<LossModel>
                {
                    new LossModel
                    {
                        Id = "235ea7b0-6b13-4124-b367-ae5cc46a4583",
                        AutoLossesDescription = "HitAnimal",
                        AutoLossesAmount = "1000",
                        AutoLossesDate = "2022-11-26",
                        SequenceNum = 0,
                        AutoLossesVehicleInvolved = "0"
                    }
                }
            };
            public static readonly DriverModel KLXTestDriver = new()
            {
                Id = "2a1385ad-9b8e-49d6-8ad4-b9e7b9694bb9",
                LastName = "Test",
                FirstName = "Test",
                DOB = "1985-11-11",
                MaritalStatus = "Single",
                Gender = "Male",
                DriverEducation = "HighSchoolDiploma",
                DriverLicenseStatus = "Valid",
                DriverEmploymentIndustry = "Art/Design/Media",
                DriverOccupationStr = "Actor",
                HasAutoLosses = false,
                HasAccidents = false,
                HasViolations = false,
                DriverStateLicensed = "TX",
                DriverDateLicensed = "2016-09-11",
                IsDefensiveDriver = false,
                DriversLicenseBeenSuspendedOrRevoked = false,
                SR22OrFinancialResponsibilityStatement = false
            };
        }
    }
}
