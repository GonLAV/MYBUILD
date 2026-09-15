using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Base
{
    public abstract class ADBX_BasePopup : IPopup
    {
        protected readonly IPageHelper PageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;

        protected abstract string PopupIdentifier { get; }
        protected abstract string PopupName { get; }

        private const string PopupLocator = "div.modal-dialog div.modal-content";
        private const string LoaderLocator = ".loader-overlay";
        private const string AddButtonLocator = "//button[contains(text(), 'ADD') or contains(text(), 'Add')]";
        private const string CloseButtonLocator = "button.close";
        private const string UpdateButtonLocator = "//button[contains(text(), 'Update')]";
        private const string ConfirmButtonLocator = "//button[contains(text(), 'Confirm')]";
        private const string DeleteButtonLocator = "//app-button[contains(.,'Delete')]";
        private const string CreateButtonLocator = "//button[contains(text(), 'Create')]";  
        private const string SaveButtonLocator = "//button[contains(text(), 'Save')]";

        /// <summary>Budget for a best-effort click (closing, optional buttons) - failure is tolerated.</summary>
        private const int BestEffortClickTimeoutMs = 3000;

        /// <summary>Budget for a click that must land (see <see cref="ClickRequiredAsync"/>).</summary>
        private const int RequiredClickTimeoutMs = 10000;

        /// <summary>How long to let the loader overlay show up before deciding this action has none.</summary>
        private const int LoaderAppearTimeoutMs = 2000;

        /// <summary>How long a submitted popup action gets to be accepted and close the popup.</summary>
        private const int PopupCloseTimeoutMs = 15000;

        /// <summary>
        /// How long to wait for the loader overlay to clear. Explicit rather than defaulted, because
        /// the optional-arg default disagrees between IPageHelper (10s) and PageHelper (30s) and C#
        /// binds it from the call site's static type.
        /// </summary>
        private const int LoaderDisappearTimeoutMs = 10000;
        protected ADBX_BasePopup(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
        {
            BrowserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            PageHelper = pageHelper ?? throw new ArgumentNullException(nameof(pageHelper));
            ScopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger;

            if (validatePageReady)
            {
                try
                {
                    ValidatePageReady().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger?.LogException(ex, "Page validation failed during {0} initialization", PopupName);
                    throw;
                }
            }
        }

        public virtual async Task ClickContinue() => await Task.CompletedTask;

        public virtual async Task ClosePopup()
        {
            if (await TryClickCloseButton()) return;
        }

        public async Task ClickPopupAdd() => await ClickRequiredAsync(AddButtonLocator, "Add");

        /// <summary>
        /// Clicks the popup's Update button and waits for the save to be accepted.
        /// </summary>
        /// <remarks>
        /// Both halves matter. The click must land - silently skipping it left the caller asserting
        /// against the pre-edit state. And the popup must then CLOSE, which is the only reliable
        /// signal here that the save round-trip succeeded: this popup raises no
        /// <c>.loader-overlay</c>, so there is otherwise nothing at all between the click and the
        /// caller reading the summary page back.
        /// </remarks>
        public async Task ClickPopupUpdate()
        {
            await ClickRequiredAsync(UpdateButtonLocator, "Update");
            await WaitForPopupToClose();
        }

        /// <summary>
        /// Waits for the popup to close, which is what confirms a submitted action was accepted.
        /// </summary>
        /// <remarks>
        /// A REJECTED save (failed validation) leaves the popup open. The caller then reads the
        /// summary page sitting behind it, gets the pre-edit values, and blames the data for what was
        /// really a refused save. Waiting for the close turns that into an explicit failure.
        /// </remarks>
        protected async Task WaitForPopupToClose(int timeoutMs = PopupCloseTimeoutMs)
        {
            try
            {
                await Page.Locator(PopupLocator).WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = timeoutMs
                });
            }
            catch (TimeoutException ex)
            {
                throw new PopupTimeoutException(PopupName,
                    $"stayed open after the action was submitted, so it was not accepted (URL '{Page.Url}')",
                    timeoutMs, ex);
            }

            _logger?.Debug($"'{PopupName}' closed; the action was accepted.");
        }

        public async Task ClickPopupConfirm() => await ClickRequiredAsync(ConfirmButtonLocator, "Confirm");

        protected virtual async Task<bool> TryClickCloseButton()
        {
            var closeButtonLocator = Page.Locator(CloseButtonLocator);
            return await TryClickElement(closeButtonLocator);
        }

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper);
        }
        public virtual async Task ValidatePageReady() => await WaitForPopupToAppear();

        public virtual async Task WaitForPopupToAppear(int timeoutMs = 10000)
        {
            try
            {
                // Wait for popup to be visible
                var popupLocator = Page.Locator(PopupLocator);
                await popupLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });

                // Additional wait for popup content to load
                await Task.Delay(500);
            }
            catch (TimeoutException)
            {
                throw new PopupTimeoutException(PopupName, "appear", timeoutMs);
            }
        }

        /// <summary>
        /// Waits for the popup's loader overlay to appear and then go away.
        /// </summary>
        /// <remarks>
        /// The loader is rendered in RESPONSE to the click that just happened, so the old single
        /// `CountAsync() > 0` check could run before it existed and skip the wait entirely - letting
        /// a save that was still in flight be read back as the pre-edit state. Waiting for it to
        /// appear first closes that race. Mirrors <c>ADBX_BasePage.WaitForLoaderToDisappear</c>,
        /// which already had this shape.
        /// </remarks>
        protected virtual async Task WaitForLoaderToDisappear()
        {
            var loaderLocator = Page.Locator(LoaderLocator);

            try
            {
                await loaderLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = LoaderAppearTimeoutMs
                });
            }
            catch (TimeoutException)
            {
                // This action does not raise a loader - there is nothing to wait for.
                _logger?.Debug($"No loader appeared on '{PopupName}' within {LoaderAppearTimeoutMs}ms; not waiting.");
                return;
            }

            try
            {
                await PageHelper.WaitForElementToDisappearAsync(loaderLocator, LoaderDisappearTimeoutMs);
            }
            catch (PageElementException ex)
            {
                // Best-effort, for the same reason ADBX_BasePage.WaitForLoaderToDisappear is: the
                // overlay is only a spinner, and whatever the caller does next waits on the real
                // element and fails with a specific error if the page is genuinely not ready. On
                // some pages this overlay outlives the action by design, so hard-failing here turned
                // a working flow into a red test (BOLTAG_Create_Policy_From_Lead_Page and siblings).
                _logger?.Warning($"Loader on '{PopupName}' did not clear: {ex.Message}. Continuing.");
            }
        }

        protected virtual ILocator CreateLocator(string locatorValue)
        {
            return locatorValue.StartsWith("//")
                ? Page.Locator($"xpath={locatorValue}")
                : Page.Locator(locatorValue);
        }

        protected virtual async Task<bool> TryClickElement(ILocator locator, int timeoutMs = BestEffortClickTimeoutMs)
        {
            try
            {
                if (await locator.CountAsync() > 0 && await locator.IsVisibleAsync())
                {
                    await locator.ClickAsync(new LocatorClickOptions { Timeout = timeoutMs });
                    return true;
                }
            }
            catch
            {
                // Element not clickable or not found
            }
            return false;
        }

        /// <summary>
        /// Clicks a popup action button that MUST land, then waits for the resulting save to settle.
        /// </summary>
        /// <remarks>
        /// This deliberately does NOT wait for the popup to close - only <see cref="ClickPopupUpdate"/>
        /// does, because it is the only action proven to close its popup. <see cref="PopupLocator"/> is
        /// a generic modal selector, so a Confirm that dismisses a nested dialog and returns to its
        /// parent modal would still match it and a blanket close-wait would falsely time out.
        /// </remarks>
        /// <remarks>
        /// <see cref="TryClickElement"/> is right for best-effort actions such as closing, where a
        /// missing button is harmless. It is wrong for a save: returning false there made
        /// <c>ClickPopupUpdate</c> a silent no-op, so the caller read the summary back unchanged and
        /// blamed the DATA for what was really a click that never happened. Uses a longer budget than
        /// the best-effort path, because a must-land click should not fail over CI back-pressure.
        /// </remarks>
        protected async Task ClickRequiredAsync(string locatorValue, string action)
        {
            var locator = CreateLocator(locatorValue);

            if (!await TryClickElement(locator, RequiredClickTimeoutMs))
            {
                throw new PopupTimeoutException(PopupName,
                    $"could not click '{action}' (locator '{locatorValue}')", RequiredClickTimeoutMs);
            }

            _logger?.Debug($"Clicked '{action}' on '{PopupName}'; waiting for the action to settle.");
            await WaitForLoaderToDisappear();
        }
        public async Task ClickPopupDelete()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                DeleteButtonLocator,
                ElementAction.Click);

        }

        public async Task ClickPopupCreate() => await ClickRequiredAsync(CreateButtonLocator, "Create");
        public async Task ClickPopupSave() => await ClickRequiredAsync(SaveButtonLocator, "Save");
    }
}
