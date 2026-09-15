using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.Popups
{
    public class ADBX_AddEditViewPopup(IBrowserManager browserManager, IPageHelper pageHelper, IScopeContext scopeContext, bool validatePageReady = true, IAutomationLogger? logger = null)
        : ADBX_BasePopup(browserManager, pageHelper, scopeContext, validatePageReady, logger)
    {
        protected override string PopupIdentifier => "adbx";
        protected override string PopupName => "Create View";

        private const string AddConditionButton = "//button[contains(text(), 'Add Condition')]";
        private const string ViewNameInput = "//input[@data-test-id='view-name']";
        private const string EmptyConditionDropdown = "//ng-select[@data-test-id='condition-field' and not(.//span[contains(@class, 'ng-value-label')])]";

        private const string StatusCondition = "Status";
        private const string AssignedToCondition = "Assigned To";
        private const string SmartSelectionPrefix = "#";

        private static readonly HashSet<string> DefaultConditions = new(StringComparer.OrdinalIgnoreCase)
        {
            StatusCondition,
            AssignedToCondition
        };

        private static readonly Dictionary<string, string> SmartOperatorMap = new()
        {
            { "==", "variableEq" },
            { "<>", "variableNeq" },
            { "In", "variableIn" }
        };

        public async Task SetViewName(string name)
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                ViewNameInput,
                UIFieldType.Input,
                name
                );
        }

        public async Task AddCondition(string conditionName, string conditionOperator, string conditionValue)
        {
            await EnsureConditionFieldExists(conditionName);
            await SelectConditionOperator(conditionName, conditionOperator, conditionValue);
            await SelectConditionValue(conditionName, conditionValue);
        }

        private async Task EnsureConditionFieldExists(string conditionName)
        {
            if (IsDefaultCondition(conditionName))
            {
                _logger?.Info($"{PopupName}: Using pre-existing '{conditionName}' condition.");
                return;
            }

            await ClickAddConditionButton();
            await SelectNewConditionField(conditionName);
        }

        private async Task ClickAddConditionButton()
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                AddConditionButton,
                ElementAction.Click);
        }

        private async Task SelectNewConditionField(string conditionName)
        {
            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                EmptyConditionDropdown,
                UIFieldType.Dropdown,
                conditionName
                );

            _logger?.Info($"{PopupName}: Selected '{conditionName}' from condition dropdown.");
        }

        private async Task SelectConditionOperator(string conditionName, string conditionOperator, string conditionValue)
        {
            bool isSmartSelection = IsSmartSelection(conditionValue);
            var conditionOperatorLocator = $"//span[contains(text(), '{conditionName}')]/ancestor::div[@data-test-id]//select[@data-test-id='condition-operator']";

            if (isSmartSelection)
            {
                if (!SmartOperatorMap.TryGetValue(conditionOperator, out var operatorValue))
                {
                    throw new ArgumentException($"Unsupported Smart operator: {conditionOperator}");
                }

                var selectElement = Page.Locator(conditionOperatorLocator);
                await selectElement.SelectOptionAsync(new[] { operatorValue });
                _logger?.Info($"{PopupName}: Selected Smart operator '{conditionOperator}' (value: {operatorValue}) for '{conditionName}'.");
            }
            else
            {
                await PageHelper.InteractWithElement(
                    LocatorType.XPath,
                    conditionOperatorLocator,
                    UIFieldType.Dropdown,
                    conditionOperator
                    );
                _logger?.Info($"{PopupName}: Selected regular operator '{conditionOperator}' for '{conditionName}'.");
            }
        }

        private async Task SelectConditionValue(string conditionName, string conditionValue)
        {
            var conditionValueLocator = $"//span[contains(text(), '{conditionName}')]/ancestor::div[@data-test-id]//label[@data-test-id]//ng-select";

            await PageHelper.InteractWithElement(
                LocatorType.XPath,
                conditionValueLocator,
                UIFieldType.Dropdown,
                conditionValue
                );

            _logger?.Info($"{PopupName}: Selected value '{conditionValue}' for '{conditionName}'.");
        }

        private static bool IsDefaultCondition(string conditionName)
            => DefaultConditions.Contains(conditionName);

        private static bool IsSmartSelection(string conditionValue)
            => conditionValue.StartsWith(SmartSelectionPrefix);
    }
}
