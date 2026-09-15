using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages
{
    public class HQXConsumer_DetailsPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXConsumerBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "details";
        protected override string PageName => "Details Page";

        public ILocator WhyCantEditThisLink =>
            Page.Locator("popup-link[splunklabel='WhyCantIEditThis'] button");

        public ILocator WhyCantEditThisPopupContent =>
            Page.Locator("popup-wrapper p.info");

        public ILocator RoofResponsibleLabel =>
            Page.Locator("label[for='RoofResponsible_label']");

        public ILocator RoofResponsibleCheckboxText =>
            Page.Locator("label[for='RoofResponsible_label'] .custom-checkbox-label");

        public ILocator RoofResponsibleSubLabel =>
            Page.Locator("label[for='RoofResponsible_label'] .question-sub-label");

        // Shown when RoofResponsible is checked (HO3 → HO6)
        public ILocator RoofResponsibleInfoBoxHO6 =>
            Page.Locator(".extra-messaging.roof-resp-disclaimer");

        // Shown when RoofResponsible is unchecked (HO6 → HO3)
        public ILocator RoofResponsibleInfoBoxHO3 =>
            Page.Locator(".extra-messaging:not(.roof-resp-disclaimer)");
    }
}