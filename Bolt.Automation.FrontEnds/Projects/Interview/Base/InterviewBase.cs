using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Base
{
    public abstract class InterviewBase : IInterview
    {
        public const string ContinueButton = "button.Next, .button.Confirm, button.Get.quotes";
        public const string Loader = ".loader, .loading-overlay, .spinner";
        public const string ErrorMessage = ".error-message, .field-error, .form-error, .validation-error";
        public const string MandatoryFields = ".ng-invalid:visible, .error-message:visible, [aria-invalid='true'], .has-error";
        public const string BundleBoxLocator = ".bundle-box";
        public const string HomeButtonLocator = "//button[contains(@class, 'Home')]";
        public const string BackButton = "app-control#Home\\.Back app-button, button.Back.button";

        protected readonly IPageHelper _pageHelper;
        public IPageHelper PageHelper => _pageHelper;
        protected readonly IBrowserManager BrowserManager;
        protected readonly IAutomationLogger? _logger;
        protected readonly IScopeContext ScopeContext;

        private IPage? _page;
        public IPage Page => _page ??= BrowserManager.GetCurrentTab()!;
        protected abstract string PageIdentifier { get; }
        protected abstract string PageName { get; }

        private static readonly string[] ContinueButtonSelectors = new[]
        {
            ContinueButton
        };
        private static readonly string[] BackButtonSelectors = new[]
{
            BackButton
        };

        protected InterviewBase(
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
                    // Browser is still on the page that would not advance - the only place to read it.
                    LogUnsatisfiedFieldsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    throw;
                }
            }
        }

        public virtual async Task ValidatePageReady()
        {
            try
            {
                await PageHelper.ValidatePageReadyAsync(PageIdentifier, PageName);
                await CaptureApplicationIdsFromSessionStorageAsync();
                if (string.IsNullOrEmpty(ScopeContext.Data.FriendlyId))
                    await TryCaptureFriendlyIdFromDomAsync();
                _logger?.LogBusinessRule("PageReady", true, $"{PageName} page validated successfully");
            }
            catch (Exception)
            {
                _logger?.LogBusinessRule("PageReady", false, $"{PageName} page validation failed");
                throw;
            }
        }

        /// <summary>
        /// Closes the alert modal that picking an industry raises to describe the NAIC class. Its backdrop
        /// intercepts every later click on the page - LOB tiles and the Next button included - so it has to
        /// go before the flow continues. No-op when no modal is open.
        /// </summary>
        protected async Task DismissAlertModalAsync()
        {
            var modal = Page.Locator("div.model-outer app-alert-modal");
            if (await modal.CountAsync() == 0 || !await modal.First.IsVisibleAsync()) return;

            var closeButton = Page.Locator("div.model-outer app-alert-modal button.Close");
            if (await closeButton.CountAsync() == 0)
                closeButton = Page.Locator("div.model-outer button.modal-outer-close");

            await closeButton.First.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
            await modal.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });

            _logger?.Info("Dismissed the industry description alert modal");
        }

        public virtual async Task ClickContinue()
        {
            await ClickContinueButton(true);
        }

        public async Task ClickContinueButton(bool throwErrorMessage = true)
        {
            if (!await TryStandardButtonClick(ContinueButtonSelectors))
            {
                if (throwErrorMessage)
                {
                    throw new PageElementException("Next/Continue button", $"Could not find or click any continue button on page: {Page.Url}");
                }
            }
        }

        // Validation state, not a field a test drives, so it is read off the page rather than the registry.
        // Markers verified on captured Staging pages: the radio <input> carries "checked"/"unchecked" (the
        // DOM property does not survive capture), a filled container carries "has-value", and ng-invalid
        // on its own is far too noisy - the app leaves it on plenty of correctly filled controls.
        private const string BlockingFieldsScript = @"
            () => {
              const out = [];
              for (const host of document.querySelectorAll('app-control')) {
                const idc = (host.getAttribute('id') || '') + ' ' + (host.getAttribute('class') || '');
                const hit = idc.match(/PolicyData\.[A-Za-z_0-9]+/);
                if (!hit || !host.getClientRects().length) continue;

                const radios = Array.prototype.slice.call(host.querySelectorAll('input[type=radio]'));
                let why = null, clickLost = false;
                if (radios.length > 0) {
                  if (radios.every(r => r.classList.contains('unchecked'))) {
                    clickLost = radios.some(r => r.classList.contains('ng-dirty'));
                    why = clickLost ? 'no option selected (the click did not register)' : 'no option selected';
                  }
                } else if (host.querySelector('.ng-invalid') && !host.querySelector('.has-value')) {
                  why = 'empty';
                }
                if (!why) continue;

                const labelEl = host.querySelector('label.title, .form-item-label, label');
                let label = labelEl ? labelEl.textContent.replace(/\s+/g, ' ').trim() : '';
                if (label.length > 80) label = label.slice(0, 80) + '...';
                const required = label.indexOf('*') >= 0 || !!host.querySelector('.required');
                if (!required && !clickLost) continue;

                out.push(hit[0] + (required ? ' [required]' : ' [optional]') + ' - ' + why
                         + (label ? ' - ""' + label + '""' : ''));
                if (out.length >= 12) break;
              }
              return out;
            }";

        /// <summary>
        ///     Names the required fields the stuck page never got an answer for, which otherwise surfaces
        ///     only as a timeout on the next page. Labels and field names only - answers are PII.
        /// </summary>
        /// <remarks>
        ///     Must run on validation failure, not on the Next click: the switcher classes lag Angular by a
        ///     change-detection pass, so a click-time read flags fields that are about to be fine.
        /// </remarks>
        private async Task LogUnsatisfiedFieldsAsync()
        {
            if (_logger is null) return;

            try
            {
                var blocking = await Page.EvaluateAsync<string[]>(BlockingFieldsScript);
                if (blocking is { Length: > 0 })
                {
                    _logger.Warning(
                        $"{PageName} was not reached; the page still showing has {blocking.Length} required field(s) unanswered: "
                        + string.Join(" | ", blocking));
                }
            }
            catch (Exception ex)
            {
                // Execution context gone means the page navigated - the good case.
                _logger.Trace($"{PageName}: could not read unsatisfied fields ({ex.GetType().Name})");
            }
        }

        public async Task ClickBack()
        {
            await ClickBackButton(true);
        }
        public async Task ClickBackButton(bool throwErrorMessage = true)
        {
            if (!await TryStandardButtonClick(BackButtonSelectors))
            {
                if (throwErrorMessage)
                {
                    throw new PageElementException("Next/Continue button", $"Could not find or click any continue button on page: {Page.Url}");
                }
            }
        }

        private async Task<bool> TryStandardButtonClick(string[] locators)
        {
            foreach (var selector in locators)
            {
                var locator = CreateLocator(selector);
                if (await TryClickingElements(locator))
                    return true;
            }
            return false;
        }

        private ILocator CreateLocator(string locatorValue)
        {
            return locatorValue.StartsWith("//")
                ? Page.Locator($"xpath={locatorValue}")
                : Page.Locator(locatorValue);
        }

        private async Task<bool> TryClickingElements(ILocator locator)
        {
            int count = await locator.CountAsync();
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var specificButton = locator.Nth(i);
                    if (!await specificButton.IsVisibleAsync())
                        continue;
                    await specificButton.ClickAsync(new() { Timeout = 5000 });
                    return true;
                }
                catch
                {
                    // Optionally log
                }
            }
            return false;
        }

        public virtual async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var (pageSpecificData, fields) = FormDataHelper.GetFieldsForPage(this, ScopeContext, formData);
            await Page.FillRelevantFields(pageSpecificData, fields, ScopeContext, PageHelper, _logger);
        }

        public async Task LogApplicationDetails()
        {
            string applicationId = await GetApplicationId();
            string friendlyId = await GetFriendlyId();
            Console.WriteLine($"application id=[{applicationId}]");
            Console.WriteLine($"friendly id=[{friendlyId}]");
        }

        private async Task<string> GetApplicationId()
        {
            var appIdElement = Page.Locator("div[application_id*='-']");
            if (await appIdElement.CountAsync() > 0)
            {
                return await appIdElement.GetAttributeAsync("application_id") ?? "NOT Found";
            }
            return "NOT Found";
        }


        protected async Task CaptureApplicationIdsFromSessionStorageAsync()
        {
            try
            {
                var metadata = await BrowserStorageHelper.GetSessionStorageJsonElementAsync(Page, "state", "interviewMetadata", _logger);
                if (metadata == null)
                    return;

                var friendlyId = metadata.Value.TryGetProperty("friendlyId", out var fid) ? fid.GetString() : null;
                var applicationId = metadata.Value.TryGetProperty("applicationId", out var aid) ? aid.GetString() : null;
                var applicantId = metadata.Value.TryGetProperty("applicantId", out var apid) ? apid.GetString() : null;

                var hasNewData = (!string.IsNullOrEmpty(friendlyId) && friendlyId != ScopeContext.Data.FriendlyId)
                              || (!string.IsNullOrEmpty(applicationId) && applicationId != ScopeContext.Data.ExternalId)
                              || (!string.IsNullOrEmpty(applicantId) && applicantId != ScopeContext.Data.ApplicantId);

                if (!hasNewData)
                    return;

                if (!string.IsNullOrEmpty(friendlyId)) ScopeContext.Set(ctx => ctx.FriendlyId, friendlyId);
                if (!string.IsNullOrEmpty(applicationId)) ScopeContext.Set(ctx => ctx.ExternalId, applicationId);
                if (!string.IsNullOrEmpty(applicantId)) ScopeContext.Set(ctx => ctx.ApplicantId, applicantId);

                _logger?.Info($"Interview Session IDs — FriendlyId: {friendlyId} | ApplicationId: {applicationId} | ApplicantId: {applicantId}");

                ScopeContext.Data.IdentifierHistory.Add(new IdentifierSnapshot
                {
                    Source = "SessionStorage",
                    FriendlyId = friendlyId,
                    ExternalId = applicationId,
                    ApplicantId = applicantId
                });
            }
            catch (Exception ex)
            {
                _logger?.Debug($"Could not capture Interview session IDs: {ex.Message}");
            }
        }

        private async Task TryCaptureFriendlyIdFromDomAsync()
        {
            var friendlyId = await GetFriendlyId();
            if (friendlyId != "NOT Found")
            {
                ScopeContext.Set(ctx => ctx.FriendlyId, friendlyId);
                _logger?.Info($"FriendlyId captured from DOM: {friendlyId}");
            }
        }

        private async Task<string> GetFriendlyId()
        {
            var meterTitle = Page.Locator("h6.progress-meter-title");
            if (await meterTitle.CountAsync() > 0)
            {
                var text = await meterTitle.InnerTextAsync();
                return text.Replace("Quote ", string.Empty).Trim();
            }
            return "NOT Found";
        }

        public virtual async Task ClickOfflineRequest()
        {
            await PageHelper.InteractWithField(OfflineRequestButton);
        }

        public async Task<ADBX_HomePage> ClickHome()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                HomeButtonLocator,
                ElementAction.Click
            );
            return new ADBX_HomePage(BrowserManager, PageHelper, ScopeContext, logger: _logger);
        }
    }
}
