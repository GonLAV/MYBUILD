using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages
{
    public class HQXConsumer_OverviewPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXConsumerBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        public static class SectionNames
        {
            public const string Property = "property";
            public const string Exterior = "exterior";
            public const string Interior = "interior";
            public const string PersonalInfo = "personal-info";
        }
        /// <summary>Home-style values the Property section's dwelling-type field accepts.</summary>
        public static class HomeStyles
        {
            public const string ManufacturedMobileHome = "Manufactured/Mobile Home";
        }

        public sealed record OverviewQuestion(string Id, string Label, bool IsHidden);
        protected override string PageIdentifier => "overview";
        protected override string PageName => "Overview Page";

        /// <summary>
        /// Root of a summary section. The page renders each section as an Angular Material
        /// accordion panel — <c>&lt;mat-expansion-panel data-section="property"&gt;</c> — not a div.
        /// </summary>
        private static string SectionRoot(string section) => $"mat-expansion-panel[data-section='{section}']";

        /// <summary>Expands the Property section and sets the home style, revealing it first if collapsed.</summary>
        public async Task SetHomeStyleAsync(string homeStyle)
        {
            _logger?.Info($"Setting the home style to '{homeStyle}' on the Property section.");
            await ExpandSectionAsync(SectionNames.Property);
            await PageHelper.InteractWithField(FieldNames.PLTypeOfDwelling, homeStyle);
        }

        public override async Task ValidatePageReady()
        {
            await base.ValidatePageReady();
            await GetFriendlyId();
        }

        /// <summary>
        /// Returns the section's collapsed summary line (the pipe-delimited text in the panel
        /// header, e.g. "Single Family House | Basic | Year built 2022 | 1,234 Sq. Ft").
        /// Readable whether the section is expanded or collapsed — it lives in the header.
        /// </summary>
        public async Task<string> GetSectionSummaryAsync(string section)
        {
            var summaryText = await PageHelper.GetValue(LocatorType.CSS, $"{SectionRoot(section)} .second-row .text");
            _logger?.Info($"{section} - summary: {summaryText}");
            return summaryText;
        }

        public async Task<bool> ValidateSectionValues(string section, params string[] expectedValues)
        {
            var summaryText = await GetSectionSummaryAsync(section);
            foreach (var value in expectedValues)
            {
                if (!summaryText.Contains(value))
                    return false;
            }
            return true;
        }

        public async Task ExpandSectionAsync(string section)
        {
            // The expand control is the panel header itself (role=button, carries aria-expanded);
            // ".section-toggle" is only the "Edit/view all" label rendered inside it.
            var header = Page.Locator($"{SectionRoot(section)} mat-expansion-panel-header");
            if (await header.CountAsync() == 0)
            {
                throw new PageElementException($"Overview '{section}' section header",
                    "Not present on the page. A missing section and a stale locator look identical here — check the panel markup before assuming the section is genuinely absent.");
            }

            var expanded = await header.GetAttributeAsync("aria-expanded");
            if (string.Equals(expanded, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await header.ClickAsync();
            await Page.Locator($"{SectionRoot(section)} mat-expansion-panel-header[aria-expanded='true']")
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        }

        public Task<IReadOnlyList<string>> GetQuestionLabelsAsync(string section, bool includeHidden = false)
        {
            var sectionLocator = Page.Locator(SectionRoot(section));
            return base.GetQuestionLabelsAsync(includeHidden, sectionLocator);
        }

        /// <summary>
        /// Returns the field IDs of all visible questions within a section.
        /// Visible means the question-wrapper does not carry aria-hidden="true".
        /// For wrappers without an id (e.g. personal-info inline name fields) the
        /// name attribute of the contained non-hidden inputs is used as the ID.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetVisibleQuestionIdsAsync(string section)
        {
            var sectionRoot = Page.Locator(SectionRoot(section));
            var wrappers = sectionRoot.Locator(".question-wrapper");
            var count = await wrappers.CountAsync();
            var ids = new List<string>();

            for (var i = 0; i < count; i++)
            {
                var wrapper = wrappers.Nth(i);
                var ariaHidden = await wrapper.GetAttributeAsync("aria-hidden");
                if (string.Equals(ariaHidden, "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                var wrapperId = await wrapper.GetAttributeAsync("id");
                if (!string.IsNullOrEmpty(wrapperId) && wrapperId.EndsWith("-question-wrapper"))
                {
                    ids.Add(wrapperId[..^"-question-wrapper".Length]);
                }
                else
                {
                    // personal-info inline fields have no wrapper id — collect by input name
                    var inputs = wrapper.Locator("input[name]:not([type='hidden'])");
                    var inputCount = await inputs.CountAsync();
                    for (var j = 0; j < inputCount; j++)
                    {
                        var name = await inputs.Nth(j).GetAttributeAsync("name");
                        if (!string.IsNullOrEmpty(name))
                            ids.Add(name);
                    }
                }
            }

            return ids;
        }

        /// <summary>
        /// Iterates all sections, collects question visibility and summary text failures without throwing.
        /// Returns a list of failure messages; empty list means all sections passed.
        /// </summary>
        public async Task<List<string>> CollectSectionValidationFailuresAsync(
            (string Name, string[] Expected, string[] SummaryValues)[] sections)
        {
            var failures = new List<string>();

            foreach (var (sectionName, expectedIds, summaryValues) in sections)
            {
                var actualIds = await GetVisibleQuestionIdsAsync(sectionName);
                _logger?.Info($"{sectionName} - expected: {string.Join(", ", expectedIds)}");
                _logger?.Info($"{sectionName} - actual:   {string.Join(", ", actualIds)}");

                var expected = new HashSet<string>(expectedIds, StringComparer.OrdinalIgnoreCase);
                var actual = new HashSet<string>(actualIds, StringComparer.OrdinalIgnoreCase);
                var missing = expected.Except(actual).OrderBy(x => x).ToList();
                var extra = actual.Except(expected).OrderBy(x => x).ToList();

                if (missing.Count > 0 || extra.Count > 0)
                    failures.Add($"Section '{sectionName}' question mismatch. Missing: [{string.Join(", ", missing)}] Extra: [{string.Join(", ", extra)}]");

                if (summaryValues.Length > 0)
                {
                    var summaryText = await GetSectionSummaryAsync(sectionName);
                    var missingValues = summaryValues.Where(v => !summaryText.Contains(v)).ToList();
                    if (missingValues.Count > 0)
                        failures.Add($"Section '{sectionName}' summary text is missing [{string.Join(", ", missingValues)}]. Actual: '{summaryText}'");
                }
            }

            return failures;
        }

    }
}