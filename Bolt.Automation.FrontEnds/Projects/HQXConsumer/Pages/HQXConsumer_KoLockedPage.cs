using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages
{
    public class HQXConsumer_KoLockedPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXConsumerBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "KickOutLocked";
        protected override string PageName => "Thanks for coming back!";
        public async Task<string> GetTitleTextAsync()
        {
            var text = await Page.Locator(".title-container h1").InnerTextAsync();
            return Normalize(text);
        }

        public async Task<string> GetMessageTextAsync()
        {
            var text = await Page.Locator(".title-container p").InnerTextAsync();
            return Normalize(text);
        }

        public async Task<string> GetWorkHoursTextAsync()
        {
            var text1 = await Page.Locator(".main-content p").First.InnerTextAsync();
            var text2 = await Page.Locator(".main-content .workHours").InnerTextAsync();

            return Normalize(text1 + text2);
        }

        public async Task<string> GetProgressiveLinkAsync()
        {
            var link = Page.Locator(".main-content a.button");
            var text = await link.InnerTextAsync();
            return Normalize(text);
        }

        private static string Normalize(string text) =>
            string.Join(" ", text.Replace("\u00A0", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
