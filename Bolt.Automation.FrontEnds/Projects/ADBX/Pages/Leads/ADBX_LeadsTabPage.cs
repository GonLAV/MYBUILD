using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using System.Globalization;
using System.Text.RegularExpressions;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads
{
    public class ADBX_LeadsTabPage : ADBX_BasePage
    {
        protected override string PageIdentifier => "leads";
        protected override string PageName => "Leads Grid Page";

        public ADBX_LeadsTabPage(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger)
        {
            WaitForLoaderToDisappear().GetAwaiter().GetResult();
        }

        /// <summary>How long the footer gets to stop reading "undefined" after a page change.</summary>
        private const int FooterSettleTimeoutMs = 10000;

        private const int FooterPollIntervalMs = 200;

        private static readonly Regex SettledFooter =
            new(@"Showing\s+\d+\s*-\s*\d+\s+of\s+\d+", RegexOptions.Compiled);

        /// <summary>
        /// Reads the grid footer once it settles - mid-page-change it reads "undefined of undefined".
        /// </summary>
        public async Task<string> ReadGridFooterAsync()
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(FooterSettleTimeoutMs);
            string footer;

            while (true)
            {
                footer = (await PageHelper.GetFieldValue(PageGridSizeMessage))
                    .Replace('\u00A0', ' ')
                    .Trim();

                if (SettledFooter.IsMatch(footer) || DateTime.UtcNow >= deadline)
                {
                    break;
                }

                await Task.Delay(FooterPollIntervalMs);
            }

            if (!SettledFooter.IsMatch(footer))
            {
                _logger?.Warning($"Grid footer never settled within {FooterSettleTimeoutMs}ms: '{footer}'.");
            }

            return footer;
        }

        /// <summary>Pulls the total out of a footer like "Showing 1 - 10 of 137".</summary>
        public static int ParseFooterTotal(string footer)
        {
            var match = Regex.Match(footer, @"of\s+([\d,]+)\s*$");

            if (!match.Success)
            {
                throw new FormatException($"Could not read the total from the grid footer '{footer}'.");
            }

            return int.Parse(
                match.Groups[1].Value.Replace(",", string.Empty),
                CultureInfo.InvariantCulture);
        }

    }
}
