using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using InterviewFieldNames = Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_CLPolicyPage : InterviewBase
    {
        public Product_CLPolicyPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "CL_Policy";
        protected override string PageName => "CL Policy Page";

        // KLX CL Policy uses a Material datepicker for EffectiveDate (hidden-input under
        // a visible MM/DD/YYYY placeholder). Same DOM-event workaround as OperatorDOB.
        private const string EffectiveDateInputLocator =
            "//*[contains(@class,'PolicyData.EffectiveDate')]//input[@placeholder='MM/DD/YYYY']";

        // KLX top-level CL Policy ng-selects have a floated `<app-form-label>` that
        // intercepts pointer events on the input area (same pattern as Vehicle Radius).
        // For these we click the `.ng-arrow-wrapper` (outside the label overlay) to open
        // the panel, then either pick a specific option text or fall back to the first
        // available option ("use judgment" per spec).
        private static readonly (string FieldName, string ClassFragment, bool FirstIfMissing)[] LabelInterceptedFields =
        {
            (InterviewFieldNames.CurrentBopCarrier,                       "PolicyData.CurrentBopCarrier", false),
            (InterviewFieldNames.CL_BIPD,                                  "PolicyData.CL_BIPD",           true),
            (InterviewFieldNames.CL_MedPay,                                "PolicyData.CL_MedPay",         true),
            (InterviewFieldNames.CombinedUninsuredUnderinsuredMotorist,   "PolicyData.CL_Combined_UM_UIM", true),
            (InterviewFieldNames.UninsuredMotoristPropertyDamage,         "PolicyData.CL_UMPD",           true),
            // Per-vehicle deductibles: KLX option lists vary per dropdown; fall back to
            // the first available option if the requested value isn't present.
            (InterviewFieldNames.CL_ComprehensiveDeductible,              "CL_Comprehensive",             true),
            (InterviewFieldNames.CL_CollisionDeductible,                  "CL_Collision",                 true),
            (InterviewFieldNames.CL_FireTheftCAC,                         "CL_FireTheftCAC",              true),
        };

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var labelInterceptValues = new Dictionary<string, string>();
            string? effectiveDateValue = null;

            if (formData != null)
            {
                var filtered = new Dictionary<string, string>(formData);

                if (filtered.TryGetValue(EffectiveDate, out var date) && !string.IsNullOrEmpty(date))
                {
                    effectiveDateValue = date;
                    filtered[EffectiveDate] = "";
                }

                foreach (var (fieldName, _, _) in LabelInterceptedFields)
                {
                    if (filtered.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value))
                    {
                        labelInterceptValues[fieldName] = value;
                        filtered[fieldName] = "";
                    }
                }

                formData = filtered;
            }
            await SelectLossesAnswer("No");

            await base.FillForm(formData);

            foreach (var (fieldName, classFragment, firstIfMissing) in LabelInterceptedFields)
            {
                if (labelInterceptValues.TryGetValue(fieldName, out var value))
                    await SelectViaArrowAsync(classFragment, value, firstIfMissing);
            }

            if (!string.IsNullOrEmpty(effectiveDateValue))
                await SetEffectiveDateAsync(effectiveDateValue);
        }

        // Clicks the ng-arrow-wrapper to bypass the floated-label overlay, then picks an
        // option by text — falling back to the first available option when the text isn't
        // present (used for fields whose valid options aren't known up-front).
        //
        // Waits up to ~3s for the container to appear so this works for fields that are
        // conditionally rendered only after earlier selections settle (e.g. Fire&Theft
        // appears after Comprehensive/Collision are filled).
        private async Task SelectViaArrowAsync(string classFragment, string optionText, bool firstIfMissing)
        {
            var container = Page.Locator($"xpath=//*[contains(@class,'{classFragment}')]").First;
            try
            {
                await container.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 3000
                });
            }
            catch (TimeoutException)
            {
                _logger?.Warning($"CLPolicy: container '{classFragment}' not present, skipping");
                return;
            }

            var arrow = container.Locator(".ng-arrow-wrapper").First;
            try
            {
                await arrow.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
            }
            catch (TimeoutException)
            {
                _logger?.Warning($"CLPolicy: arrow click timed out for '{classFragment}'");
                return;
            }

            var panel = Page.Locator(".ng-dropdown-panel");
            await panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });

            var option = panel.Locator(".ng-option", new() { HasText = optionText }).First;
            if (await option.CountAsync() == 0)
            {
                if (firstIfMissing)
                {
                    option = panel.Locator(".ng-option").First;
                    _logger?.Info($"CLPolicy: option '{optionText}' not found in '{classFragment}'; selecting first available");
                }
                else
                {
                    _logger?.Warning($"CLPolicy: option '{optionText}' not found in '{classFragment}'");
                    // Close the panel by pressing Escape so it doesn't intercept later actions.
                    await Page.Keyboard.PressAsync("Escape");
                    return;
                }
            }

            await option.ClickAsync();
            await panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 5000
            });
        }

        private async Task SetEffectiveDateAsync(string value)
        {
            _logger?.Info($"Setting EffectiveDate = '{value}' via DOM (mat-datepicker workaround)");

            var input = Page.Locator($"xpath={EffectiveDateInputLocator}").First;
            if (await input.CountAsync() == 0) return;

            await input.EvaluateAsync(
                @"(el, v) => {
                    el.value = v;
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                    el.dispatchEvent(new Event('change', { bubbles: true }));
                    el.dispatchEvent(new Event('blur', { bubbles: true }));
                }",
                value);
        }

        private async Task SelectLossesAnswer(string answer)
        {
            var control = Page.Locator("[id='PolicyData.PLHaveAnyLosses']");
            if (await control.CountAsync() == 0) return;

            // Check if already selected
            var checkedInput = control.Locator("input.checked");
            if (await checkedInput.CountAsync() > 0) return;

            // Click the label containing the answer text (Yes or No)
            var label = control.Locator($"label.switcher-wrapper:has-text('{answer}')");
            await label.ClickAsync();

            _logger?.Info($"Selected losses answer: {answer}");
        }
    }
}
