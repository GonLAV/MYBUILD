using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.Common.Models.TestData.Interview.Models;
using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;

namespace Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider
{
    public static class CommercialLineDataMapper
    {
        public static void MapAddress(CommercialLineData data, Address? address)
        {
            CommonDataMapper.MapAddress(data, address);
            data.PreviousAddress = AddressData.CT;
        }

        /// <summary>
        /// Maps the property address and, when a mailing address differs, the mailing address.
        /// Used by the Commercial Auto path (no PreviousAddress, unlike the WC path).
        /// </summary>
        public static void MapAutoAddresses(CommercialLineData data, Address? propertyAddress, Address? mailingAddress)
        {
            CommonDataMapper.MapAddress(data, propertyAddress);
            data.MailingAddress = mailingAddress;
            data.IsMailAddress = mailingAddress != null;
        }

        public static void MapPersonalInfo(CommercialLineData data, PersonalInfoModel personalInfo)
        {
            CommonDataMapper.MapPersonalInfo(data, personalInfo);
        }

        public static void MapBusinessDetails(CommercialLineData data, BusinessDetails businessDetails)
        {
            data.OrganizationName = businessDetails.OrganizationName;
            data.BoltIndustry = businessDetails.BoltIndustry;
            data.YearsOfManagementExperience = businessDetails.YearsOfManagementExperience;
            data.AnnualPayroll = businessDetails.AnnualPayroll;
            data.FederalIDNumber = businessDetails.FederalIDNumber;
            data.LegalEntity = businessDetails.LegalEntity;
            data.NumberOfEmployees = businessDetails.NumberOfEmployees;
            data.StartYear = businessDetails.StartYear;
            data.NumberOfVehicles = businessDetails.NumberOfVehicles;
            data.DoYouProvideMedicalBenefitsToYourEmployees = businessDetails.DoYouProvideMedicalBenefitsToYourEmployees;
            data.EmployeeWorkplaceSafetyProgram = businessDetails.EmployeeWorkplaceSafetyProgram;
            data.HasThisSafetyProgramBeenCertified = businessDetails.HasThisSafetyProgramBeenCertified;
            data.DoesYourBusinessHaveAWebsite = businessDetails.DoesYourBusinessHaveAWebsite;
            data.WebsiteAddress = businessDetails.WebsiteAddress;
            data.OtherOrganizationName = businessDetails.OtherOrganizationName;
            data.FranchiseSalonOrSpa = businessDetails.FranchiseSalonOrSpa;
            data.AnnualSales = businessDetails.AnnualSales;
        }

        public static void MapVehicles(CommercialLineData data, List<VehicleModel>? vehicles)
        {
            data.Vehicles = vehicles;
        }

        public static void MapLosses(CommercialLineData data, List<CommercialLoss>? losses)
        {
            data.Losses = losses;
            data.PLHaveAnyLosses = losses?.Count > 0;
        }

        public static void MapLocationDetails(CommercialLineData data, List<LocationDetails> locationDetailsList)
        {
            data.Locations = locationDetailsList.Select(ld => new Location
            {
                Id = ld.Id,
                SequenceNum = ld.SequenceNum,
                HasEmployees = ld.HasEmployees,
                Buildings = ld.Buildings != null
                    ? ld.Buildings.Select(b => new Building
                    {
                        Id = b.Id,
                        LocationAddress = b.LocationAddress,
                        AnySubcontractedWork = b.AnySubcontractedWork,
                        SquareFootageOccupied = b.SquareFootageOccupied,
                        SequenceNum = b.SequenceNum,
                        LocationOption = b.LocationOption,
                        YearOriginalConstruction = b.YearOriginalConstruction,
                        NumberOfStories = b.NumberOfStories,
                        ConstructionType = b.ConstructionType,
                        AnnualSales = b.AnnualSales,
                    }).ToList()
                    : null,
                EmployeeClassDetails = ld.EmployeeClassDetails != null
                    ? ld.EmployeeClassDetails.Select(e => new EmployeeClassDetail
                    {
                        Id = e.Id,
                        FullTime = e.FullTime,
                        TotalPayroll = e.TotalPayroll,
                        Description = e.Description,
                        SequenceNum = e.SequenceNum,
                        PartTime = e.PartTime,
                        ClassCode = e.ClassCode
                    }).ToList()
                    : null
            }).ToList();
        }

        public static void MapPolicyDetails(CommercialLineData data, PolicyDetails policyDetails)
        {
            data.CurrentPremiumWC = policyDetails.CurrentPremiumWC;
            data.EffectiveDate = policyDetails.EffectiveDate;
            data.CurrentWCCarrier = policyDetails.CurrentWCCarrier;
            data.DeclinedCanceledOrNonRenewed = policyDetails.DeclinedCanceledOrNonRenewed;
            data.WCContinousCoverage = policyDetails.WCContinousCoverage;
            data.WcExpirationDate = policyDetails.WcExpirationDate;
            data.CreditCheckPermission = policyDetails.CreditCheckPermission;
        }


    }
}
