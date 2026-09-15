using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Flows
{
    public static class Flows
    {
        private static FlowsHelpers CreateD2CFlow(FlowType flowType, List<Type> pages, Dictionary<string, object?> defaultData)
        {
            var flow = new FlowsHelpers
            {
                FlowType = flowType,
                Pages = pages,
                DefaultData = defaultData
            };

            FlowsHelpers.FlowRegistry.Register(flowType, flow);
            return flow;
        }

        [FlowInitializer(flowType: FlowType.D2CAutoFlow)]
        public static FlowsHelpers D2CAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PrimaryDriverPage),
                typeof(D2C_AdditionalDriversPage),
                typeof(D2C_VehiclesPage),
                typeof(D2C_DriverHistoryPage),
                typeof(D2C_CoveragesPage),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.D2CHomeFlow)]
        public static FlowsHelpers D2CHomeFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PropertiesPage),
                typeof(D2C_PrimaryResidencePage),
                typeof(D2C_PropertiesUsagePage),
                typeof(D2C_HouseDetailsPage),
                typeof(D2C_RoofReplacementPage),
                typeof(D2C_SafetyAlarms),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CHomeFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home)
            );
        }
        [FlowInitializer(flowType:FlowType.D2CAutoHomeFlow)]

        public static FlowsHelpers D2CAutoHomeFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PrimaryDriverPage),
                typeof(D2C_AdditionalDriversPage),
                typeof(D2C_VehiclesPage),
                typeof(D2C_DriverHistoryPage),
                typeof(D2C_CoveragesPage),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_HouseDetailsPage),
                typeof(D2C_RoofReplacementPage),
                typeof(D2C_SafetyAlarms),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CAutoHomeFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Auto, LobType.Home)
            );
        }
        [FlowInitializer(flowType:FlowType.D2CHomeAutoFlow)]

        public static FlowsHelpers D2CHomeAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PropertiesPage),
                typeof(D2C_PrimaryResidencePage),
                typeof(D2C_PropertiesUsagePage),
                typeof(D2C_HouseDetailsPage),
                typeof(D2C_RoofReplacementPage),
                typeof(D2C_SafetyAlarms),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_AdditionalDriversPage),
                typeof(D2C_VehiclesPage),
                typeof(D2C_DriverHistoryPage),
                typeof(D2C_CoveragesPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CHomeAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.D2CRentersFlow)]
        public static FlowsHelpers D2CRentersFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PropertiesPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CRentersFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Renters)
            );
        }

        [FlowInitializer(flowType: FlowType.D2CCondoFlow)]
        public static FlowsHelpers D2CCondoFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PropertiesPage),
                typeof(D2C_PrimaryResidencePage),
                typeof(D2C_PropertiesUsagePage),
                typeof(D2C_HouseDetailsPage),
                typeof(D2C_SafetyAlarms),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CCondoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home)
            );
        }
        [FlowInitializer(flowType: FlowType.D2CCondoAutoFlow)]
        public static FlowsHelpers D2CCondoAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PropertiesPage),
                typeof(D2C_PrimaryResidencePage),
                typeof(D2C_PropertiesUsagePage),
                typeof(D2C_HouseDetailsPage),
                typeof(D2C_SafetyAlarms),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_AdditionalDriversPage),
                typeof(D2C_VehiclesPage),
                typeof(D2C_DriverHistoryPage),
                typeof(D2C_CoveragesPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.D2CCondoAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.PetsFlow)]
        public static FlowsHelpers PetsFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_LobsPage),
                typeof(D2C_PetsPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.PetsFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Pets)
            );
        }

        [FlowInitializer(flowType: FlowType.SafecoAutoFQFlow)]
        public static FlowsHelpers AutoFQSafeco()
        {
            var pages = new List<Type>
            {
                typeof(D2C_SafecoPolicyDpolicyFQ),
                typeof(D2C_DriverDetailsFQ),
                //typeof(D2C_MissingDriversPageFQ),
                typeof(D2C_VehiclesDetailsPageFQ),
                typeof(D2C_QuoteConfirmationPage),
                typeof(D2C_PaymentPlanPageFQ),
                typeof(D2C_PaymentPageFQ)
            };

            return CreateD2CFlow(
                FlowType.SafecoAutoFQFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.USAAAutoFQFlow)]
        public static FlowsHelpers USAAFQPgrAuto()
        {
            var pages = new List<Type>
            {
                typeof(D2C_ProgressiveDisclosurePageFQ),
                typeof(D2C_ProgressiveSnapshotFQ),
                typeof(D2C_ProgressiveVehiclesFQ),
                typeof(D2C_ProgressiveDriversFQ),
                typeof(D2C_ProgressiveAdditionalQuestionsFQ),
                typeof(D2C_ProgressiveCoveragesFQ),
                typeof(D2C_QuoteConfirmationPage),
                typeof(D2C_PaymentPlanPageFQ),
                typeof(D2C_PaymentPageFQ),
            };

            return CreateD2CFlow(
                FlowType.USAAAutoFQFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.BristolWestAutoFQFlow)]
        public static FlowsHelpers BristolWestAutoFQFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_BristolWestPolicyFQ),
                typeof(D2C_BristolWestVehiclesFQ),
                typeof(D2C_BristolWestDriversFQ),
                typeof(D2C_QuoteConfirmationPage),
            };

            return CreateD2CFlow(
                FlowType.BristolWestAutoFQFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.StillwaterHomeFQFlow)]
        public static FlowsHelpers StillwaterHomeFQFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_StillwaterPropertyInformationPage),
                typeof(D2C_StillwaterAdditionalPropertyPage),
                typeof(D2C_StillwaterPropertySafetyPage),
                typeof(D2C_StillwaterPropertyFeaturesPage),
                typeof(D2C_StillwaterAdditionalFeaturesPage),
                typeof(D2C_StillwaterPropertyUtilitiesPage),
                typeof(D2C_StillwaterCurrentInsurancePage),
                typeof(D2C_StillwaterCarrierDisclosurePage),
                typeof(D2C_QuoteConfirmationPage),
                typeof(D2C_PaymentPlanPageFQ),
                typeof(D2C_PaymentPageFQ),
            };

            return CreateD2CFlow(
                FlowType.StillwaterHomeFQFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home)
            );
        }

        [FlowInitializer(flowType: FlowType.LakeviewHomeFlow)]
        public static FlowsHelpers LakeviewHomeFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_YourAddressPage),
                typeof(D2C_PropertiesPage),
                typeof(D2C_PrimaryResidencePage),
                typeof(D2C_HouseDetailsPage),
                typeof(D2C_SafetyAlarms),
                typeof(D2C_CrossSellInformationPage),
                typeof(D2C_PersonalDetailsPage),
                typeof(D2C_PolicyDatePickerPage),
                typeof(D2C_RatesPage)
            };

            return CreateD2CFlow(
                FlowType.LakeviewHomeFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home)
            );
        }

        [FlowInitializer(flowType: FlowType.LemonadeFQFlow)]
        public static FlowsHelpers LemonadeFQFlow()
        {
            var pages = new List<Type>
            {
                typeof(D2C_QuoteConfirmationPage),
                typeof(D2C_PaymentPlanPageFQ),
                typeof(D2C_PaymentPageFQ),
            };

            return CreateD2CFlow(
                FlowType.LemonadeFQFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.D2C, LobType.Home)
            );
        }
    }
}