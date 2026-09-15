using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_PolicyDatePickerPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext,
        bool validatePageReady = true,
        IAutomationLogger? logger = null
    ) : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        private const string ChoseMonthAndYearLocator = "//button[contains(@aria-label, 'Choose month and year')]";
        private const string YearMonthDayLocatorFormat = "//td[contains(@class, 'mat-calendar-body-cell')]//span[contains(text(), '{0}')]";
        private const string DayLocatorFormat = "//td[contains(@class, 'mat-calendar-body-cell')]//span[text()='{0}']";

        protected override string PageIdentifier => "-date";
        protected override string PageName => "Policy Date Picker Page";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            var isContinueDisabled = await IsContinueButtonDisabled();

            if (isContinueDisabled &&
                formData != null &&
                formData.TryGetValue(FieldNames.EffectiveDate, out var effectiveDate))
            {
                await SelectDate(effectiveDate);
            }
        }

        public async Task SelectDate(string dateInp)
        {
            var date = dateInp.Split('/');

            // Select year - Click on Choose month and year button
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                ChoseMonthAndYearLocator,
                ElementAction.Click
            );

            // Year
            string yearLocator = string.Format(YearMonthDayLocatorFormat, date[2]);
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                yearLocator,
                ElementAction.Click
            );

            // Month
            string monthLocator = string.Format(YearMonthDayLocatorFormat, date[1].ToUpper());
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                monthLocator,
                ElementAction.Click
            );

            // Day - Use exact text match with spaces around the day number
            string dayValue = $" {int.Parse(date[0])} ";
            string dayLocator = string.Format(DayLocatorFormat, dayValue);
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                dayLocator,
                ElementAction.Click
            );
        }
    }
}