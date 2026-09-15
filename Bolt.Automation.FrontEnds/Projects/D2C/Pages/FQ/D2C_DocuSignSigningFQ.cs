using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    /// <summary>
    /// The DocuSign embedded signing ceremony reached after clicking "Open DocuSign to complete"
    /// on <see cref="D2C_BristolWestEsignFQ"/>. The redirect is same-tab and the ceremony renders
    /// in-page (no iframe), so all controls are reachable via <see cref="D2CBase.Page"/>.
    ///
    /// Selectors use DocuSign's stable <c>data-qa</c> hooks. The signature/initials field tabs carry
    /// per-session GUID suffixes, so they are matched by prefix. Verified live against the demo
    /// (apps-d.docusign.com/sign) — see the captured ceremony map.
    /// </summary>
    public class D2C_DocuSignSigningFQ(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        // Host-agnostic: matches apps-d.docusign.com/sign, apps.docusign.com/sign, etc.
        protected override string PageIdentifier => "docusign.com/sign";
        protected override string PageName => "DocuSign signing ceremony";

        // Electronic Record and Signature Disclosure (consent) gate.
        private const string ConsentCheckboxLabel = "[data-qa='ersd-agree-checkbox-label']";
        private const string ConsentContinueButton = "[data-qa='ersd-modal-agree']";

        // The clickable field is a <button class="tab-button" data-qa="signature-tab-required-<guid>"
        // (or "initials-tab-required-<guid>"). An UNSIGNED required field carries aria-invalid="true";
        // once signed, that flips to false. So aria-invalid is the reliable "still needs signing" gate
        // (the data-qa suffix does NOT change after signing). Clicking a *signature* button opens the
        // adopt dialog; once adopted, every Sign/Initial button places its mark directly on click.
        private const string UnsignedSignatureButton =
            "button.tab-button[data-qa^='signature-tab-required-'][aria-invalid='true']";
        private const string UnsignedInitialsButton =
            "button.tab-button[data-qa^='initials-tab-required-'][aria-invalid='true']";
        private const string AnyUnsignedFieldButton = UnsignedSignatureButton + ", " + UnsignedInitialsButton;

        // Adopt-your-signature dialog (Full Name / Initials are pre-filled & disabled).
        private const string AdoptSignatureModal = "[data-qa='adopt-signature-modal']";
        private const string AdoptSubmitButton = "[data-qa='adopt-submit']";

        // Completes the envelope.
        private const string FinishButton = "[data-qa='action-bar-btn-finish']";

        private const int ClickTimeoutMs = 10_000;
        private const int MaxFieldTabClicks = 30;

        /// <summary>
        /// Runs the full signing flow: accept consent, sign every required field, finish.
        /// Returns the <see cref="D2C_PaymentPageFQ"/> the host app redirects to after Finish.
        /// </summary>
        public async Task<D2C_PaymentPageFQ> CompleteSigningAsync()
        {
            return await _logger.ExecuteStepAsync("Complete DocuSign signing", async () =>
            {
                await AcceptConsentAsync();
                await SignAllRequiredFieldsAsync();
                return await FinishAsync();
            });
        }

        /// <summary>
        /// Accepts the Electronic Record and Signature Disclosure. The consent gate is rendered as
        /// a modal whose checkbox &lt;input&gt; is visually hidden, so the styled label is clicked.
        /// No-op if the consent gate is not present.
        /// </summary>
        public async Task AcceptConsentAsync()
        {
            await _logger.ExecuteStepAsync("Accept DocuSign electronic-records consent", async () =>
            {
                if (!await PageHelper.ElementExists(LocatorType.CSS, ConsentCheckboxLabel, timeout: 15000))
                {
                    _logger?.Info("DocuSign consent gate not present — skipping.");
                    return;
                }

                await PageHelper.InteractWithElement(LocatorType.CSS, ConsentCheckboxLabel, ElementAction.Click);
                await PageHelper.InteractWithElement(LocatorType.CSS, ConsentContinueButton, ElementAction.Click);

                // Wait for the consent modal to dismiss before interacting with the document.
                await PageHelper.WaitForElementToDisappearAsync(Page.Locator(ConsentContinueButton), timeout: 15000);
            });
        }

        /// <summary>
        /// Clicks each required signature/initials field. The first click opens the adopt dialog
        /// (name + initials are pre-filled; "Adopt and Sign" submitted once, which also signs that
        /// first field). Subsequent clicks place the adopted signature/initials directly. Each
        /// iteration is gated on the unsigned-field count (aria-invalid='true') decreasing, so the
        /// loop cannot spin on an already-signed field.
        /// </summary>
        public async Task SignAllRequiredFieldsAsync()
        {
            await _logger.ExecuteStepAsync("Sign all required DocuSign fields", async () =>
            {
                var unsignedSignature = Page.Locator(UnsignedSignatureButton);
                var unsignedAny = Page.Locator(AnyUnsignedFieldButton);

                // Wait for the document to render at least one unsigned signature field.
                await PageHelper.WaitForElementAsync(unsignedSignature.First, timeout: 30000, waitForVisibility: true);

                // Step 1: click a SIGNATURE button to open the adopt dialog (covers name + initials) and
                // adopt once. Clicking an initials button does not reliably trigger adoption.
                await unsignedSignature.First.ClickAsync(new() { Timeout = ClickTimeoutMs });
                var adoptSubmit = Page.Locator(AdoptSubmitButton);
                if (await PageHelper.WaitForElementAsync(adoptSubmit, timeout: 15000, waitForVisibility: true) != null)
                {
                    _logger?.LogUiAction("Click", "AdoptAndSign", "Adopting signature in DocuSign dialog");
                    await adoptSubmit.ClickAsync(new() { Timeout = ClickTimeoutMs });
                    await PageHelper.WaitForElementToDisappearAsync(Page.Locator(AdoptSignatureModal), timeout: 15000);
                }
                else
                {
                    _logger?.Warning("Adopt dialog did not appear after clicking the first signature button.");
                }

                // Step 2: click every remaining unsigned Sign/Initial button. Each places the adopted
                // mark directly. Progress is gated on the unsigned count (aria-invalid='true') dropping,
                // so the loop advances field-by-field and cannot spin on an already-signed button.
                for (int i = 0; i < MaxFieldTabClicks; i++)
                {
                    int before = await unsignedAny.CountAsync();
                    if (before == 0)
                    {
                        _logger?.Info($"All required fields signed after {i} additional click(s).");
                        return;
                    }

                    await unsignedAny.First.ClickAsync(new() { Timeout = ClickTimeoutMs });

                    try
                    {
                        await Page.WaitForFunctionAsync(
                            "args => document.querySelectorAll(args.sel).length < args.n",
                            new { sel = AnyUnsignedFieldButton, n = before },
                            new() { Timeout = 8000 });
                    }
                    catch (TimeoutException)
                    {
                        _logger?.Debug($"Unsigned-field count did not drop after click {i + 1} (was {before}); retrying.");
                    }
                }

                _logger?.Warning($"Stopped signing after {MaxFieldTabClicks} clicks; required fields may remain.");
            });
        }

        /// <summary>
        /// Clicks Finish to complete the envelope and waits for the redirect back out of DocuSign,
        /// then returns the host app's <see cref="D2C_PaymentPageFQ"/> (which validates its own
        /// readiness on construction).
        /// </summary>
        public async Task<D2C_PaymentPageFQ> FinishAsync()
        {
            return await _logger.ExecuteStepAsync("Finish DocuSign envelope", async () =>
            {
                await PageHelper.InteractWithElement(LocatorType.CSS, FinishButton, ElementAction.Click);

                try
                {
                    await Page.WaitForURLAsync(
                        url => !url.Contains("docusign", StringComparison.OrdinalIgnoreCase),
                        new() { Timeout = 30000 });
                    _logger?.Info($"Returned from DocuSign to: {Page.Url}");
                }
                catch (TimeoutException)
                {
                    _logger?.Warning($"Did not navigate away from DocuSign after Finish; current URL: {Page.Url}");
                }

                return new D2C_PaymentPageFQ(BrowserManager, PageHelper, ScopeContext, validatePageReady: true, _logger);
            });
        }
    }
}
