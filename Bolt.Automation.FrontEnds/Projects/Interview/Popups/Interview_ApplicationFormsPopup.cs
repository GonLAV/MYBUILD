using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Popups
{
    public class Interview_ApplicationFormsPopup : InterviewPopupBase
    {
        public Interview_ApplicationFormsPopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PopupIdentifier => "Application Forms";
        protected override string PopupName => "Application Forms Popup";

        private const string AcordFormsListLocator = "//div[@class = 'form-name']";

        public async Task<List<string>> GetAcordFormsList()
        {
            var items = Page.Locator(AcordFormsListLocator);
            var count = await items.CountAsync();
            var formNames = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var text = await items.Nth(i).InnerTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                    formNames.Add(text.Trim());
            }

            _logger?.Info($"Found {formNames.Count} ACORD forms: {string.Join(", ", formNames)}");
            return formNames;
        }
    }
}