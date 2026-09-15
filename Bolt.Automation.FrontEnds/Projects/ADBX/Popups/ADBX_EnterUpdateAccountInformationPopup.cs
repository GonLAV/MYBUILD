using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Popups
{
    public class ADBX_EnterUpdateAccountInformationPopup(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePopup(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "adbx";

        protected override string PopupName => "Account Information";
        private const string CommercialLineOfBusiness = "Commercial";
        public async Task FillForm(bool selectCommercialLineOfBusiness, Dictionary<string, string>? formData = null)
        {
            if (selectCommercialLineOfBusiness)
            {
                _logger?.Info("Selecting Commercial line of business.");
                formData ??= new Dictionary<string, string>();
                formData[ADBX_FieldNames.AccountBusinessLine] = CommercialLineOfBusiness;
            }
            await base.FillForm(formData);
        }

        public async Task RemoveTcpaConsent(List<string> consentsToRemove)
        {
            foreach (var consent in consentsToRemove)
            {
                var xpath = $"//app-dropdown//span[text()='{consent}']/following-sibling::span";
                var locator = CreateLocator(xpath);
                await TryClickElement(locator);
            }
        }
        public async Task<List<string>> GetTcpaConsents()
        {
            var locator = CreateLocator("label.tcpa-label span.ng-value-label");
            var consentTexts = new List<string>();

            var elements = await locator.AllAsync();
            
            foreach (var element in elements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    consentTexts.Add(text.Trim());
                }
            }

            return consentTexts;
        }
    }
}
