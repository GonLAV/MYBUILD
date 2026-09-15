using System.Text.RegularExpressions;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Base;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Popups
{
    /// <summary>
    /// The side-by-side carrier comparison opened from the rates-page compare footer.
    /// </summary>
    public class D2C_ComparisonPopUp(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : D2CPopupBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string CarrierColumnLocator = "comparison-popup .rates-wrapper .th .compare-item";
        private const string CarrierLogoLocator = "img.carrier-logo";
        private const string PremiumLocator = ".price-container .price";
        // The table is rendered twice, once per breakpoint; the desktop body is the one that carries
        // the coverage name and every carrier's value in the same row.
        private const string CoverageRowLocator = "comparison-popup .tbody-desktop .coverages-row";
        private const string CoverageNameLocator = ".coverage-name";
        private const string CoverageValueLocator = ".coverage-price .td";
        #endregion

        private static readonly Regex TooltipGlyphPattern = new(@"\s*(help_outline|info_outline|help|info|tooltip)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override string PopupIdentifier => "comparison";
        protected override string PopupName => "Comparison Popup";

        /// <summary>Carrier display names, left to right, as the comparison columns present them.</summary>
        public async Task<List<string>> GetComparedCarriers()
        {
            var carriers = new List<string>();

            foreach (var column in await Page.Locator(CarrierColumnLocator).AllAsync())
            {
                var name = CarrierLogo.DisplayNameFrom(await column.Locator(CarrierLogoLocator).GetAttributeAsync("src"));
                if (!string.IsNullOrEmpty(name))
                    carriers.Add(name);
            }

            _logger?.LogUiAction("Read", PopupName, $"Compared carriers: [{string.Join(", ", carriers)}]");
            return carriers;
        }

        /// <summary>Monthly premium per comparison column, in the same order as <see cref="GetComparedCarriers"/>.</summary>
        public async Task<List<string>> GetComparedPremiums()
        {
            var premiums = new List<string>();

            foreach (var column in await Page.Locator(CarrierColumnLocator).AllAsync())
            {
                var premium = await column.Locator(PremiumLocator).TextContentAsync();
                if (!string.IsNullOrWhiteSpace(premium))
                    premiums.Add(premium.Trim());
            }

            _logger?.LogUiAction("Read", PopupName, $"Compared premiums: [{string.Join(", ", premiums)}]");
            return premiums;
        }

        /// <summary>Each coverage row keyed by its name, with one value per compared carrier.</summary>
        public async Task<Dictionary<string, List<string>>> GetCoverageRows()
        {
            var rows = new Dictionary<string, List<string>>();

            foreach (var row in await Page.Locator(CoverageRowLocator).AllAsync())
            {
                var rawName = await row.Locator(CoverageNameLocator).TextContentAsync();
                var name = TooltipGlyphPattern.Replace((rawName ?? string.Empty).Trim(), string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                var values = new List<string>();
                foreach (var cell in await row.Locator(CoverageValueLocator).AllAsync())
                    values.Add((await cell.TextContentAsync() ?? string.Empty).Trim());

                rows[name] = values;
            }

            _logger?.LogUiAction("Read", PopupName, $"Coverage rows: [{string.Join(", ", rows.Keys)}]");
            return rows;
        }
    }
}
