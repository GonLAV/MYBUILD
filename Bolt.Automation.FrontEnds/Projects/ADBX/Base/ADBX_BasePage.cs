using System.Reflection;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Microsoft.Playwright;
using PlaywrightValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Base
{
    public abstract class ADBX_BasePage : IDashboard
    {
        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;
        protected const int DefaultTimeout = 20000;
        protected abstract string PageIdentifier { get; }
        protected abstract string PageName { get; }

        /// <summary>
        /// Budget for the grid loader (<see cref="CommonLocators.Loader"/>) to disappear during page
        /// readiness. A little larger than <see cref="DefaultTimeout"/> so a genuinely slow-but-real
        /// grid under QA back-pressure gets room to settle. Override per page if a grid is known to
        /// take longer. The wait is best-effort — exceeding this budget does NOT fail page construction
        /// (see <see cref="WaitForLoaderToDisappear"/>).
        /// </summary>
        protected virtual int GridLoaderTimeoutMs => 30000;

        protected static class CommonLocators
        {
            public const string Loader = "div.table-wrapper object.loader";
            public const string SearchResultsOptions = "//app-autocomplete/ng-select//div[@role='option']";
            public const string SearchButton = "button.search-button";
            public const string DataTable = "ngx-datatable";
            public const string SearchInput = "div.search-container input";
            public const string AccountPopup = "//h5[contains(text(),'Update Account Information')]";
            public const string AccountMatchPopup = "//h5[contains(text(),'An account already exists with the same information')]";
            public const string NewQuote = "//app-button[@type = 'hidden-cta']//span[text() = 'NEW QUOTE']";
            public const string RecentSearch = "//input[@type='search']";
            public const string SummaryDetailTitle = "div.detail-item div.detail-title";
            public const string SummaryDetailValue = "div.detail-item div.detail-value";
            public const string NotificationsCountIcon = "span.notifications-count-badge";
            public const string SearchResultOptions = "//app-autocomplete/ng-select//div[@role='option']";
            public const string NavigateInnerTab = "//li/a[contains(text(),'{0}')]";
        }

        protected ADBX_BasePage(
          IBrowserManager browserManager,
          IPageHelper pageHelper,
          IScopeContext scopeContext,
          bool validatePageReady = true,
          IAutomationLogger? logger = null)
        {
            BrowserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            _pageHelper = pageHelper ?? throw new ArgumentNullException(nameof(pageHelper));
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
                    _logger?.Error($"Page validation failed during {PageName} initialization: {ex.Message}");
                    throw;
                }
            }
        }

        public virtual async Task ValidatePageReady()
        {
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName);
        }

        public Task WaitForGridLoadAsync() => WaitForLoaderToDisappear();

        /// <summary>
        /// Best-effort wait for the grid loader spinner to disappear. If the loader is never seen it
        /// returns immediately; if it lingers past <see cref="GridLoaderTimeoutMs"/> the timeout is
        /// swallowed (logged, not thrown) — the same contract as the generic
        /// <c>PageValidationHelper.WaitForLoadersToDisappearAsync</c>. This deliberately does NOT fail
        /// page construction: a slow grid backend under QA back-pressure would otherwise surface as an
        /// opaque <c>PageCreationException</c> at readiness time, even though every downstream grid
        /// consumer (SelectTableRowAsync / GetRowCount / GetRowData / IsTableDisplayed) waits on the
        /// <c>ngx-datatable</c> itself and fails with a specific, actionable error if the grid is truly
        /// not ready. Letting the real grid interaction own that wait yields a clearer failure and
        /// removes the false-negative when the loader is merely slow.
        /// </summary>
        protected virtual async Task WaitForLoaderToDisappear()
        {
            try
            {
                var loaderLocator = Page.Locator(CommonLocators.Loader);

                if (!await TryWaitForLoaderVisible(loaderLocator, timeoutMs: 2000))
                {
                    _logger?.Debug("No loader found on page");
                    return;
                }

                _logger?.Debug("Loader found, waiting for it to disappear");
                await PageHelper.WaitForElementToDisappearAsync(loaderLocator, GridLoaderTimeoutMs, initialRetries: 3, retryDelay: 500);
                _logger?.Debug("Loader disappeared successfully");
            }
            catch (Exception ex)
            {
                // Best-effort: a lingering/slow grid loader must not fail page construction. Log and
                // continue — the subsequent grid interaction waits on its own target element.
                _logger?.Debug($"Grid loader did not disappear within {GridLoaderTimeoutMs}ms on {PageName} - continuing anyway: {ex.Message}");
            }
        }

        private async Task<bool> TryWaitForLoaderVisible(ILocator loaderLocator, int timeoutMs)
        {
            try
            {
                await PageHelper.WaitForElementAsync(loaderLocator, timeoutMs, waitForVisibility: true, retries: 1);
                return await loaderLocator.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Searches the ADBX header search box. The search resolves one of two ways: the autocomplete
        /// dropdown offers a match and it is clicked, or no suggestion appears within the wait and the
        /// Search button is clicked instead. Both routes land on /search and render the results grid,
        /// so the route does not change what the caller must do next — it is returned and logged only
        /// because it changes the timing of when the grid appears, which has caused grid-wait
        /// timeouts that were hard to read.
        /// Returns <c>true</c> when the autocomplete route was taken.
        /// To open the first match, prefer <see cref="SearchAndOpenFirstResultAsync"/>.
        /// </summary>
        public async Task<bool> SearchFor(string input)
        {
            await PageHelper.WaitForElementAsync(
               Page.Locator(CommonLocators.SearchInput),
               waitForVisibility: true
            );

            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                CommonLocators.SearchInput,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = input, PressTab = false }
            );

            var isSearchResultsDisplayed = await PageHelper.ElementExists(LocatorType.XPath, CommonLocators.SearchResultOptions, 5000);

            if (isSearchResultsDisplayed)
            {
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    CommonLocators.SearchResultOptions,
                    ElementAction.Click
                );

                _logger?.Info($"Search for '{input}' resolved via the autocomplete suggestion.");
                return true;
            }

            await ClickOnSearchButton();
            _logger?.Info($"Search for '{input}' resolved via the Search button.");
            return false;
        }

        /// <summary>
        /// Searches for <paramref name="input"/> and opens the first match from the results grid.
        /// Both <see cref="SearchFor"/> routes land on /search and render the grid, so the row
        /// selection is always required; the route only differs in how the page was reached, which
        /// <see cref="SearchFor"/> logs for diagnostics.
        /// </summary>
        public async Task SearchAndOpenFirstResultAsync(string input)
        {
            await SearchFor(input);
            await PageHelper.SelectTableRowAsync(1);
        }

        public async Task ClickOnSearchButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                CommonLocators.SearchButton,
                ElementAction.Click
            );
            await WaitForLoaderToDisappear();
        }

        public async Task ClickOnSpecificLink(NavigationType type)
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                $"//div[contains(@class,'detail-title') and contains(text(),'{type}')]/following-sibling::div[contains(@class,'detail-value')]/span[contains(@class,'link')]",
                ElementAction.Click
            );
        }

        public Task ClickContinue()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsUpdateAccountInformationPopUpExists()
        {
            return await PageHelper.ElementExists(LocatorType.XPath, CommonLocators.AccountPopup);
        }

        /// <summary>
        /// Checks whether the "An account already exists with the same information"
        /// popup (account match — hard or soft) is currently displayed.
        /// </summary>
        public async Task<bool> IsAccountMatchPopUpExists()
        {
            return await PageHelper.ElementExists(LocatorType.XPath, CommonLocators.AccountMatchPopup);
        }

        public async Task<ADBX_EnterUpdateAccountInformationPopup> ClickOnNewQuote()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                CommonLocators.NewQuote,
                ElementAction.Click
            );
            await WaitForLoaderToDisappear();

            return new ADBX_EnterUpdateAccountInformationPopup(BrowserManager, PageHelper, ScopeContext);
        }

        public async Task SearchRecentRecords(string text)
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                CommonLocators.RecentSearch,
                ElementAction.Fill,
                new ElementInteractionOptions { Value = text }
            );
            await WaitForLoaderToDisappear();
        }

        public async Task<Dictionary<string, string>> GetSummaryData()
        {
            var titleLocator = Page.Locator(CommonLocators.SummaryDetailTitle);
            var valueLocator = Page.Locator(CommonLocators.SummaryDetailValue);

            // Wait for the first elements to be visible before getting all elements
            await PageHelper.WaitForElementAsync(titleLocator.Last, DefaultTimeout, waitForVisibility: true);
            await PageHelper.WaitForElementAsync(valueLocator.Last, DefaultTimeout, waitForVisibility: true);

            var titleElements = await titleLocator.AllAsync();
            var valueElements = await valueLocator.AllAsync();

            if (titleElements.Count == 0 || valueElements.Count == 0)
                throw new PageElementException("Dashboard summary", "has no titles or values");

            var actDic = new Dictionary<string, string>();

            for (int i = 0; i < titleElements.Count; i++)
            {
                var key = (await titleElements[i].TextContentAsync())?.Trim()
                    .Replace(":", "")
                    .Replace(" ", "")
                    ?? string.Empty;
                var keyVal = (await valueElements[i].TextContentAsync())?.Trim()
                    .Replace("\n      ", " ")
                    .Replace("  ", "")
                    .Replace("-", "")
                    ?? string.Empty;

                actDic[key] = keyVal;
            }

            return actDic;
        }

        public virtual async Task WaitForNotificationsCountIconToAppear()
        {

            var loaderLocator = Page.Locator(CommonLocators.NotificationsCountIcon);
            await PageHelper.WaitForElementAsync(loaderLocator, 100000, waitForVisibility: true);
        }

        public async Task<int> GetSmsNotificationsCountAsync()
        {
            var badgeExists = await PageHelper.ElementExists(ADBX_FieldNames.SmsNotificationsCountBadge, 5000);
            if (!badgeExists)
                return 0;

            var badgeText = (await PageHelper.GetFieldValue(ADBX_FieldNames.SmsNotificationsCountBadge)).Trim();
            return int.TryParse(badgeText, out var count) ? count : 0;
        }

        public async Task OpenSmsNotificationsDropdownAsync()
        {
            await PageHelper.InteractWithField(ADBX_FieldNames.SmsNotificationsButton);
            await PageHelper.GetFieldValue(ADBX_FieldNames.SmsNotificationsDropdown, PlaywrightValueType.IsVisible, DefaultTimeout, waitForVisibility: true);
        }

        public async Task<string> GetTopSmsNotificationTimestampAsync()
        {
            return (await PageHelper.GetFieldValue(ADBX_FieldNames.SmsNotificationsFirstItemTimestamp, PlaywrightValueType.Text, DefaultTimeout, waitForVisibility: true)).Trim();
        }

        public async Task<string> GetTopSmsNotificationSubtitleAsync()
        {
            return (await PageHelper.GetFieldValue(ADBX_FieldNames.SmsNotificationsFirstItemSubtitle, PlaywrightValueType.Text, DefaultTimeout, waitForVisibility: true)).Trim();
        }

        public async Task<T> ClickSeeAllSmsNotificationsAsync<T>(IPageFactory pageFactory)
            where T : ADBX_BasePage
        {
            await PageHelper.InteractWithField(ADBX_FieldNames.SeeAllSmsNotificationsLink);
            await WaitForLoaderToDisappear();
            return pageFactory.CreatePage<T>();
        }

        public async Task ClickOnMenuTab(NavigationType type)
        {
            var uiText = NavigationTypeMapper.ToUiText(type);
            _logger?.Info($"Navigate to {uiText} page.");
            var locator = $"//app-nav-item//span[normalize-space()='{uiText}']/ancestor::app-nav-item[1]";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click,
                new ElementInteractionOptions { Timeout = 4000 }
            );
        }

        public async Task ClickOnAdminMenuTab(string tabName, string? subtabName = null)
        {
            var locator = $"app-nav-item a:has-text('{tabName}')";

            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                locator,
                ElementAction.Hover,
                new ElementInteractionOptions { Timeout = 5000 }
            );

            if (string.IsNullOrWhiteSpace(subtabName))
                return;

            var subtabLocator = $"ul.dropdown app-nav-item a:has-text('{subtabName}')";
            await PageHelper.WaitForElementAsync(
                Page.Locator(subtabLocator),
                4000,
                waitForVisibility: true
            );

            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                subtabLocator,
                ElementAction.Click,
                new ElementInteractionOptions { Timeout = 4000 }
            );
        }

        public async Task AddFileAsync()
        {
            // Get the base directory of the executing assembly
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            // Build the full path to the attachment
            string attachmentPath = Path.Combine(baseDir, "Models", "TestData", "ExternalDataFile", "Attachment.pdf");

            // Ensure the file exists
            if (!File.Exists(attachmentPath))
                throw new FileNotFoundException($"Attachment file not found: {attachmentPath}");
            // Find the file input and set the file
            var fileInput = Page.Locator(FieldRegistryADBX.Fields[FieldNames.AddFile].Locators);
            await fileInput.SetInputFilesAsync(attachmentPath);
        }

        public async Task ClearEmailRecipientsAsync()
        {
            var input = Page.Locator("app-emails-input input#innerInput");
            await PageHelper.WaitForElementAsync(input, DefaultTimeout, waitForVisibility: true);

            await input.ClickAsync();

            var chips = Page.Locator("app-emails-input span.item");
            while (await chips.CountAsync() > 0)
            {
                await Page.Keyboard.PressAsync("Backspace");
            }
        }

        public async Task SendEmailWithTemplate(string text, string templateName, bool attachmentAdd)
        {
            await ClearEmailRecipientsAsync();
            await PageHelper.InteractWithField(FieldNames.EmailRecipient);
            await PageHelper.InteractWithField(FieldNames.EmailTemplateList, templateName);
            await AddCombinedFreeText(text);
            if (attachmentAdd)
            {
                await AddFileAsync();
            }
            var sendButtonSelector = (string)FieldRegistryADBX.Fields[FieldNames.SendEmailButton].Locators;
            await Page.WaitForSelectorAsync($"{sendButtonSelector}:not([disabled])", new() { Timeout = DefaultTimeout });

            await PageHelper.InteractWithField(FieldNames.SendEmailButton);
        }

        public async Task SendSms(string content)
        {
            await PageHelper.InteractWithField(ADBX_FieldNames.SmsContent, content);
            var sendButtonSelector = (string)FieldRegistryADBX.Fields[ADBX_FieldNames.SmsSendButton].Locators;
            await Page.WaitForSelectorAsync($"{sendButtonSelector}:not([disabled])", new() { Timeout = DefaultTimeout });
            await PageHelper.InteractWithField(ADBX_FieldNames.SmsSendButton);
        }

        //add extra text to current text without overriding it.
        public async Task AddCombinedFreeText(string text)
        {
            var current = await PageHelper.GetFieldValue(FieldNames.FreeText);
            var combined = string.IsNullOrWhiteSpace(current)
                ? text
                : $"{text}{Environment.NewLine}{current}";

            await PageHelper.InteractWithField(FieldNames.FreeText, combined);
        }

        public async Task<bool> HasSmsDeliveredStatusAsync(string content, IPollyRetryService pollyRetryService, int retryTimeoutSeconds = 30)
        {
            _logger?.Info($"Checking SMS delivered status for note with content '{content}'.");

            var note = Page.Locator("div.note.ng-star-inserted")
                .Filter(new LocatorFilterOptions
                {
                    Has = Page.Locator("div.description", new PageLocatorOptions { HasText = content })
                });

            var deliveredIcon = note.Locator("img[src*='status-delivered.svg']");
            try
            {
                return await pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    await PageHelper.RefreshPageAsync();
                    try
                    {
                        await PageHelper.WaitForElementAsync(deliveredIcon.First, 3000, waitForVisibility: true);
                        return await deliveredIcon.CountAsync() > 0;
                    }
                    catch (PageElementException)
                    {
                        return false;
                    }
                }, retryTimeoutSeconds);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> HasTimelineNoteAsync(string noteTitle, string? uniqueIdentifier = null, int timeoutMs = 10000)
        {
            _logger?.Info($"Checking if note with title '{noteTitle}' exists in the timeline.");

            var notes = Page.Locator("div.note.ng-star-inserted")
                .Filter(new LocatorFilterOptions
                {
                    Has = Page.Locator("div.top span.date", new PageLocatorOptions { HasText = noteTitle })
                });

            if (!string.IsNullOrWhiteSpace(uniqueIdentifier))
            {
                notes = notes.Filter(new LocatorFilterOptions
                {
                    Has = Page.Locator("div.description", new PageLocatorOptions { HasText = uniqueIdentifier })
                });
            }

            await PageHelper.WaitForElementAsync(notes.First, timeoutMs, waitForVisibility: true);
            return await notes.CountAsync() > 0;
        }
        public async Task<T> NavigateToMenuAsync<T>(NavigationType navigationType, IPageFactory pageFactory)
           where T : ADBX_BasePage
        {
            await ClickOnMenuTab(navigationType);
            return pageFactory.CreatePage<T>();
        }

        public async Task<T> NavigateInnerTabAsync<T>(NavigationType navigationType, IPageFactory pageFactory)
         where T : ADBX_BasePage
        {
            var uiText = NavigationTypeMapper.ToUiText(navigationType);
            _logger.Info($"Navigate to {uiText} page.");
            await PageHelper.InteractWithElement(
                           LocatorType.XPath,
                           $"//li/a[contains(text(),'{uiText}')]",
                           ElementAction.Click
                       );

            return pageFactory.CreatePage<T>();
        }
    }
}
