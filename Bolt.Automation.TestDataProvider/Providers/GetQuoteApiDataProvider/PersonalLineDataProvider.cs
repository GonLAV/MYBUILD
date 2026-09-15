using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.Models.TestData.Interview.Models;
using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using static Bolt.Automation.TestDataProvider.TestData.PageSkippingTestData.PageSkipping;

namespace Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider
{
    public static class PersonalLineDataProvider
    {
        private static PersonalLineData MapAllToPersonalHomeData(
            Address address,
            PersonalInfoModel personalInfo,
            HomeDetails homeDetails,
            HomeFeaturesDetails homeFeatures,
            PolicyDetails policyDetails,
            CustomFieldsModel customFields)
        {
            var data = new PersonalLineData();
            PersonalLineDataMapper.MapAddress(data, address);
            PersonalLineDataMapper.MapPersonalInfo(data, personalInfo);
            PersonalLineDataMapper.MapHomeDetails(data, homeDetails);
            PersonalLineDataMapper.MapHomeFeatures(data, homeFeatures);
            PersonalLineDataMapper.MapPolicyDetails(data, policyDetails);
            PersonalLineDataMapper.MapCustomFields(data, customFields);
            return data;
        }

        private static PersonalLineData MapAllToPersonalAutoData(
            Address address,
            PersonalInfoModel personalInfo,
            List<DriverModel> drivers,
            List<VehicleModel> vehicles,
            PolicyDetails policyDetails,
            bool assignDriversToVehicles = false)
        {
            var data = new PersonalLineData();
            PersonalLineDataMapper.MapAddress(data, address);
            PersonalLineDataMapper.MapPersonalInfo(data, personalInfo);
            PersonalLineDataMapper.MapAutoDrivers(data, drivers);
            PersonalLineDataMapper.MapAutoVehicles(data, vehicles);
            PersonalLineDataMapper.MapAutoPolicyDetails(data, policyDetails);

            if (assignDriversToVehicles)
                AssignDriversToVehicles(data.PersonalVehicles, data.Drivers);
            return data;
        }

        private static void AssignDriversToVehicles(List<VehicleModel> vehicles, List<DriverModel> drivers)
        {
            if (vehicles == null || drivers == null) return;
            int driverCount = drivers.Count;
            for (int i = 0; i < vehicles.Count; i++)
            {
                var assignments = new List<AssignmentModel>();
                for (int j = 0; j < driverCount; j++)
                {
                    var driver = drivers[j];
                    assignments.Add(new AssignmentModel
                    {
                        SequenceNum = driver.SequenceNum,
                        Id = driver?.Id,
                        DriverId = driver?.Id,
                        Percent = j == i % driverCount ? "100" : "0"
                    });
                }
                vehicles[i].Assignments = assignments;
            }
        }

        public static PersonalLineData GetPersonalHomeData(
                Address? address = null,
                PersonalInfoModel? personalInformation = null,
                HomeDetails? homeDetails = null,
                HomeFeaturesDetails? homeFeatures = null,
                PolicyDetails? policyDetails = null,
                CustomFieldsModel? customFields = null
              )
        {
            var personalInfo = personalInformation ?? PersonalInfo.GetRandomPersonalInfo();
            var homeDetailsData = homeDetails ?? HomeDetailsTestData.PersonalHomeDetailsData;
            var homeFeaturesData = homeFeatures ?? HomeFeaturesTestData.PersonalHomeFeaturesData;
            var policyData = policyDetails ?? PolicyTestData.PersonalHomePolicyDetails;
            var customFieldsData = customFields ?? new CustomFieldsModel();
            return MapAllToPersonalHomeData(
                address ?? AddressData.AL_Adger,
                personalInfo,
                homeDetailsData,
                homeFeaturesData,
                policyData,
                customFieldsData);
        }

        public static PersonalLineData GetPersonalAutoData(
            Address? address = null,
            List<VehicleModel>? vehicles = null,
            List<DriverModel>? drivers = null,
            PersonalInfoModel? personalInformation = null,
            PolicyDetails? policyDetails = null,
            bool assignDriversToVehicles = false)
        {
            var personalInfo = personalInformation ?? PersonalInfo.GetRandomPersonalInfo();
            var selectedVehicles = vehicles?.Count > 0 ? vehicles : [Vehicles.SCFAD02E19GB11912];
            var selectedDrivers = drivers?.Count > 0 ? drivers : [Drivers.BobSmith, Drivers.KateSmith];
            var policyData = policyDetails ?? PolicyTestData.PersonalAutoPolicyData;
            return MapAllToPersonalAutoData(
                address ?? AddressData.AL_Adger,
                personalInfo,
                selectedDrivers,
                selectedVehicles,
                policyData, 
                assignDriversToVehicles);
        }

