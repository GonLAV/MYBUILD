using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;
using Microsoft.Playwright;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_RatesPage : D2CBase
    {
        #region Locators
        private const string GettingTheBestQuoteLoaderLocator = "//app-loader[@class = 'ng-star-inserted']";
        private const string BundleResultsLocator = "span:has-text('Bundle & Save')";
        private const string BuySeparatelyResultsLocator = "//span[contains(text(),'Buy separately')]";
        private const string RatedCallToPurchaseCarriersBundleLocator = "//span[contains(text(), 'Bundle & Save')]/ancestor::mat-tab-group//ul[@class='coverages']/li/app-coverage/div";
        private const string BuyNowButtonLocator = "//span[contains(text(), 'Buy now')]/ancestor::div[@class = 'coverage-wrapper']";
        private const string CallAgentButtonLocator = "//button[contains(@aria-label, 'callAgentButton')]/ancestor::div[@class = 'coverage-wrapper']";
        private const string PolicyCoverageNameLocator = "//div[contains(@aria-label , 'Coverage')]//div[@class = 'name']";
        private const string LobCoverageNameLocator = "//div[@class = 'coverage-row-item']//h3[@class='coverage-section-title'] | //td[contains(@aria-label, 'Line')]/following-sibling::td";
        private const string EntityNameLocator = "div.entity-name";

        private const string CompareCheckboxLocatorFormat = ".coverage-wrapper[carrier-name-automation='{0}'] input.compare-checkbox:visible";
        private const string ComparisonFooterLocator = ".compare-popup";
        private const string ComparisonFooterItemLocator = ".compare-popup .popup-content .compare-item:not(.placeholder)";
        private const string ComparisonFooterCarrierLogoLocator = "img.carrier-logo";
        private const string CompareButtonLocator = ".compare-popup .popup-actions button.app-button";
        private const string FloodCoverageCardLocator = "app-toggled-coverage";
        private const string FloodToggleLocator = "app-toggled-coverage label.switch >> nth=0";
        #endregion

        protected override string PageIdentifier => "rates";
        protected override string PageName => "Rates Page";

        public D2C_RatesPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) 
        {
            var loaderLocator = Page.Locator(GettingTheBestQuoteLoaderLocator);
            PageHelper.WaitForElementToDisappearAsync(loaderLocator, 60000, initialRetries: 5, retryDelay: 1000).GetAwaiter().GetResult();
            PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName).GetAwaiter().GetResult();
        }

        public override async Task ValidatePageReady()
        {
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, timeout: 40000);
            await CaptureApplicationIdsFromSessionStorageAsync();
        }

        public async Task<D2C_CallAgentPage> ClickOnSpecificCarrierCallAgent(string carrier)
        {
            var locator = $"//div[@carrier-name-automation='{carrier}']//button[contains(@aria-label, 'callAgentButton')]";
            
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click
            );

            return new D2C_CallAgentPage(BrowserManager, PageHelper, ScopeContext);
        }

        public async Task<List<string>> GetAllRatedCarriers()
        {
            var viewQuoteElements = await Page.Locator(CallAgentButtonLocator).AllAsync();
            var buyNowElements = await Page.Locator(BuyNowButtonLocator).AllAsync();

            var carriers = new List<string>();

            foreach (var element in viewQuoteElements.Concat(buyNowElements))
            {
                var carrierName = await element.GetAttributeAsync("carrier-name-automation");
                if (!string.IsNullOrEmpty(carrierName))
                {
                    carriers.Add(carrierName);
                }
            }

            _logger?.LogUiAction("Read", PageName, $"Rated carriers: [{string.Join(", ", carriers)}]");
            return carriers;
        }

        public async Task ClickOnSpecificCarrierViewDetails(string carrier)
        {
            var locator = $"//img[contains(@src, 'carrierDisplayName={carrier}')]/ancestor::div[contains(@class, 'left')]//following-sibling::div[contains(@class,'right')]//button[contains(@class,'details-button')]";
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click
            );
        }

        public async Task ClickOnFirstViewDetailsButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                "(//span[contains(text(),' View details ')])[1]",
                ElementAction.Click
            );
        }

        public async Task ClickOnSpecificCarrierBuyNow(string carrier)
        {
            var locator = $"//div[@carrier-name-automation='{carrier}']//span[contains(text(), 'Buy now')]";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                locator,
                ElementAction.Click
            );
        }
        public async Task<bool> IsBundleResultsDisplayed()
        {
            var element = await Page.Locator(BundleResultsLocator).CountAsync();
            return element > 0;
        }

        public async Task<bool> IsBuySeparatelyResultsDisplayed()
        {
            var element = await Page.Locator(BuySeparatelyResultsLocator).CountAsync();
            return element > 0;
        }

        public async Task ClickOnBuySeparatelyResults()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                BuySeparatelyResultsLocator,
                ElementAction.Click
            );
        }

        public async Task<List<string>> GetRatedCallToPurchaseCarriersForLob(string lob)
        {
            string locator;

            if (lob == "Bundle & Save")
            {
                locator = RatedCallToPurchaseCarriersBundleLocator;
            }
            else
            {
                locator = $"//button[contains(text(), '{lob}')]/following-sibling::ul[@class='coverages']/li//div[@class = 'coverage-wrapper']";
            }

            var elements = await Page.Locator(locator).AllAsync();
            var carriers = new List<string>();

            foreach (var element in elements)
            {
                var carrierName = await element.GetAttributeAsync("carrier-name-automation");
                if (!string.IsNullOrEmpty(carrierName))
                {
                    carriers.Add(carrierName);
                }
            }

            return carriers;
        }

        public async Task<List<string>> ReturnAllLobsTitle()
        {
            var elements = await Page.Locator("ul[aria-label='Quotes'] button.group-title").AllAsync();
            var lobs = new List<string>();

            foreach (var element in elements)
            {
                var buttonText = await element.EvaluateAsync<string>(@"
                    button => {
                        let textNodes = [];
                        for (let node of button.childNodes) {
                            if (node.nodeType === Node.TEXT_NODE && node.textContent.trim()) {
                                textNodes.push(node.textContent.trim());
                            }
                        }
                        return textNodes.join(' ');
                    }
                ");

                if (!string.IsNullOrEmpty(buttonText))
                {
                    buttonText = System.Text.RegularExpressions.Regex.Replace(buttonText, 
                        @"\s*(expand_less|expand_more|keyboard_arrow_up|keyboard_arrow_down|&nbsp;)\s*", 
                        "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    
                    buttonText = buttonText.TrimEnd('.', ':', '-', '–', '—').Trim();

                    if (!string.IsNullOrEmpty(buttonText) && 
                        !System.Text.RegularExpressions.Regex.IsMatch(buttonText, 
                            @"^(expand_less|expand_more|keyboard_arrow_up|keyboard_arrow_down|No quotes|quotes were returned).*", 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        lobs.Add(buttonText);
                    }
                }
            }

            return lobs;
        }

        public async Task ClickOnSpecificCarrierViewDetailsForLob(string lob, string carrier)
        {
            var locator = $"//button[contains(text(), '{lob}')]/following-sibling::ul//div[@carrier-name-automation='{carrier}']//span[contains(text(), 'View details')]";

            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                locator,
                ElementAction.Click
            );
        }

        public async Task SwitchFirstFloodToggleOn()
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                FloodToggleLocator,
                ElementAction.Click
            );
        }

        public async Task<List<string>> GetEntityCoverages(string entityName)
        {
            var locator = $"//div[normalize-space()='{entityName}']/ancestor::div[contains(@class,'entity-coverages-wrapper')]//div[@class='name']";
            var coverageElements = await Page.Locator(locator).AllAsync();
            var coverages = new List<string>();

            foreach (var element in coverageElements)
            {
                var text = await element.InnerTextAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    var cleanedText = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s*(help_outline|info_outline|help|info|tooltip)\s*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    
                    if (!string.IsNullOrEmpty(cleanedText))
                    {
                        coverages.Add(cleanedText);
                    }
                }
            }

            return coverages;
        }

        public async Task<List<string>> GetPolicyLobs()
        {
            var lobElements = await Page.Locator(LobCoverageNameLocator).AllAsync();
            var lobs = new List<string>();

            foreach (var element in lobElements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    lobs.Add(text);
                }
            }

            return lobs;
        }

        public async Task<List<string>> GetPolicyCoverages()
        {
            var coverageElements = await Page.Locator(PolicyCoverageNameLocator).AllAsync();
            var coverages = new List<string>();

            foreach (var element in coverageElements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    var cleanedText = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s*(help_outline|info_outline|help|info|tooltip)\s*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrEmpty(cleanedText))
                    {
                        coverages.Add(cleanedText);
                    }
                }
            }

            return coverages;
        }

        public async Task<List<string>> GetEntityNames()
        {
            var nameElements = await Page.Locator(EntityNameLocator).AllAsync();
            var names = new List<string>();

            foreach (var element in nameElements)
            {
                var text = await element.TextContentAsync();
                if (!string.IsNullOrEmpty(text))
                {
                    names.Add(text);
                }
            }

            return names;
        }

        public async Task SelectCarrierForComparison(string carrier)
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                string.Format(CompareCheckboxLocatorFormat, carrier),
                ElementAction.Click
            );
        }

        /// <summary>Waits for the compare footer to be raised by a carrier selection; false if it never is.</summary>
        public async Task<bool> IsComparisonFooterDisplayed(int timeoutMs = 10000)
        {
            try
            {
                await Page.Locator(ComparisonFooterLocator).WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        /// <summary>Carrier display names currently held in the compare footer, placeholders excluded.</summary>
        public async Task<List<string>> GetCarriersInComparisonFooter()
        {
            var carriers = new List<string>();

            foreach (var item in await Page.Locator(ComparisonFooterItemLocator).AllAsync())
            {
                var name = CarrierLogo.DisplayNameFrom(await item.Locator(ComparisonFooterCarrierLogoLocator).GetAttributeAsync("src"));
                if (!string.IsNullOrEmpty(name))
                    carriers.Add(name);
            }

            _logger?.LogUiAction("Read", PageName, $"Carriers in comparison footer: [{string.Join(", ", carriers)}]");
            return carriers;
        }

        public async Task<D2C_ComparisonPopUp> ClickCompare()
        {
            await PageHelper.InteractWithElement(
                LocatorType.CSS,
                CompareButtonLocator,
                ElementAction.Click
            );

            return new D2C_ComparisonPopUp(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }

        public async Task<bool> IsFloodCoverageDisplayed()
        {
            try
            {
                return await Page.Locator(FloodCoverageCardLocator).First.IsVisibleAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to check if flood coverage is displayed");
                return false;
            }
        }
    }
}