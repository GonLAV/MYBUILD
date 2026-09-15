using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;

public class BlockBindVehiclePageCL : InterviewBase
{
    public BlockBindVehiclePageCL(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

    protected override string PageIdentifier => "CL_BlockBindVehicle";
    protected override string PageName => "Block Bind Vehicle";
    private const string VinSubmitButtonLocator =
        "//*[contains(@class,'vehicle-search-block')]//app-button[@text='Submit']//button";

    public async Task SetVIN(string vin)
    {
        await PageHelper.InteractWithField(VIN, new ElementInteractionOptions
        {
            Value = vin,
            PressEnter = true,
            PressTab = false
        });

        var submit = Page.Locator($"xpath={VinSubmitButtonLocator}").First;
        await submit.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15000
        });
        await submit.ClickAsync();
    }
}