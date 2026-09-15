using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using PlaywrightValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages
{
    public class ADBX_UpdateLeadSourcePage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePage(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "leads-sources/";
        protected override string PageName => "Update Lead Source Page";

        /// <summary>Rendered when two journeys of different types share a Line of Business.</summary>
        public const string SameLineOfBusinessWarning = "*A D2C flow can be connected to only one journey type";

        /// <summary>Rendered when Internal and Farmers journeys meet on one source, whatever their Lines of Business.</summary>
        public const string OneJourneyTypePerSourceWarning = "*Only one D2C journey type is allowed per source.";

        // The journeys the Consumer Journeys picker offers on farmersdhub. Labels read
        // "{Description} {Journey Type}": the three Homeowners entries share a Line of Business and
        // differ only by type, which is what makes the same-LOB rule testable; Dwelling Fire is Farmers
        // on a different LOB, which isolates the Internal-vs-Farmers rule.
        public const string HomeownersFarmers = "Homeowners Farmers";
        public const string HomeownersInternal = "Homeowners Internal";
        public const string HomeownersExternal = "Homeowners External";
        public const string DwellingFireFarmers = "Dwelling Fire Farmers";

        /// <summary>
        /// Waits until the stored journeys have been bound to the field. The control renders before its
        /// values arrive, so acting straight after a page load sees an empty field that is merely
        /// unpopulated - which would silently turn a clear into a no-op.
        /// </summary>
        public async Task WaitForJourneysLoadedAsync() =>
            await PageHelper.WaitForElementAsync(
                Page.Locator((string)FieldRegistryADBX.Fields[LeadSourceJourneysLoaded].Locators),
                DefaultTimeout,
                waitForVisibility: true);

        public async Task<IReadOnlyList<string>> GetAttachedJourneysAsync()
        {
            var chips = Page.Locator((string)FieldRegistryADBX.Fields[LeadSourceJourneyChips].Locators);
            var labels = (await chips.AllTextContentsAsync())
                .Select(label => label.Trim())
                .Where(label => !string.IsNullOrEmpty(label))
                .ToList();

            _logger?.Info($"Consumer Journeys holds {labels.Count} journey(s): {string.Join(", ", labels)}");
            return labels;
        }

        /// <summary>
        /// Opens the picker from the search input rather than the field body. Clicking the field lands
        /// on whatever sits at its centre, which with chips attached can be a chip's remove icon - the
        /// same trap <see cref="CloseJourneyPickerAsync"/> avoids on the way out.
        /// </summary>
        public async Task OpenJourneyPickerAsync()
        {
            await PageHelper.InteractWithField(LeadSourceJourneysSearchInput);
            await PageHelper.WaitForElementAsync(
                Page.Locator((string)FieldRegistryADBX.Fields[LeadSourceJourneyPicker].Locators),
                DefaultTimeout,
                waitForVisibility: true);
        }

        /// <summary>
        /// Closes the picker and waits for the panel to go, so the next read or re-open cannot race a
        /// panel that is still up - left unverified, that surfaces later as a missing option rather
        /// than as a failure to close.
        /// </summary>
        /// <remarks>
        /// Escape on the field, not a second click on it: clicking would toggle the panel shut just as
        /// well, but with chips present the click can land on a chip's remove button and silently
        /// detach a journey the test believes is still attached.
        /// </remarks>
        public async Task CloseJourneyPickerAsync()
        {
            var journeysField = FieldRegistryADBX.Fields[LeadSourceConsumerJourneys];

            await PageHelper.InteractWithElement(
                journeysField.Strategy,
                journeysField.Locators,
                ElementAction.Press,
                new ElementInteractionOptions { Value = "Escape", Timeout = DefaultTimeout });

            await PageHelper.WaitForElementToDisappearAsync(
                Page.Locator((string)FieldRegistryADBX.Fields[LeadSourceJourneyPicker].Locators),
                DefaultTimeout);
        }

        public async Task SelectJourneyAsync(string journeyLabel)
        {
            _logger?.Info($"Selecting journey '{journeyLabel}' from the Consumer Journeys picker.");

            await OpenJourneyPickerAsync();
            await PageHelper.InteractWithField(LeadSourceJourneyOption, journeyLabel);
            await CloseJourneyPickerAsync();
        }

        /// <summary>
        /// Detaches every journey, giving a test a known-empty starting point instead of depending on
        /// whatever the shared lead source currently holds. Nothing is saved until CONFIRM.
        /// </summary>
        public async Task ClearAllJourneysAsync()
        {
            _logger?.Info("Clearing every attached Consumer Journey.");
            await PageHelper.InteractWithField(LeadSourceJourneysClearAll);

            var chips = Page.Locator((string)FieldRegistryADBX.Fields[LeadSourceJourneyChips].Locators);
            await PageHelper.WaitForElementToDisappearAsync(chips.First, DefaultTimeout);
        }

        public async Task<string?> GetJourneysWarningAsync()
        {
            if (!await PageHelper.ElementExists(LeadSourceJourneysWarning, DefaultTimeout))
            {
                _logger?.Info("No Consumer Journeys warning is displayed.");
                return null;
            }

            var warning = (await PageHelper.GetFieldValue(
                LeadSourceJourneysWarning, PlaywrightValueType.Text, DefaultTimeout, waitForVisibility: true)).Trim();

            _logger?.Info($"Consumer Journeys warning displayed: '{warning}'");
            return warning;
        }

        /// <summary>
        /// Discards every pending edit and returns to the grid. This form is never saved by tests.
        /// </summary>
        /// <remarks>
        /// Waiting for the form to go away is what makes the returned page trustworthy: this form and
        /// the grid share the /leads-sources route, so the grid page's URL readiness check matches just
        /// as well while the form is still on screen. The form's own field disappearing is the only
        /// signal that it actually closed - without this wait, a Cancel that did nothing would still
        /// hand back a grid page that validates.
        /// </remarks>
        public async Task<ADBX_LeadsSourcesPage> CancelAsync(IPageFactory pageFactory)
        {
            _logger?.Info("Cancelling the Update Lead Source form - no changes are saved.");
            await PageHelper.InteractWithField(LeadSourceCancelButton);

            await PageHelper.WaitForElementToDisappearAsync(
                Page.Locator((string)FieldRegistryADBX.Fields[LeadSourceConsumerJourneys].Locators),
                DefaultTimeout);

            return pageFactory.CreatePage<ADBX_LeadsSourcesPage>();
        }
    }
}
