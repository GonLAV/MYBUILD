using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_StartPage : InterviewBase
    {
        private const string LobContainerLocator = "//*[contains(@class,'PolicyData.Lobs[]')]";
        private const string LobItemLocator = "//*[contains(@class,'PolicyData.Lobs[]')]//li";

        public Product_StartPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "_Start";
        protected override string PageName => "Start Page";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            if (formData == null)
            {
                await base.FillForm(null);
                return;
            }

            // Extract fields we handle specially on the consumer CL start page
            formData.TryGetValue(Lob, out var lobValue);
            formData.TryGetValue(EposNaicDescription, out var industryValue);

            // Fill industry FIRST - the ng-select typeahead needs focus to move away to commit the value
            if (!string.IsNullOrEmpty(industryValue))
            {
                await FillIndustryTypeahead(industryValue);
            }

            // Remove EposNaicDescription so base FillForm doesn't use fill() on the ng-select typeahead
            var adjustedFormData = new Dictionary<string, string>(formData);
            adjustedFormData.Remove(EposNaicDescription);

            // base.FillForm will click on other fields, committing the industry selection
            await base.FillForm(adjustedFormData);

            // Consumer CL flow: LOB selection is on the Start Page (not a separate LobsPage)
            if (!string.IsNullOrEmpty(lobValue) && await IsLobSelectionPresent())
            {
                await SelectLob(lobValue);
            }
        }

        private async Task<bool> IsLobSelectionPresent()
        {
            var lobContainer = Page.Locator(LobContainerLocator);
            return await lobContainer.CountAsync() > 0;
        }

        public async Task SelectLob(string lobName)
        {
            var lobItems = Page.Locator(LobItemLocator);
            var count = await lobItems.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var item = lobItems.Nth(i);
                var text = await item.TextContentAsync() ?? "";

                // Match by partial text (e.g. "WorkersCompensation" matches "Workers Compensation")
                var normalizedText = text.Replace(" ", "").Trim();
                var normalizedLob = lobName.Replace(" ", "").Trim();

                if (normalizedText.Contains(normalizedLob, StringComparison.OrdinalIgnoreCase))
                {
                    var checkbox = item.Locator("input[type='checkbox']");
                    if (!await checkbox.IsCheckedAsync())
                    {
                        await item.Locator("label").ClickAsync();
                        _logger?.Info($"Selected LOB: {text.Trim()}");
                    }
                    return;
                }
            }

            _logger?.Warning($"LOB '{lobName}' not found on Start Page");
        }

        public async Task FillIndustryTypeahead(string searchText)
        {
            var controlContainer = Page.Locator("//*[contains(@class,'PolicyData.EposNaicDescription')]");
            if (await controlContainer.CountAsync() == 0) return;

            // Check if already selected (placeholder not visible means value is set)
            var placeholder = controlContainer.Locator(".ng-placeholder");
            if (await placeholder.CountAsync() == 0 || !await placeholder.IsVisibleAsync())
            {
                _logger?.Debug("Industry already selected, skipping typeahead");
                return;
            }

            // Type into the industry search input
            var input = controlContainer.Locator("input[role='combobox']");
            await input.ClickAsync();
            await input.TypeAsync(searchText, new LocatorTypeOptions { Delay = 80 });

            // Wait for the first selectable option (excludes ng-select loading/placeholder items)
            var firstOption = controlContainer.Locator("div[role='option']:not(.ng-option-disabled)").First;
            await firstOption.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            // Click the first option
            await firstOption.ClickAsync();

            // Wait for loading to complete (the old solution called WaitforLoading after clicking)
            var loader = Page.Locator(Loader);
            await loader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30000 });

            _logger?.Info($"Selected industry via typeahead: {searchText}");

            await DismissAlertModalAsync();
        }

        public async Task<Product_ResultsPage> ClickSubmitForQuoteButton()
        {
            var submitButtonLocator = "//button[contains(@class, 'Submit button')]";

            _logger?.Info("Clicking Submit for Quote button");
            await PageHelper.InteractWithElement(LocatorType.XPath, submitButtonLocator, ElementAction.Click);

            // Wait for loader to disappear
            var loaderLocator = Page.Locator(Loader);
            await loaderLocator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60000
            });

            _logger?.Info("Navigating to Results Page");
            return new Product_ResultsPage(BrowserManager, PageHelper, ScopeContext, true, _logger);
        }
    }
}
