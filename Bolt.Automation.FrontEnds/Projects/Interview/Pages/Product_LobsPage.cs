using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_LobsPage : InterviewBase
    {
        private const string LobsLocator = "//*[contains(@class,'PolicyData.Lobs[]')]//li";

        public Product_LobsPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "Product";

        protected override string PageName => "Product Selection Page";

        public async Task<List<string>> GetSelectedLobs()
        {
            var lobs = await Page.Locator(LobsLocator).AllAsync();
            var selectedLobs = new List<string>();

            foreach (var element in lobs)
            {
                var lobCheckbox = element.Locator("//input");
                if (await lobCheckbox.IsCheckedAsync())
                {
                    var text = await element.TextContentAsync();
                    if (text != null)
                    {
                        selectedLobs.Add(text.Trim());
                    }
                }
            }
            return selectedLobs;
        }
    }
}
