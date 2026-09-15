using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base
{
    public abstract class HQXConsumerBase : IInterview
    {
        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;
        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;

        protected abstract string PageIdentifier { get; }
        protected abstract string PageName { get; }
        public string PageDisplayName => PageName;

        protected virtual int PageTimeout => 30000;
        protected virtual Dictionary<string, UIElement> FieldRegistry => FormData.FieldRegistryHQXConsumer.Fields;
        protected UIElement Field(string fieldName) => FieldRegistry[fieldName];
        protected UIElement FieldWithValue(string fieldName, string value) => FieldRegistry[fieldName][value];
        protected HQXConsumerBase(
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
            await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName, PageTimeout);
        }
        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper);
        }

        public virtual async Task ClickContinue()
        {
            if (FieldRegistry.ContainsKey(FieldNames.NextButton))
            {
                await PageHelper.InteractWithField(FieldNames.NextButton);
            }
        }

        /// <summary>Returns whether the exit-to-auto link is on the stage, without clicking it.</summary>
        public Task<bool> IsExitToAutoQuoteLinkVisibleAsync() =>
            PageHelper.ElementExists(FormData.FieldsNameHQXConsumer.ExitToAutoQuoteLink);

        /// <summary>
        /// Clicks the exit-to-auto link and waits for the hand-back to Progressive.
        /// </summary>
        /// <returns>Whether the browser reached the Progressive domain.</returns>
        public async Task<bool> ClickExitToAutoQuoteAsync(int timeout = 30000)
        {
            _logger?.Info("Clicking the exit-to-auto-quote link to hand the consumer back to Progressive.");
            await PageHelper.InteractWithField(FormData.FieldsNameHQXConsumer.ExitToAutoQuoteLink);

            // Caught so the bool is real — the wait throws on timeout and never returns false.
            // See kb framework:navigation-waits.
            var handedBack = true;
            try
            {
                await PageHelper.WaitForNavigationOrUrlContainsAsync("progressive.com", timeout);
            }
            catch (NavigationException ex)
            {
                handedBack = false;
                _logger?.Info($"Hand-back wait failed: {ex.Message}");
            }

            _logger?.Info($"Hand-back to Progressive {(handedBack ? "completed" : "did not happen")}. URL: {Page.Url}");
            return handedBack;
        }

        /// <summary>
        /// How long to wait for the quote number to render. Only pages that show it read it, so
        /// this covers a render race rather than a genuine absence.
        /// </summary>
        private const int QuoteNumberTimeoutMs = 10_000;

        public async Task<string> GetFriendlyId()
        {
            try
            {
                // First: the page is not meant to show the number twice but has been seen to
                // (bug 254855), and evaluating against a multi-match locator throws.
                var quoteNumberElement = QuoteNumber.First;

                // CountAsync does not auto-wait, so counting straight away races the SPA and
                // reads zero while the quote number is still rendering — which is how the
                // placeholder used to escape into SEARCH APPLICATIONS. Wait for it instead.
                try
                {
                    await quoteNumberElement.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = QuoteNumberTimeoutMs
                    });
                }
                catch (Exception ex) when (ex is PlaywrightException or System.TimeoutException)
                {
                    _logger?.Info($"Quote number did not render within {QuoteNumberTimeoutMs}ms.");
                    return "NOT Found";
                }

                // The number is a bare text node alongside the "Quote #:" label spans,
                // so read the element's own text nodes rather than its full inner text.
                var quoteNumber = await quoteNumberElement.EvaluateAsync<string>(
                    "el => Array.from(el.childNodes).filter(n => n.nodeType === Node.TEXT_NODE).map(n => n.textContent).join('').trim()");
                if (string.IsNullOrWhiteSpace(quoteNumber))
                    return "NOT Found";

                ScopeContext.Set(ctx => ctx.FriendlyId, quoteNumber);
                return quoteNumber;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "Failed to get friendly ID");
                return "NOT Found";
            }
        }
        public async Task<IReadOnlyList<string>> GetQuestionLabelsAsync(bool includeHidden = false, ILocator? rootLocator = null)
        {
            rootLocator ??= Page.Locator("form.stage");
            var wrappers = rootLocator.Locator(".question-wrapper");
            var count = await wrappers.CountAsync();
            var labels = new List<string>(count);

            for (var i = 0; i < count; i++)
            {
                var wrapper = wrappers.Nth(i);
                var ariaHidden = await wrapper.GetAttributeAsync("aria-hidden");
                var hidden = await wrapper.GetAttributeAsync("hidden");
                var isHidden = string.Equals(ariaHidden, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(hidden, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(hidden, "hidden", StringComparison.OrdinalIgnoreCase);

                if (!includeHidden && isHidden)
                {
                    continue;
                }

                var labelLocator = wrapper.Locator(".question-label, .custom-checkbox-label, legend");
                if (await labelLocator.CountAsync() == 0)
                {
                    continue;
                }

                // Drop the ".question-description" hint span and the mandatory "*" so the label matches
                // the registry's plain Label. Order matters: the marker precedes the description span.
                var questionText = await labelLocator.First.EvaluateAsync<string>(
                    """
                    el => {
                        const clone = el.cloneNode(true);
                        clone.querySelectorAll('.question-description').forEach(node => node.remove());
                        return clone.textContent ?? '';
                    }
                    """);

                var label = TextHelper.StripRequiredMarker(TextHelper.CollapseWhitespace(questionText));
                if (!string.IsNullOrWhiteSpace(label))
                {
                    labels.Add(label);
                }
            }

            return labels;
        }

        public ILocator Title => Page.Locator("#title-element h1");
        public ILocator Subtitle => Page.Locator("#title-element h2");
        public ILocator LobType => Page.Locator("nav .lob-type");
        public ILocator ProgressiveFooter => Page.Locator("#ma-progressive-footnote");
        public ILocator Copyright => Page.Locator("app-footer .copyright");
        public ILocator QuoteNumber => Page.Locator("app-quote-number-bar .quote-number-text");
    }
}
