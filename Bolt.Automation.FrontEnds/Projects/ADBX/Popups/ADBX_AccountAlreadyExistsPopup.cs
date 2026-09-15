using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Popups
{
    /// <summary>
    /// "An account already exists with the same information" popup that appears
    /// after clicking ADD on the Enter Account Information popup when the form
    /// data matches an existing customer record.
    ///
    /// Two variants:
    ///  - <see cref="AccountMatchType.HardMatch"/> — "A new account cannot be created";
    ///    user MUST pick an existing account.
    ///  - <see cref="AccountMatchType.SoftMatch"/> — "To proceed, select one of the
    ///    options below"; user can either create a new account or choose an existing one.
    /// </summary>
    public class ADBX_AccountAlreadyExistsPopup(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : ADBX_BasePopup(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "adbx";

        protected override string PopupName => "Account Already Exists";

        public const string TitleLocator =
            "//h5[contains(text(),'An account already exists with the same information')]";

        private const string SoftMatchSubtitleLocator =
            "//*[self::h2 or self::label][contains(text(),'To proceed, select one of the options below')]";

        private const string HardMatchSubtitleLocator =
            "//*[self::h2 or self::label][contains(text(),'A new account cannot be created')]";

        private const string CreateNewAccountRadio =
            "//label[contains(@for,'newAccountValue')] | //input[@id='new']/parent::label | //label[normalize-space(.)='Create New Account']";

        private const string ChooseExistingAccountRadio =
            "//label[contains(@for,'existingAccountValue')] | //input[@id='existing']/parent::label | //label[normalize-space(.)='Choose an existing account']";

        private const string ExistingAccountOptions =
            "//input[@name='account'] | //label[contains(@for,'accountresult')]";

        private const string ContinueButton =
            "//button[normalize-space(.)='Continue' or normalize-space(.)='CONTINUE']";

        /// <summary>
        /// Determines which variant of the popup is shown. Returns
        /// <see cref="AccountMatchType.None"/> when the popup is not present.
        /// </summary>
        public async Task<AccountMatchType> GetMatchTypeAsync()
        {
            if (!await IsPopupVisibleAsync()) return AccountMatchType.None;

            if (await IsLocatorVisibleAsync(HardMatchSubtitleLocator)) return AccountMatchType.HardMatch;
            if (await IsLocatorVisibleAsync(SoftMatchSubtitleLocator)) return AccountMatchType.SoftMatch;

            // Title visible but neither known subtitle — caller should treat as unknown.
            return AccountMatchType.Unknown;
        }

        /// <summary>
        /// Checks whether the popup title is currently visible.
        /// Useful as a defensive guard right after clicking ADD.
        /// </summary>
        public async Task<bool> IsPopupVisibleAsync()
            => await IsLocatorVisibleAsync(TitleLocator);

        /// <summary>
        /// Soft-match action: pick "Create new account".
        /// </summary>
        public async Task SelectCreateNewAccountAsync()
        {
            var locator = CreateLocator(CreateNewAccountRadio);
            if (!await TryClickElement(locator))
            {
                _logger?.Warning($"{PopupName}: 'Create new account' radio not clickable.");
            }
        }

        /// <summary>
        /// Both variants: pick an existing customer.
        /// For soft match this also clicks the parent "Choose an existing account" radio first.
        /// <paramref name="optionIndex"/> is zero-based and applies to the inner customer list.
        /// </summary>
        public async Task SelectExistingAccountAsync(int optionIndex = 0)
        {
            // Soft match: parent radio is required to reveal the inner customer list.
            // Hard match: this radio is absent — TryClickElement is a no-op in that case.
            var parent = CreateLocator(ChooseExistingAccountRadio);
            await TryClickElement(parent);

            var options = CreateLocator(ExistingAccountOptions);
            var count = await options.CountAsync();
            if (count == 0)
            {
                _logger?.Warning($"{PopupName}: no existing-account radio options found.");
                return;
            }

            if (optionIndex < 0 || optionIndex >= count)
            {
                _logger?.Warning($"{PopupName}: optionIndex {optionIndex} out of range (count={count}); falling back to 0.");
                optionIndex = 0;
            }

            await options.Nth(optionIndex).ClickAsync(new LocatorClickOptions { Timeout = 3000 });
        }

        /// <summary>
        /// Clicks the popup's Continue button. The base popup class already exposes
        /// ClickPopupConfirm(); this is a sibling for the "Continue" wording used here.
        /// </summary>
        public async Task ClickContinueAsync()
        {
            var locator = CreateLocator(ContinueButton);
            if (await TryClickElement(locator))
            {
                await WaitForLoaderToDisappear();
            }
            else
            {
                _logger?.Warning($"{PopupName}: Continue button not clickable.");
            }
        }

        /// <summary>
        /// One-shot helper that detects the variant and resolves it sensibly:
        ///   - Hard match → select first existing account, click Continue
        ///   - Soft match → caller-provided <paramref name="softMatchAction"/> drives the
        ///     decision (default: select existing); then click Continue
        ///   - None / unknown → no-op
        /// </summary>
        public async Task<AccountMatchType> ResolveAsync(
            AccountMatchSoftAction softMatchAction = AccountMatchSoftAction.SelectExisting,
            int existingOptionIndex = 0)
        {
            var type = await GetMatchTypeAsync();

            switch (type)
            {
                case AccountMatchType.HardMatch:
                    await SelectExistingAccountAsync(existingOptionIndex);
                    await ClickContinueAsync();
                    break;
                case AccountMatchType.SoftMatch:
                    if (softMatchAction == AccountMatchSoftAction.CreateNew)
                        await SelectCreateNewAccountAsync();
                    else
                        await SelectExistingAccountAsync(existingOptionIndex);
                    await ClickContinueAsync();
                    break;
                case AccountMatchType.None:
                case AccountMatchType.Unknown:
                default:
                    break;
            }

            return type;
        }

        public override async Task ValidatePageReady()
        {
            // Override the base WaitForPopupToAppear flow — the title h5 is the
            // most reliable signal for THIS popup specifically (not the generic
            // modal-dialog which the Enter Account Information popup also uses).
            try
            {
                await Page.Locator($"xpath={TitleLocator}").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000
                });
            }
            catch (TimeoutException)
            {
                // Caller is expected to have used IsPopupVisibleAsync first; if validation
                // is still requested, surface the timeout the same way base class would.
                throw;
            }
        }

        private async Task<bool> IsLocatorVisibleAsync(string xpath)
        {
            var locator = CreateLocator(xpath);
            if (await locator.CountAsync() == 0) return false;
            try
            {
                return await locator.First.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Variant of the account-match popup that is currently displayed.</summary>
    public enum AccountMatchType
    {
        /// <summary>Popup is not on screen.</summary>
        None,
        /// <summary>"To proceed, select one of the options below" — user can create new or pick existing.</summary>
        SoftMatch,
        /// <summary>"A new account cannot be created" — user must pick an existing customer.</summary>
        HardMatch,
        /// <summary>Title is shown but neither known subtitle is — caller should investigate.</summary>
        Unknown
    }

    /// <summary>Soft-match resolution preference for <see cref="ADBX_AccountAlreadyExistsPopup.ResolveAsync"/>.</summary>
    public enum AccountMatchSoftAction
    {
        SelectExisting,
        CreateNew
    }
}
