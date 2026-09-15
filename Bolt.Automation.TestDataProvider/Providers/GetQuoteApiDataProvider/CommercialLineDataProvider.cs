using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.Common.Models.TestData.Interview.Models;
using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;

namespace Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider
{
    public static class CommercialLineDataProvider
    {
        private static CommercialLineData MapAllToCommercialLineData(
            Address address,
            BusinessDetails businessDetails,
            PersonalInfoModel personalInfo,
            List<LocationDetails> locationDetails,
            PolicyDetails policyDetails)
        {
            var data = new CommercialLineData();
            CommercialLineDataMapper.MapAddress(data, address);
            CommercialLineDataMapper.MapBusinessDetails(data, businessDetails);
            CommercialLineDataMapper.MapPersonalInfo(data, personalInfo);
            CommercialLineDataMapper.MapLocationDetails(data, locationDetails);
            CommercialLineDataMapper.MapPolicyDetails(data, policyDetails);
            return data;
        }

        public static CommercialLineData GetCommercialLineData(
            Address? address = null,
            BusinessDetails? businessDetails = null,
            PersonalInfoModel? personalInfo = null,
            List<LocationDetails>? locationDetails = null,
            PolicyDetails? policyDetails = null)
        {
            var selectedAddress = address ?? AddressData.NY_Averill_Park;
            var selectedBusinessDetails = businessDetails ?? BusinessTestData.BusinessDetails;
            var selectedPersonalInfo = personalInfo ?? PersonalInfo.GetRandomPersonalInfo();
            var selectedLocationDetails = locationDetails ?? new List<LocationDetails>
            {
                // Example: create from ApplicationTestData.LocationsData.LocationA
                new LocationDetails
                {
                    Id = LocationsData.LocationA.Id,
                    SequenceNum = LocationsData.LocationA.SequenceNum,
                    HasEmployees = LocationsData.LocationA.HasEmployees,
                    Buildings = LocationsData.LocationA.Buildings,
                    EmployeeClassDetails = LocationsData.LocationA.EmployeeClassDetails
                }
            };
            var selectedPolicyDetails = policyDetails ?? PolicyTestData.CLPolicyDetails;

            return MapAllToCommercialLineData(
                selectedAddress,
                selectedBusinessDetails,
                selectedPersonalInfo,
                selectedLocationDetails,
                selectedPolicyDetails
            );
        }

        private static CommercialLineData MapAllToCommercialAutoData(
            Address propertyAddress,
            Address? mailingAddress,
            BusinessDetails businessDetails,
            PersonalInfoModel personalInfo,
            List<VehicleModel> vehicles,
            List<LocationDetails> locationDetails,
            PolicyDetails policyDetails,
            List<CommercialLoss>? losses)
        {
            var data = new CommercialLineData();
            CommercialLineDataMapper.MapAutoAddresses(data, propertyAddress, mailingAddress);
            CommercialLineDataMapper.MapBusinessDetails(data, businessDetails);
            CommercialLineDataMapper.MapPersonalInfo(data, personalInfo);
            CommercialLineDataMapper.MapVehicles(data, vehicles);
            CommercialLineDataMapper.MapLocationDetails(data, locationDetails);
            CommercialLineDataMapper.MapPolicyDetails(data, policyDetails);
            CommercialLineDataMapper.MapLosses(data, losses);
            return data;
        }

        /// <summary>
        /// Builds Commercial Auto (Products = ["AUTO"]) application data. Mirrors
        /// <see cref="GetCommercialLineData"/> but adds vehicles, an optional mailing address,
        /// and optional commercial losses. Defaults reproduce the Progressive DHUB VIN scenario.
        /// </summary>
        public static CommercialLineData GetCommercialAutoData(
            Address? propertyAddress = null,
            Address? mailingAddress = null,
            BusinessDetails? businessDetails = null,
            PersonalInfoModel? personalInfo = null,
            List<VehicleModel>? vehicles = null,
            List<LocationDetails>? locationDetails = null,
            PolicyDetails? policyDetails = null,
            List<CommercialLoss>? losses = null)
        {
            var selectedPropertyAddress = propertyAddress ?? AddressData.NY_StatenIsland;
            var selectedMailingAddress = mailingAddress ?? AddressData.AL_Huntsville;
            var selectedBusinessDetails = businessDetails ?? BusinessTestData.CommercialAutoBusiness;
            var selectedPersonalInfo = personalInfo ?? PersonalInfo.GetRandomPersonalInfo();
            var selectedVehicles = vehicles?.Count > 0 ? vehicles : [Vehicles.CommercialAuto_001AN4GY3MM021769];
            var selectedLocationDetails = locationDetails ?? new List<LocationDetails>
            {
                new LocationDetails
                {
                    Id = LocationsData.LocationA.Id,
                    SequenceNum = LocationsData.LocationA.SequenceNum,
                    HasEmployees = LocationsData.LocationA.HasEmployees,
                    Buildings = LocationsData.LocationA.Buildings,
                    EmployeeClassDetails = LocationsData.LocationA.EmployeeClassDetails
                }
            };
            var selectedPolicyDetails = policyDetails ?? PolicyTestData.CLPolicyDetails;
            var selectedLosses = losses ?? new List<CommercialLoss> { CommercialLosses.WcIncidentOnly };

            return MapAllToCommercialAutoData(
                selectedPropertyAddress,
                selectedMailingAddress,
                selectedBusinessDetails,
                selectedPersonalInfo,
                selectedVehicles,
                selectedLocationDetails,
                selectedPolicyDetails,
                selectedLosses
            );
        }
    }
}
