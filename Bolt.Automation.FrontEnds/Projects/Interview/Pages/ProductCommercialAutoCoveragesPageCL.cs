using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;
    public class ProductCommercialAutoCoveragesPageCL : InterviewBase
{
    public ProductCommercialAutoCoveragesPageCL(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "CL_CommercialAutoCoverages";
    protected override string PageName => "Commercial Auto Coverages Page";
}
