using System.Text.RegularExpressions;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Base;
using HtmlAgilityPack;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages
{
    public class HQXAgent_CarrierQuestionsPage(IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : HQXAgentBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PageIdentifier => "CarrierQuestions";
        protected override string PageName => "PGR Carrier Questions Page";

        // Generalized: List of control-wrapper IDs for which value labels should be skipped
        private static readonly HashSet<string> ControlWrappersToSkipValues = new()
        {
            "PLPlumbingUpdated-control-wrapper"
            // Add more control-wrapper IDs here as needed
        };

        public override async Task FillForm(Dictionary<string, string> formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext);
        }

        /// <summary>Answers only the fields gating the bridge for <paramref name="carrier"/>; none if it has no entry.</summary>
        /// <remarks>Not <see cref="FillForm"/> — the registry is sparse and shared, so filling the whole page
        /// reveals required sub-questions and leaves the form invalid.</remarks>
        public async Task AnswerBridgeQuestionsAsync(CarrierEnums carrier)
        {
            var fillData = CarrierQuestionsFillData.GetFillData(carrier);
            _logger?.Info($"Answering {fillData.Count} bridge-gating question(s) for {carrier}.");

            foreach (var (fieldName, value) in fillData)
            {
                await PageHelper.InteractWithField(fieldName, value);
            }
        }

        public async Task<List<string>> ExtractCarrierQuestionsAsync()
        {
            string html = await Page.ContentAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var questions = new HashSet<string>();
            // Extract <legend> elements (main question text)
            var legends = doc.DocumentNode
                .SelectNodes("//app-carrier-questions//legend")
                ?? new HtmlNodeCollection(null);
            foreach (var legendNode in legends)
            {
                // Only add <legend> if NOT inside a <control-checkbox-list>
                if (!IsInsideControlCheckboxList(legendNode))
                {
                    var clean = CleanQuestionText(legendNode.InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(clean))
                        questions.Add(clean);
                }
            }
            // Optionally, add <label> elements that are not Yes/No or button labels
            var labelNodes = doc.DocumentNode
                .SelectNodes("//app-carrier-questions//label")
                ?? new HtmlNodeCollection(null);
            foreach (var labelNode in labelNodes)
            {
                var clean = CleanQuestionText(labelNode.InnerText.Trim());
                if (string.IsNullOrWhiteSpace(clean) || IsYesNoLabel(clean))
                    continue;
                // If inside <control-checkbox-list>, skip label if it matches the legend (subtitle)
                var checkboxListParent = GetParentControlCheckboxList(labelNode);
                if (checkboxListParent != null)
                {
                    var legendNode = checkboxListParent.SelectSingleNode(".//legend");
                    if (legendNode != null)
                    {
                        var legendText = CleanQuestionText(legendNode.InnerText.Trim());
                        if (clean.Equals(legendText, StringComparison.OrdinalIgnoreCase))
                            continue; // skip subtitle label
                    }
                }
                // Skip if label is for a disabled input or contains a disabled input
                if (IsLabelForDisabledInput(labelNode, doc))
                    continue;
                // Generalized: skip value labels for any control-wrapper in the skip list
                var parentControlWrapper = labelNode.Ancestors("div").FirstOrDefault(div => ControlWrappersToSkipValues.Contains(div.Id));
                if (parentControlWrapper != null)
                    continue;
                questions.Add(clean);
            }
            return questions.ToList();
        }

        private static HtmlNode? GetParentControlCheckboxList(HtmlNode node)
        {
            var parent = node.ParentNode;
            while (parent != null)
            {
                if (parent.Name.Equals("control-checkbox-list", StringComparison.OrdinalIgnoreCase))
                    return parent;
                parent = parent.ParentNode;
            }
            return null;
        }

        private static bool IsInsideControlCheckboxList(HtmlNode node)
        {
            // Traverse up the parent chain to see if any parent is <control-checkbox-list>
            var parent = node.ParentNode;
            while (parent != null)
            {
                if (parent.Name.Equals("control-checkbox-list", StringComparison.OrdinalIgnoreCase))
                    return true;
                parent = parent.ParentNode;
            }
            return false;
        }

        private static string CleanQuestionText(string text)
        {
            // Remove extra whitespace and trailing colons
            var cleaned = Regex.Replace(text, @"\s+", " ").Trim();
            if (cleaned.EndsWith(":"))
                cleaned = cleaned.TrimEnd(':').Trim();
            return cleaned;
        }

        private static bool IsYesNoLabel(string text)
        {
            var lower = text.Trim().ToLowerInvariant();
            return lower == "yes" || lower == "no";
        }

        private static bool IsLabelForDisabledInput(HtmlNode labelNode, HtmlDocument doc)
        {
            // Check if label contains a disabled input
            var input = labelNode.SelectSingleNode(".//input[@disabled]");
            if (input != null)
                return true;
            // Check if label's 'for' attribute points to a disabled input elsewhere
            var forAttr = labelNode.GetAttributeValue("for", null);
            if (!string.IsNullOrEmpty(forAttr))
            {
                var inputById = doc.DocumentNode.SelectSingleNode($"//input[@id='{forAttr}' and @disabled]");
                if (inputById != null)
                    return true;
            }
            return false;
        }

        public override async Task ClickContinue()
        {
            await ClickContinueButton(true);
            var loaderOverlay = Page.Locator("//div[@class='overlay']");
            try
            {
                PageHelper.WaitForElementToDisappearAsync(loaderOverlay, 300000, initialRetries: 5, retryDelay: 1000).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Loader overlay wait encountered an exception (continuing): {ex.Message}");
            }
        }
    }
}
