using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_OperatorPage : InterviewBase
    {
        public Product_OperatorPage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "_Operator";
        protected override string PageName => "Operator Page";

        private const string DateOfBirthLocator = "//*[contains(@id,'DOB')]//app-masked-input//input";
        private const string SsnLocator = "//*[contains(@id,'SSN')]//input";
        private const string DriverLicenseNumberLocator = "//*[contains(@id,'DriverLicenseNumber')]//input";

        // KLX-only: OperatorDOB uses a Material datepicker with class "hidden-input" on the
        // underlying matinput — Playwright's standard FillAsync rejects hidden elements.
        // Setting the value + dispatching input/change/blur events from the DOM bypasses
        // visibility checks and lets Angular pick up the new value.
        private const string KlxOperatorDobInputLocator =
            "//*[contains(@class,'OperatorDOBDateInput')]//input[@placeholder='MM/DD/YYYY']";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            string? dobValue = null;

            if (formData != null && formData.TryGetValue(OperatorDateOfBirth, out var dob) && !string.IsNullOrEmpty(dob))
            {
                var filtered = new Dictionary<string, string>(formData) { [OperatorDateOfBirth] = "" };
                dobValue = dob;
                formData = filtered;
            }

            await base.FillForm(formData);

            if (!string.IsNullOrEmpty(dobValue))
                await SetKlxOperatorDobAsync(dobValue);
        }

        private async Task SetKlxOperatorDobAsync(string value)
        {
            _logger?.Info($"Setting OperatorDOB = '{value}' via DOM (mat-datepicker hidden-input workaround)");

            var input = Page.Locator($"xpath={KlxOperatorDobInputLocator}").First;
            await input.EvaluateAsync(
                @"(el, v) => {
                    el.value = v;
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                    el.dispatchEvent(new Event('change', { bubbles: true }));
                    el.dispatchEvent(new Event('blur', { bubbles: true }));
                }",
                value);
        }

        #region PII Masking Methods
        public async Task<bool> CheckIfAllPiiFieldsAreMasked()
        {
            var dob = await PageHelper.GetValue(LocatorType.XPath, DateOfBirthLocator);
            var ssn = await PageHelper.GetValue(LocatorType.XPath, SsnLocator);
            var licenseNumber = await PageHelper.GetValue(LocatorType.XPath, DriverLicenseNumberLocator);

            return IsFieldContentMasked(dob) &&
                   (string.IsNullOrEmpty(ssn) || IsFieldContentMasked(ssn)) &&
                   (string.IsNullOrEmpty(licenseNumber) || IsFieldContentMasked(licenseNumber));
        }

        public bool IsFieldContentMasked(string fieldContent)
        {
            if (string.IsNullOrEmpty(fieldContent)) return false;
            return fieldContent.Contains("X") || fieldContent.Contains("*");
        }
        #endregion
    }
}
