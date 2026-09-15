using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Popups
{
    /// <summary>
    /// Email Quotes Popup - Send quote details via email.
    /// Use FieldRegistry with EmailQuote fields and FillForm pattern for form interactions.
    /// </summary>
    public class Interview_EmailQuotesPopup : InterviewPopupBase
    {
        public Interview_EmailQuotesPopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PopupIdentifier => "email-quotes";
        protected override string PopupName => "Email Quotes Popup";
    }
}
