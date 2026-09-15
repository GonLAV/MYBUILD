using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

public class D2C_PaymentFailurePageFQ(
    IBrowserManager browserManager,
    IPageHelper pageHelper,
    IScopeContext scopeContext,
    bool validatePageReady = true,
    IAutomationLogger? logger = null
) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
{
    #region Page Properties
    protected override string PageIdentifier => "payment-failed";
    protected override string PageName => "Payment Failure Full Quote Page";
    #endregion
}