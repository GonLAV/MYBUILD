using System.Globalization;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;
using D2CBase = Bolt.Automation.FrontEnds.Projects.D2C.Base.D2CBase;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Pages
{
    public class D2C_DriverHistoryPage(
        IBrowserManager browserManager,
        IPageHelper pageHelper,
        IScopeContext scopeContext, 
        bool validatePageReady = true,
        IAutomationLogger? logger = null)
        : D2CBase(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        #region Locators
        private const string ContinueButtonLocator = "(//button[@data-automation-data='driver-history'])";
        private const string SkipAllDriversButtonLocator = "(//button[@class = 'skip-button ng-star-inserted'])[1]";
        #endregion

        protected override string PageIdentifier => "driver-history";
        protected override string PageName => "Driver History Page";

        public override async Task FillForm(Dictionary<string, string> policyData)
        {
            await SetSpecificStepper("accidents", "no");
            await SetSpecificStepper("violations", "no");
            await SetSpecificStepper("losses", "no");
        }

        public async Task SetSpecificStepper(string label, string value, string type = null, string when = null, int index = 1)
        {
            value = value.ToLower();

            if (value == "no")
            {
                string stepperSelector = $"(//div[@class='left']//label[contains(text(),'{label}')]//parent::div//following-sibling::div[@class='right']//span[contains(text(),'No')])[{index}]";

                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    stepperSelector,
                    ElementAction.Click
                );
            }
            else
            {
                string stepperSelector = $"(//div[@class='left']//label[contains(text(),'{label}')]//parent::div//following-sibling::div[@class='right']//span[contains(text(),'Yes')])[{index}]";

                var stepper = Page.Locator(stepperSelector);
                if (await stepper.CountAsync() > 0)
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        stepperSelector,
                        ElementAction.Click
                    );
                }

                await SetDriverHistoryValues(label, type, when, index);
            }
        }

        public async Task SetDriverHistoryValues(string label, string type, string when, int index)
        {
            if (!string.IsNullOrEmpty(type))
            {
                string stepperTypeSelector = $"(//app-dropdown[@data-automation='select-type-{label}'])[{index}]";
                var stepperType = Page.Locator($"xpath={stepperTypeSelector}");

                if (await stepperType.CountAsync() > 0)
                {

                    var arrow = stepperType.Locator(".ng-arrow-wrapper").First;
                    await arrow.ClickAsync(new LocatorClickOptions { Timeout = 5000 });

                    var panel = Page.Locator(".ng-dropdown-panel");
                    await panel.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 5000
                    });

                    var option = panel.Locator(".ng-option").First;
                    await option.ClickAsync();

                    await panel.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Hidden,
                        Timeout = 5000
                    });
                }
            }

            if (!string.IsNullOrEmpty(when))
            {
                string stepperWhenSelector = $"(//masked-input[@data-automation='select-date-{label}']//input)[{index}]";
                var stepperWhen = Page.Locator(stepperWhenSelector);

                if (await stepperWhen.CountAsync() > 0)
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        stepperWhenSelector,
                        ElementAction.Fill,
                        new ElementInteractionOptions { 
                            Value = when,
                        }
                    );
                }
            }

            if (label == "losses")
            {
                string lossesAmountSelector = $"(//input[@id='AutoLossesAmount'])[{index}]";
                var lossesAmountElement = Page.Locator(lossesAmountSelector);

                if (await lossesAmountElement.CountAsync() > 0)
                {
                    await PageHelper.InteractWithElement(
                        LocatorType.XPath,
                        lossesAmountSelector,
                        ElementAction.Fill,
                        new ElementInteractionOptions { 
                            Value = "500",
                        }
                    );
                }
            }
        }

        public async Task AddAnotherButton(string value)
        {
            string buttonSelector = $"//div[@class='left']//label[contains(text(),'{value}')]//parent::div//following-sibling::div//button[@aria-label='+ Add another']";
            var button = Page.Locator(buttonSelector);

            if (await button.CountAsync() > 0)
            {
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    buttonSelector,
                    ElementAction.Click,
                    new ElementInteractionOptions { IgnoreIfNotFound = true }
                );
            }
        }

        public async Task<bool> IsAddAnotherButtonDisplay()
        {
            var locator = Page.Locator("//button[@aria-label='+ Add another']");
            return await locator.CountAsync() > 0 && await locator.IsVisibleAsync();
        }

        public async Task<int> GetNumberOfIncidents(string label)
        {
            string selector = $"app-dropdown[data-automation='select-type-{label}']";
            return await Page.Locator(selector).CountAsync();
        }

        public async Task ClickContinueButton(int index = 1)
        {
            string buttonSelector = $"({ContinueButtonLocator})[{index}]";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                buttonSelector,
                ElementAction.Click
            );

            await WaitForLoaderToDisappear();
        }

        public async Task AddIncidents(string category, string description, int num)
        {
            // Wait for the incident section to be ready
            await Task.Delay(1000);

            int index = 1;
            for (int i = 0; i < num; i++)
            {
                await SetSpecificStepper(
                    category,
                    "yes",
                    $" {description} ",
                    DateTime.Today.AddYears(-1).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture),
                    index
                );
                if (i < num - 1)
                {
                    await AddAnotherButton(category);
                }
                index++;
            }
        }

        public async Task SkipAllIncidentsButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                SkipAllDriversButtonLocator,
                ElementAction.Click
            );

            await WaitForLoaderToDisappear();
        }
    }
}