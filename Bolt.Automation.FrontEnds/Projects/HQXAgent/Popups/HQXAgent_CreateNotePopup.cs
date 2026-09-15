using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Base;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Popups
{
    public class HQXAgent_CreateNotePopup(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXAgentPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "app-create-note";
        protected override string PopupName => "Create Note Popup";

        // ── Lifecycle ──────────────────────────────────────────────────────────────────

        public override async Task ValidatePageReady()
        {
            var modal = Page.Locator("app-create-note");
            await modal.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15000
            });
            _logger?.Info("Create Note popup is visible.");
        }

        // ── Note type selection (initial modal) ────────────────────────────────────────

        /// <summary>
        /// Selects the note type radio button.
        /// </summary>
        /// <param name="noteType">
        /// The radio button id suffix: <c>"CallOutcomeNote"</c> or <c>"NewNote"</c>.
        /// </param>
        public async Task SelectNoteTypeAsync(string noteType)
        {
            _logger?.Info($"Selecting note type: {noteType}.");
            await PageHelper.InteractWithElement(NoteFields.Fields[NoteType][noteType]);
        }

        /// <summary>Selects the given action from the Action dropdown.</summary>
        public async Task SelectActionAsync(string action)
        {
            _logger?.Info($"Selecting action: {action}.");
            await PageHelper.InteractWithElement(NoteFields.Fields[ActionValue][action]);
            _logger?.Info($"Action '{action}' selected.");
        }

        // ── Submit ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Clicks the submit/continue button inside the Create Note popup.
        /// Overrides the base to avoid page-level strict-mode violations when
        /// multiple buttons exist outside the popup.
        /// </summary>
        public override async Task ClickContinue()
        {
            _logger?.Info("Submitting Create Note popup.");
            var popup = Page.Locator("app-create-note");
            // QA uses 'btn-submit'; UAT uses 'next-button'. Both use type="button", not type="submit".
            // The combined selector handles both environments without needing a runtime branch.
            var submitBtn = popup.Locator("button.btn-submit, button.next-button");

            await submitBtn.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });
            // DispatchEventAsync bypasses actionability / overlay checks and fires
            // the click event directly in the browser, which Angular handles correctly.
            await submitBtn.DispatchEventAsync("click");
            _logger?.Info("Create Note popup submitted.");
        }

        // ── Compound fill methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Fills the initial Create Note modal for a call-outcome or new note.
        /// Does not submit — call <see cref="ClickContinue"/> afterwards.
        /// </summary>
        public async Task FillCallOutcomeNoteAsync(string action, string product, string description)
        {
            await SelectActionAsync(action);
            await PageHelper.InteractWithElement(NoteFields.Fields[CallProductType][product]);
            await PageHelper.InteractWithElement(NoteFields.Fields[ActivityDescription][description]);
            _logger?.Info($"Filled call outcome note — Action: {action}, Product: {product}.");
        }

        /// <summary>
        // ── Confirmation handling ────────────────────────────────────────────────────

        /// <summary>
        /// Waits for the success confirmation state inside the Create Note popup
        /// (the "Your note has been saved!" message that appears after submission).
        /// </summary>
        public async Task WaitForSuccessConfirmationAsync()
        {
            _logger?.Info("Waiting for sold note success confirmation.");
            var confirmation = Page.Locator("app-create-note p").Filter(new LocatorFilterOptions { HasText = "Your note has been saved!" });
            await confirmation.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15000
            });
            _logger?.Info("Sold note success confirmation is visible.");
        }

        /// <summary>
        /// Clicks the "Close" button inside the success confirmation panel
        /// (<c>.note-footer button.btn-submit</c>) that appears after a note is saved.
        /// Avoids the generic <see cref="HQXAgentPopupBase.ClosePopup"/> selector, which would
        /// accidentally match the outer dialog's X button (<c>.close-btn</c>) instead.
        /// </summary>
        public async Task CloseSuccessConfirmationAsync()
        {
            _logger?.Info("Closing sold note success confirmation popup.");
            var closeBtn = Page.Locator("app-create-note .note-footer button.btn-submit");
            await closeBtn.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });
            await closeBtn.ClickAsync();
            _logger?.Info("Sold note success confirmation popup closed.");
        }

        /// <summary>
        /// Fills the Sold Note modal fields (shown after selecting the "Sold" action).
        /// Does not submit — call <see cref="ClickContinue"/> afterwards.
        /// </summary>
        public async Task FillSoldNoteAsync(SoldNoteFormData data)
        {
            _logger?.Info($"Filling Sold Note modal — PolicyNumber: {data.PolicyNumber}, Product: {data.Product}.");

            var quoteOwnerValue = data.IsQuoteOwner ? "true" : "false";
            await PageHelper.InteractWithElement(NoteFields.Fields[QuoteOwner][quoteOwnerValue]);

            if (!data.IsQuoteOwner && !string.IsNullOrWhiteSpace(data.AgentName))
                await PageHelper.InteractWithElement(NoteFields.Fields[SelectedAgentNote][data.AgentName]);

            await PageHelper.InteractWithElement(NoteFields.Fields[PolicyNumberNote][data.PolicyNumber]);
            await PageHelper.InteractWithElement(NoteFields.Fields[CallProductType][data.Product]);
            await PageHelper.InteractWithElement(NoteFields.Fields[EffectiveDateNote][data.EffectiveDate]);
            await PageHelper.InteractWithElement(NoteFields.Fields[ParentCompany][data.ParentCompany]);
            await PageHelper.InteractWithElement(NoteFields.Fields[PremiumNote][data.Premium]);

            _logger?.Info("Sold Note modal filled.");
        }

        /// <summary>
        /// End-to-end convenience method: selects the Sold action, fills all sold-note fields,
        /// submits the form, waits for the "Your note has been saved!" confirmation, and closes
        /// the confirmation panel — leaving the interview in its post-sale locked state.
        /// </summary>
        public async Task CreateSoldNoteAndConfirmAsync(SoldNoteFormData data)
        {
            _logger?.Info($"Creating sold note end-to-end — Carrier: {data.ParentCompany}, PolicyNumber: {data.PolicyNumber}.");
            await SelectActionAsync("Sold");
            await FillSoldNoteAsync(data);
            await ClickContinue();
            await WaitForSuccessConfirmationAsync();
            await CloseSuccessConfirmationAsync();
            _logger?.Info("Sold note created and confirmation closed.");
        }
    }
}



