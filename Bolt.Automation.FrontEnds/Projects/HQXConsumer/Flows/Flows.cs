using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows
{
    public static class Flows
    {
        private static FlowsHelpers CreateHQXFlow(FlowType flowType, List<Type> pages, Dictionary<string, object?> defaultData)
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

        [FlowInitializer(FlowType.HQXShortFlow)]
        public static FlowsHelpers CreateShortFlow()
        {
            var pages = new List<Type>
            {
                typeof(HQXConsumer_OverviewPage),
                typeof(HQXConsumer_DetailsPage),
                typeof(HQXConsumer_DiscountsPage),
                typeof(HQXConsumer_OwnerPage),
                typeof(HQXConsumer_RatesPage)
            };
            return CreateHQXFlow(
                FlowType.HQXShortFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.HQXConsumer, LobType.Home)
            );
        }

        [FlowInitializer(FlowType.HQXLongFlow)]
        public static FlowsHelpers CreateLongFlow()
        {
            var pages = new List<Type>
            {
                typeof(HQXConsumer_OverviewPage),
                typeof(HQXConsumer_DetailsPage),
                typeof(HQXConsumer_PropertyPage),
                typeof(HQXConsumer_ExteriorPage),
                typeof(HQXConsumer_InteriorPage),
                typeof(HQXConsumer_DiscountsPage),
                typeof(HQXConsumer_OwnerPage),
                typeof(HQXConsumer_RatesPage)
            };
            return CreateHQXFlow(
                FlowType.HQXLongFlow,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.HQXConsumer, LobType.Home)
            );
        }

        [FlowInitializer(FlowType.MPQ3Renters)]
        public static FlowsHelpers CreateMPQ3RentersFlow()
        {
            var pages = new List<Type>
            {
                typeof(HQXConsumer_EditAddressPage)
            };
            return CreateHQXFlow(
                FlowType.MPQ3Renters,
                pages,
                FlowDefaultsProvider.GetFlowDefaults(FrontEndType.HQXConsumer, LobType.Renters)
            );
        }
    }
}
