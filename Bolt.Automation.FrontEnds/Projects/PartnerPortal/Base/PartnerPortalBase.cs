using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.PartnerPortal.Base
{
    public abstract class PartnerPortalBase : IDashboard
    {
        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        protected const int DefaultTimeout = 20000;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;

        protected abstract string PageIdentifier { get; }
        protected abstract string PageName { get; }
        protected virtual Dictionary<string, UIElement> FieldRegistry => FieldRegistryPartnerPortal.Fields;
        protected static class CommonLocators
        {
            public const string Loader = "//object[contains(@class,'loader')]";
        }

        private const string TableLocator = "//table[contains(@class,'mat-mdc-table')]";
        private const string TableHeaderCellLocator = "thead th.mat-mdc-header-cell";
        private const string TableRowLocator = "tbody tr.mat-mdc-row";
        private const string TableCellLocator = "td.mat-mdc-cell";


        protected PartnerPortalBase(IBrowserManager browserManager,
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

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
          await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
        }

        public virtual async Task ValidatePageReady()
        {
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName);
        }

        protected virtual async Task WaitForLoaderToDisappear()
        {
            try
            {
                var loaderLocator = Page.Locator(CommonLocators.Loader);
                if (await loaderLocator.CountAsync() > 0)
                {
                    _logger?.Debug("Loader found, waiting for it to disappear");
                    await PageHelper.WaitForElementToDisappearAsync(loaderLocator);
                    _logger?.Debug("Loader disappeared successfully");
                }
                else
                {
                    _logger?.Debug("No loader found on page");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to wait for loader to disappear on {0}", PageName);
                // Don't throw - this is not critical
            }
        }

        public virtual async Task ClickContinue()
        {
            if (FieldRegistry.ContainsKey(FrontEnds.FormData.Common.FieldNames.NextButton))
            {
                await PageHelper.InteractWithField(FrontEnds.FormData.Common.FieldNames.NextButton);
            }
        }

        public async Task SearchRecentRecords(string text)
        {
            await PageHelper.WaitForApiResponseAsync(
                () => PageHelper.InteractWithField(
                    PartnerPortal_FieldNames.RecentSearch,
                    new ElementInteractionOptions { Value = text, UseSequentialTyping = true, PressTab = false }
                ),
                "progress",
                200,
                30000);
        }

        public async Task<bool> IsSpecificColumnHaveDataInTableAsync(string expectedData, string column, IPollyRetryService pollyRetryService, int retryTimeoutSeconds = 20)
        {
            if (string.IsNullOrWhiteSpace(expectedData))
                throw new ArgumentException("Expected data cannot be null or empty.", nameof(expectedData));
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(column));

            return await pollyRetryService.ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await SnapshotCheckColumnAsync(expectedData, column);
                }
                catch (Exception)
                {
                    // DOM may be mid-update; treat as not-found so Polly retries
                    return false;
                }
            }, retryTimeoutSeconds);
        }

        private async Task<bool> SnapshotCheckColumnAsync(string expectedData, string column)
        {
            var table = Page.Locator(TableLocator);
            if (await table.CountAsync() == 0)
                return false;

            var headers = table.Locator(TableHeaderCellLocator);
            var headerCount = await headers.CountAsync();

            int? targetColumnIndex = null;
            for (int i = 0; i < headerCount; i++)
            {
                var header = headers.Nth(i);
                if (!await header.IsVisibleAsync())
                    continue;

                var headerLabel = header.Locator(".header-label");
                var headerText = await headerLabel.CountAsync() > 0
                    ? await headerLabel.InnerTextAsync()
                    : await header.InnerTextAsync();

                if (string.Equals(NormalizeHeaderText(headerText), column.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    targetColumnIndex = i;
                    break;
                }
            }

            if (targetColumnIndex == null)
                return false;

            var rows = table.Locator(TableRowLocator);
            var rowCount = await rows.CountAsync();
            if (rowCount == 0)
                return false;

            for (var i = 0; i < rowCount; i++)
            {
                var cell = rows.Nth(i).Locator(TableCellLocator).Nth(targetColumnIndex.Value);
                if (await cell.CountAsync() == 0)
                    continue;

                var cellText = NormalizeHeaderText(await cell.InnerTextAsync());
                if (cellText.Contains(expectedData, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public async Task<bool> IsResendInviteVisibleInTableAsync(IPollyRetryService pollyRetryService, int retryTimeoutSeconds = 20)
        {
            var table = Page.Locator(TableLocator);
            await table.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await WaitForMatTableContentAsync(table);

            return await pollyRetryService.ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var resendButton = table.Locator(".Resend-Invite");
                    return await resendButton.CountAsync() > 0 && await resendButton.First.IsVisibleAsync();
                }
                catch (Exception)
                {
                    return false;
                }
            }, retryTimeoutSeconds);
        }

        private async Task WaitForMatTableContentAsync(ILocator table, int timeoutMs = 30000)
        {
            var rows = table.Locator(TableRowLocator);
            var rowsTask = rows.First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });

            await rowsTask;

            if (!await rows.First.IsVisibleAsync())
                throw new TimeoutException("Timed out waiting for table rows to render.");
        }

        private static string NormalizeHeaderText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Replace('\u00A0', ' ');
            return string.Join(" ", normalized.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

    }
}
