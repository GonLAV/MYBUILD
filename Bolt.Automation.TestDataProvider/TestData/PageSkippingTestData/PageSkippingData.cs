using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.Interview.Models;
using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;

namespace Bolt.Automation.TestDataProvider.TestData.PageSkippingTestData
{
    public static partial class PageSkippingData
    {
        public static class MissingAdditionalDrivers
        {

            /// <summary>
            /// Primary driver (Firstdriv Bolt) for page skipping scenario
            /// </summary>
            public static readonly DriverModel PrimaryDriver = new()
            {
                SequenceNum = 0,
                Id = "5740a098-74eb-11eb-9439-0242ac130002",
                DOB = "1999-05-11",
                Gender = "Male",
                MaritalStatus = "Single",
                LastName = "Bolt",
                FirstName = "Firstdriv",
                DriverEducation = "HighSchoolDiploma",
                DriverEmploymentIndustry = "Agriclt/Forestry/Fish",
                DriverOccupationStr = "Agr Inspect/Grader",
                HasAutoLosses = false,
                DriverStateLicensed = "CA",
                DriverLicenseStatus = "Valid"
            };

            /// <summary>
            /// Secondary driver (Secondrive Bolt) with missing DOB for page skipping scenario
            /// </summary>
            public static readonly DriverModel SecondaryDriverMissingDOB = new()
            {
                SequenceNum = 1,
                Id = "60267f2c-b3c8-11eb-8529-0242ac130003",
                DOB = "", // Missing DOB to trigger page skipping scenario
                Gender = "Female",
                MaritalStatus = "Married",
                LastName = "Bolt",
                FirstName = "Secondrive",
                DriverRelationshipToDriver1 = "Relative",
                DriverEducation = "HighSchoolDiploma",
                DriverEmploymentIndustry = "Agriclt/Forestry/Fish",
                DriverLicenseStatus = "Valid",
                DriverOccupationStr = "Agr Inspect/Grader"
            };

            /// <summary>
            /// First vehicle - 2009 DODGE CHALLENGER SE (Owned)
            /// VIN: 2B3LJ44V59H599429
            /// </summary>
            public static readonly VehicleModel Vehicle1_DodgeChallenger = new()
            {
                SequenceNum = 0,
                VIN = "2B3LJ44V59H599429",
                Id = "42ad81d2-74eb-11eb-9439-0242ac130002",
                MilesToWork = 20,
                NumberOfMiles = 1234,
                OwnershipType = "Owned"
            };

            /// <summary>
            /// Second vehicle - 2008 TOYOTA CAMRY
            /// VIN: 4T1BE46K98U203009
            /// </summary>
            public static readonly VehicleModel Vehicle2_ToyotaCamry = new()
            {
                SequenceNum = 1,
                VIN = "4T1BE46K98U203009",
                Id = "247b632e-7f0d-4f15-a034-c6354a5731e2",
                MilesToWork = 20,
                NumberOfMiles = 1234
            };

            /// <summary>
            /// Third vehicle - 2011 GMC ACADIA
            /// VIN: 1GNSKAE03BR182672
            /// </summary>
            public static readonly VehicleModel Vehicle3_GMCAcadia = new()
            {
                SequenceNum = 2,
                VIN = "1GNSKAE03BR182672",
                Id = "5f93f8c1-c9ca-4e98-936c-116d2c10c64d",
                MilesToWork = 20,
                OwnershipType = "Owned"
            };

            /// <summary>
            /// Fourth vehicle - 1987 MERCURY TRACER
            /// VIN: 4M2YU811X7KJ17302
            /// </summary>
            public static readonly VehicleModel Vehicle4_MercuryTracer = new()
            {
                SequenceNum = 3,
                VIN = "4M2YU811X7KJ17302",
                Id = "c2be7407-5928-425e-b9de-f00d171c1c41",
                MilesToWork = 20,
                NumberOfMiles = 1234,
                OwnershipType = "Owned"
            };

            /// <summary>
            /// Policy details for page skipping scenario
            /// </summary>
            public static readonly PolicyDetails PolicyData = new()
            {
                TypeOfResidence = "OwnHome",
                CurrentPersonalAutoCarrier = "TwentyFirstCentury",
                MonthsWithPriorCarrierAuto = 5,
                YearsWithContinuousCoverageAuto = 5,
                YearsWithPriorCarrierAuto = 2,
                CreditCheckPermission = true,
                IAgreeToReceiveEmailsByBolt = true,
                EffectiveDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                PriorCarrierExpirationDateAuto = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")
            };

            /// <summary>
            /// Personal information for page skipping scenario
            /// </summary>
            public static readonly PersonalInfoModel PersonalInformation = new()
            {
                FirstName = "Firstdriv",
                LastName = "Bolt",
                PrimaryPhoneNumber = "281-229-0999",
                Email = "demo1test@epos.com"
            };

            /// <summary>
            /// Gets a list of all drivers (Primary and Secondary with missing DOB)
            /// </summary>
            public static List<DriverModel> GetAllDrivers() => new()
     {
  PrimaryDriver,
    SecondaryDriverMissingDOB
   };

            /// <summary>
            /// Gets a list of all vehicles
            /// </summary>
            public static List<VehicleModel> GetAllVehicles() => new()
      {
       Vehicle1_DodgeChallenger,
       Vehicle2_ToyotaCamry,
       Vehicle3_GMCAcadia,
  Vehicle4_MercuryTracer
   };
        }
    }
}
