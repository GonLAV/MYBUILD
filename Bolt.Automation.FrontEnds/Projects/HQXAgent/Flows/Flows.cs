using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Flows
{
    public static class Flows
    {
        private static FlowsHelpers CreatePAAFlow(FlowType flowType, List<Type> pages, Dictionary<string, object?> defaultData)
        {
            var flow = new FlowsHelpers
            {
                FlowType = flowType,
                Pages = pages,
                DefaultData = defaultData,
            };
            FlowsHelpers.FlowRegistry.Register(flowType, flow);
            return flow;
        }

        [FlowInitializer(FlowType.PgrHomeFlow)]
        public static FlowsHelpers CreatePgrHomeFlow()
        {
            var pages = new List<Type>
            {
                typeof(HQXAgent_CustomerIntroPage),
                typeof(HQXAgent_OverviewPage),
                typeof(HQXAgent_TriagePage),
                typeof(HQXAgent_ExteriorPage),
                typeof(HQXAgent_InteriorPage),
                typeof(HQXAgent_OwnerPage),
                typeof(HQXAgent_DiscountsPage),
                typeof(HQXAgent_FinalDetailsPage),
                typeof(HQXAgent_SelectedCarrierPage),
                // The bridge legs. Safe to append: the executor walks only as far as the requested end
                // page, so callers ending at Selected Carrier are unaffected.
                typeof(HQXAgent_PrefillVerificationPage),
                typeof(HQXAgent_CarrierQuestionsPage)
            };
            return CreatePAAFlow(
                FlowType.PgrHomeFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.HQXAgent, LobType.Home)
            );
        }
    }
}