        /// <summary>
        /// Gets PersonalAutoData for the Missing Additional Drivers scenario
        /// This creates an application with a secondary driver missing DOB
        /// </summary>
        public static PersonalLineData GetMissingAdditionalDriversData(
            Address? address = null,
            List<DriverModel>? drivers = null,
            List<VehicleModel>? vehicles = null,
            PersonalInfoModel? personalInformation = null,
            PolicyDetails? policyDetails = null,
            bool assignDriversToVehicles = false)
        {
            var personalInfo = personalInformation ?? MissingAdditionalDrivers.PersonalInformation;
            var selectedVehicles = vehicles ?? MissingAdditionalDrivers.GetAllVehicles();
            var selectedDrivers = drivers ?? MissingAdditionalDrivers.GetAllDrivers();
            var policyData = policyDetails ?? MissingAdditionalDrivers.PolicyData;
            return MapAllToPersonalAutoData(
                address ?? AddressData.TX,
                personalInfo,
                selectedDrivers,
                selectedVehicles,
                policyData,
                assignDriversToVehicles);
        }

        /// <summary>
        /// Gets PersonalAutoData for the Multi-LOB Page Skipping scenario
        /// This creates an application with both missing driver DOB and incomplete vehicle data
        /// Uses only first 2 vehicles for multi-LOB testing
        /// </summary>
        public static PersonalLineData GetMultiLobPageSkippingData(
            Address? address = null,
            List<DriverModel>? drivers = null,
            List<VehicleModel>? vehicles = null,
            PersonalInfoModel? personalInformation = null,
            PolicyDetails? policyDetails = null,
            bool assignDriversToVehicles = false)
        {
            var personalInfo = personalInformation ?? MissingAdditionalDrivers.PersonalInformation;
            // Use only first 2 vehicles for multi-LOB scenario
            var selectedVehicles = vehicles ?? MissingAdditionalDrivers.GetMultiLobVehicles();
            var selectedDrivers = drivers ?? MissingAdditionalDrivers.GetAllDrivers();
            var policyData = policyDetails ?? MissingAdditionalDrivers.PolicyData;
            return MapAllToPersonalAutoData(
                address ?? AddressData.TX,
                personalInfo,
                selectedDrivers,
                selectedVehicles,
                policyData,
                assignDriversToVehicles);
        }

        /// <summary>
        /// Gets PersonalAutoData for the Driver Losses Page Skipping scenario
        /// This creates an application with a driver that has accidents, violations, and losses
        /// to trigger the Driver History page
        /// </summary>
        public static PersonalLineData GetDriverLossesPageSkippingData(
        Address? address = null,
        List<DriverModel>? drivers = null,
        List<VehicleModel>? vehicles = null,
        PersonalInfoModel? personalInformation = null,
        PolicyDetails? policyDetails = null,
        bool assignDriversToVehicles = false)
        {
            var personalInfo = personalInformation ?? MissingAdditionalDrivers.PersonalInformation;
            // Use only one vehicle for driver losses scenario
            var selectedVehicles = vehicles ?? MissingAdditionalDrivers.GetSingleVehicle();
            // Use driver with losses for this scenario
            var selectedDrivers = drivers ?? MissingAdditionalDrivers.GetDriverWithLosses();
            var policyData = policyDetails ?? MissingAdditionalDrivers.PolicyData;
            return MapAllToPersonalAutoData(
                address ?? AddressData.TX,
                personalInfo,
                selectedDrivers,
                selectedVehicles,
                policyData,
                assignDriversToVehicles);
        }

        /// <summary>
        /// Creates a Progressive Homeowners scenario with PGR-specific details.
        /// Uses GAThomaston address by default. Parameters follow GetPersonalHomeData convention.
        /// </summary>
        public static PersonalLineData GetPersonalDataProgressive(
            Address? address = null,
            PersonalInfoModel? personalInformation = null,
            HomeDetails? homeDetails = null,
            HomeFeaturesDetails? homeFeatures = null,
            PolicyDetails? policyDetails = null,
            CustomFieldsModel? customFields = null,
            IEnumerable<CarrierEnums>? rankingCarrierOverrides = null)
        {
            var personalInfo = personalInformation ?? PersonalInfo.GetRandomPersonalInfoPGR();
            var homeDetailsData = homeDetails ?? HomeDetailsTestData.PGRHomeDetails;
            var homeFeaturesData = homeFeatures ?? HomeFeaturesTestData.PGRHomeFeatureDetails;
            var policyData = policyDetails ?? PolicyTestData.PGRPolicyDetails;
            var customFieldsData = customFields ?? CustomFields.PGRCustomFields;

            if (rankingCarrierOverrides is not null)
            {
                // Copy first: PGRCustomFields is a shared static, so setting the field on it would leak
                // the override into every other Progressive quote in the run.
                customFieldsData = customFieldsData.Copy();
                customFieldsData.RankingCarrierOverrides =
                    string.Join(",", rankingCarrierOverrides.Select(ToRankingOverrideName));
            }
            return MapAllToPersonalHomeData(
                address ?? AddressData.OH,
                personalInfo,
                homeDetailsData,
                homeFeaturesData,
                policyData,
                customFieldsData);
        }

        // Ranking keys TowerHill by its full cobranded name; overriding plain "TowerHill" is silently
        // ignored (confirmed live on QA — still removed by ranking).
        private static string ToRankingOverrideName(CarrierEnums carrier) => carrier switch
        {
            CarrierEnums.TowerHill => "TowerHillPrimeInsurance",
            _ => carrier.ToString()
        };
    }
}
