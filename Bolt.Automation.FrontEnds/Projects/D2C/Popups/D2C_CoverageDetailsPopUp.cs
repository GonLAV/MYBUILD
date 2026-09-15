using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Popups
{
    public class D2C_CoverageDetailsPopUp(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : D2CPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string EntityNameLocator = "div.entity-name";
        private const string LobCoverageNameLocator = "h3.coverage-section-title";
        #endregion

        protected override string PopupIdentifier => "coverage";
        protected override string PopupName => "Coverage Details Popup";


        public async Task<List<string>> GetEntityNames()
        {
            var nameElements = await Page.Locator(EntityNameLocator).AllAsync();
            var names = new List<string>();

            foreach (var element in nameElements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    names.Add(text);
                }
            }

            return names;
        }

        public async Task<List<string>> GetPolicyLobs()
        {
            var lobElements = await Page.Locator(LobCoverageNameLocator).AllAsync();
            var lobs = new List<string>();

            foreach (var element in lobElements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    lobs.Add(text);
                }
            }

            return lobs;
        }
    }
}