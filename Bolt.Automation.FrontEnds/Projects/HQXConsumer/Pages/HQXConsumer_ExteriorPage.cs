using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages
{
    public class HQXConsumer_ExteriorPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXConsumerBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "exterior";
        protected override string PageName => "Exterior Page";
    }
}