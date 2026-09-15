
using Bolt.Automation.Common.Models.TestData.Interview.Models;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class BusinessTestData
        {
            public static BusinessDetails BusinessDetails = new()
            {
                OrganizationName= "TestOrganization",
                BoltIndustry= "81233200",
                YearsOfManagementExperience= 15,
                AnnualPayroll= 318000,
                FederalIDNumber= "71-5540267",
                LegalEntity= "SoleProprietorsOrIndividuals",
                NumberOfEmployees= "5",
                StartYear= 2019,
                NumberOfVehicles= 0,
                DoYouProvideMedicalBenefitsToYourEmployees= false,
                EmployeeWorkplaceSafetyProgram= true,
                HasThisSafetyProgramBeenCertified= false,
                DoesYourBusinessHaveAWebsite= "false"
            };

            public static BusinessDetails CommercialAutoBusiness = new()
            {
                OrganizationName = "TestAAYT",
                OtherOrganizationName = "TestY",
                BoltIndustry = "81233200",
                YearsOfManagementExperience = 15,
                AnnualSales = 500000,
                AnnualPayroll = 318000,
                FederalIDNumber = "715540267",
                LegalEntity = "SoleProprietorsOrIndividuals",
                NumberOfEmployees = "5",
                StartYear = 2019,
                NumberOfVehicles = 0,
                DoYouProvideMedicalBenefitsToYourEmployees = false,
                EmployeeWorkplaceSafetyProgram = true,
                HasThisSafetyProgramBeenCertified = false,
                DoesYourBusinessHaveAWebsite = "true",
                WebsiteAddress = "www.google.com",
                FranchiseSalonOrSpa = "false"
            };
        }
    }
   
}
