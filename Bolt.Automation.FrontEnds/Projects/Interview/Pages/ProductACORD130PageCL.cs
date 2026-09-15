using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;

/// <summary>
/// ACORD 130 (Workers' Compensation Application) operations/risk-exposure questionnaire. Renders
/// regardless of the AcordAppetiteWC Yes/No choice on Market Selections - that choice only
/// controls whether a pre-filled ACORD form gets generated for download, not whether this page
/// appears. No fields here are marked required in the DOM; Next is enabled without answering any.
/// </summary>
public class ProductACORD130PageCL : InterviewBase
{
    public ProductACORD130PageCL(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "CL_ACORD_ACORD130";
    protected override string PageName => "ACORD 130 - Operations and Risk Exposure";
}
