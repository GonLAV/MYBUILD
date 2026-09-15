using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.Interview.Base;
using Microsoft.Playwright;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages
{
    public class Product_EmployeePage : InterviewBase
    {
        private const string EmployeeSearchInputLocator = "app-employee-search ng-select input[role='combobox']";
        private const string EmployeeRowLocator = "app-employee-table-row";
        private const string FullTimePlusButtonLocator = "app-control[id*='FullTime'] .icon-plus";
        private const string PayrollInputLocator = "app-control[id*='AnnualLocationPayroll'] input";

        public Product_EmployeePage(
            IBrowserManager browserManager,
            IPageHelper pageHelper,
            IScopeContext scopeContext,
            bool validatePageReady = true,
            IAutomationLogger? logger = null)
            : base(browserManager, pageHelper, scopeContext, validatePageReady, logger) { }

        protected override string PageIdentifier => "_Employee";
        protected override string PageName => "Employee Page";

        public override async Task FillForm(Dictionary<string, string>? formData = null)
        {
            if (formData == null)
            {
                await base.FillForm(null);
                return;
            }

            formData.TryGetValue(EmployeeClassCode, out var classCodeValue);
            formData.TryGetValue(EmployeePayroll, out var payrollValue);

            // Remove employee table fields - base FillForm can't handle the typeahead/row interaction
            var adjustedFormData = new Dictionary<string, string>(formData);
            adjustedFormData.Remove(EmployeeClassCode);
            adjustedFormData.Remove(EmployeeClassCodeText);
            adjustedFormData.Remove(EmployeePayroll);

            await base.FillForm(adjustedFormData);

            // Handle employee table: search typeahead, select class, fill row details
            var employeeSearch = Page.Locator(EmployeeSearchInputLocator);
            if (await employeeSearch.CountAsync() > 0 && !string.IsNullOrEmpty(classCodeValue))
            {
                await FillEmployeeTable(classCodeValue, payrollValue);
            }
        }

        private async Task FillEmployeeTable(string classCode, string? payrollValue = null)
        {
            payrollValue ??= "50000";
            var input = Page.Locator(EmployeeSearchInputLocator);
            await input.ClickAsync();
            await input.FillAsync("");
            await input.TypeAsync(classCode, new LocatorTypeOptions { Delay = 80 });

            // Wait for dropdown panel to appear with options
            var dropdownPanel = Page.Locator("ng-dropdown-panel");
            await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            var option = dropdownPanel.Locator(".ng-option").First;
            await option.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await option.ClickAsync();

            // Wait for dropdown to close
            await dropdownPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 10000 });

            _logger?.Info($"Selected employee class via typeahead: {classCode}");

            // Wait for the employee row to appear
            var employeeRow = Page.Locator(EmployeeRowLocator).First;
            await employeeRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // Click the + button to set full time employees to 1
            var fullTimePlus = employeeRow.Locator(FullTimePlusButtonLocator);
            if (await fullTimePlus.CountAsync() > 0)
            {
                await fullTimePlus.ClickAsync();
                _logger?.Debug("Clicked full time + button (set to 1)");
            }

            // Fill total payroll
            var payrollInput = employeeRow.Locator(PayrollInputLocator);
            if (await payrollInput.CountAsync() > 0)
            {
                await payrollInput.ClickAsync();
                await payrollInput.FillAsync(payrollValue);
                await payrollInput.PressAsync("Tab");
                _logger?.Debug($"Filled employee payroll: {payrollValue}");
            }
        }
    }
}
