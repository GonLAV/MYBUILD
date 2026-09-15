using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Flows
{
    /// <summary>
    /// Flow definitions for Interview v3.
    /// </summary>
    public static class Flows
    {
        private static FlowsHelpers CreateInterviewFlow(FlowType flowType, List<Type> pages, Dictionary<string, object?> defaultData)
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

        [FlowInitializer(flowType: FlowType.InterviewHO3Flow)]
        public static FlowsHelpers InterviewHO3Flow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_LobsPage),
                typeof(Product_HomePage),
                typeof(Product_StructurePage),
                typeof(Product_FeaturesPage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewHO3Flow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Home)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewPLAgentFlow)]
        public static FlowsHelpers InterviewPLAgentFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_MarketsPage),
                typeof(Product_HomePage),
                typeof(Product_StructurePage),
                typeof(Product_FeaturesPage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewPLAgentFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Home)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewPLAgentAutoFlow)]
        public static FlowsHelpers InterviewPLAgentAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_MarketsPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewPLAgentAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewHO4Flow)]
        public static FlowsHelpers InterviewHO4Flow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_LobsPage),
                typeof(Product_HomePage),
                typeof(Product_StructurePage),
                typeof(Product_FeaturesPage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewHO4Flow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Renters)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewAutoFlow)]
        public static FlowsHelpers InterviewAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_LobsPage),
                typeof(Product_VehiclePage),
                typeof(Product_OperatorPage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewBundleFlow)]
        public static FlowsHelpers InterviewBundleFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_LobsPage),
                typeof(Product_HomePage),
                typeof(Product_StructurePage),
                typeof(Product_FeaturesPage),
                typeof(Product_VehiclePage),
                typeof(Product_OperatorPage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewBundleFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Home, LobType.Auto)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewFloodFlow)]
        public static FlowsHelpers InterviewFloodFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_LobsPage),
                typeof(Product_HomePage),
                typeof(Product_StructurePage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewFloodFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Flood)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewWCFlow)]
        public static FlowsHelpers InterviewWCFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_BusinessPage),
                typeof(Product_LocationsPage),
                typeof(Product_EmployeePage),
                typeof(Product_PolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewWCFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.WorkersCompensation)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewMotorcycleFlow)]
        public static FlowsHelpers InterviewMotorcyclelow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_LobsPage),
                typeof(Product_MotorcyclePage),
                typeof(Product_OperatorPage),
                typeof(Product_MotorcyclePolicyPage),
                typeof(Product_MotorcycleCoveragePage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewMotorcycleFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.Motorcycle)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewCLAutoFlow)]
        public static FlowsHelpers InterviewCLAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_MarketsPage),
                typeof(Product_BusinessPage),
                typeof(Product_VehiclePage),
                typeof(Product_OperatorPage),
                typeof(Product_CLPolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewCLAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.CommercialAuto)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewBOPOldFlow)]
        public static FlowsHelpers InterviewBOPOldFlow()
        {
            var pages = new List<Type>
            {
                typeof(Product_StartPage),
                typeof(Product_MarketsPage),
                typeof(Product_BusinessPage),
                typeof(Product_LocationsPage),
                typeof(Product_CLPolicyPage),
                typeof(Product_ApplicantPage),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewBOPOldFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.BusinessOwners)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewBoltAccessCLAutoFlow)]
        public static FlowsHelpers InterviewBoltAccessCLAutoFlow()
        {
            var pages = new List<Type>
            {
                typeof(ProductBusinessProfilePageCL),
                typeof(ProductSelectionPageCL),
                typeof(ProductMarketResultsPageCL),
                typeof(ProductInsuranceHistoryPageCL),
                typeof(ProductApplicantsAndDriversPageCL),
                typeof(ProductCommercialVehiclesPageCL),
                typeof(ProductCommercialAutoCoveragesPageCL),
                typeof(ProductMarketSelectionsPageCL),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewBoltAccessCLAutoFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.CommercialAuto)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewBOPFlow)]
        public static FlowsHelpers InterviewBOPFlow()
        {
            var pages = new List<Type>
            {
                typeof(ProductBusinessProfilePageCL),
                typeof(ProductSelectionPageCL),
                typeof(ProductMarketResultsPageCL),
                typeof(ProductInsuranceHistoryPageCL),
                typeof(Product_LocationsPage),
                typeof(Product_EmployeePage),
                typeof(ProductOwnersAndOfficersPageCL),
                typeof(ProductCoverageDetailsPageCL),
                typeof(ProductMarketSelectionsPageCL),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewBOPFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.BusinessOwners)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewBoltAccessGLFlow)]
        public static FlowsHelpers InterviewBoltAccessGLFlow()
        {
            var pages = new List<Type>
            {
                typeof(ProductBusinessProfilePageCL),
                typeof(ProductSelectionPageCL),
                typeof(ProductMarketResultsPageCL),
                typeof(ProductInsuranceHistoryPageCL),
                typeof(Product_LocationsPage),
                typeof(Product_EmployeePage),
                typeof(ProductOwnersAndOfficersPageCL),
                typeof(ProductCoverageDetailsPageCL),
                typeof(ProductMarketSelectionsPageCL),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewBoltAccessGLFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.GeneralLiability)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewWCFlowNew)]
        public static FlowsHelpers InterviewWCFlowNew()
        {
            // No separate Locations page - the single work location is captured on
            // ProductBusinessProfilePageCL; Insurance History goes straight to Employee, then
            // Owners/Officers (confirmed by walking the live flow against Unify Staging).
            var pages = new List<Type>
            {
                typeof(ProductBusinessProfilePageCL),
                typeof(ProductSelectionPageCL),
                typeof(ProductMarketResultsPageCL),
                typeof(ProductInsuranceHistoryPageCL),
                typeof(Product_EmployeePage),
                typeof(ProductOwnersAndOfficersPageCL),
                typeof(ProductCoverageDetailsPageCL),
                typeof(ProductMarketSelectionsPageCL),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewWCFlowNew,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.WorkersCompensation)
            );
        }

        [FlowInitializer(flowType: FlowType.InterviewCLAutoFlowNew)]
        public static FlowsHelpers InterviewCLAutoFlowNew()
        {
            var pages = new List<Type>
            {
                typeof(ProductBusinessProfilePageCL),
                typeof(ProductSelectionPageCL),
                typeof(ProductMarketResultsPageCL),
                typeof(ProductInsuranceHistoryPageCL),
                typeof(ProductApplicantsAndDriversPageCL),
                typeof(ProductCommercialVehiclesPageCL),
                typeof(ProductCommercialAutoCoveragesPageCL),
                typeof(ProductMarketSelectionsPageCL),
                typeof(Product_ResultsPage)
            };

            return CreateInterviewFlow(
                FlowType.InterviewCLAutoFlowNew,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.Interview, LobType.CommercialAuto)
            );
        }
    }
}
