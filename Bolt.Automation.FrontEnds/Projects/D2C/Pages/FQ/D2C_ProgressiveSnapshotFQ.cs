using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_ProgressiveSnapshotFQ : D2CBase
    {

        #region Page Properties
        protected override string PageIdentifier => "progressive-snapshot";
        protected override string PageName => "progressive snapshot Full Quote Page";
        #endregion

        public D2C_ProgressiveSnapshotFQ(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
    : base(browserManager, pageHelper, scopeContext, validatePageReady, logger){}

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
           await SelectSnapshot("Do Not Participate");
        }

        public async Task SelectSnapshot(string snapshotOption)
        {
            var snapshotLocator = $"//input/following::span[text() = '{snapshotOption}']";
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                snapshotLocator,
                ElementAction.Click
            );

            // Select agree to terms if "Mobile App" is selected
            if (snapshotOption.Equals("Mobile App", StringComparison.OrdinalIgnoreCase))
            {
                var agreeCheckboxLocator = "//app-checkbox//input[@id = 'FQData.ProgressivePersonalAuto.UBIAcknowledgement']";
                var checkboxExists = await PageHelper.ElementExists(LocatorType.XPath, agreeCheckboxLocator);

                if (checkboxExists)
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        agreeCheckboxLocator,
                        ElementAction.Check
                    );
                    await Task.Delay(2000);
                }
            }
        }
    }
}
