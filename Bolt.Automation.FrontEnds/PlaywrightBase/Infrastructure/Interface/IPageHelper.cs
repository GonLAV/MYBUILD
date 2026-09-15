using Bolt.Automation.Common.Context;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Microsoft.Playwright;
using ValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;

public interface IPageHelper
{
    IScopeContext? ScopeContext { get; }
    Task ClickBrowserBackButton();
    Task RefreshPageAsync(double waitSeconds = 0);
    Task ScrollPageAsync(string position);
    Task SelectTableRowAsync(int rowIndex, bool doubleClick = false, int timeoutMs = 30000);
    Task ClickTableRowCheckboxAsync(string columnName, string value);
    Task ClickTableRowCheckboxByIndexAsync(int rowIndex);
    Task ClickTableHeaderCheckboxAsync();
    Task<bool> IsTableRowCheckboxCheckedByIndexAsync(int rowIndex);
    Task<bool> IsAllTableRowCheckboxesCheckedAsync();
    Task ClickTableCellButtonAsync(string columnName, string columnValue, string targetColumn);
    Task<string> GetTableCellValueAsync(string columnName, string columnValue, string targetColumn);
    Task ValidatePageReadyAsync(string expectedUrlPart, string pageName, int timeout = 30000);
    Task<ILocator?> WaitForElementAsync(ILocator locator, int timeout = 10000, bool waitForVisibility = false, int retries = 3);
    Task<IReadOnlyList<string>> WaitForElementsValuesAsync(ILocator elementsLocator, int timeout = 10000);
    Task<bool> WaitForElementToDisappearAsync(ILocator element, int timeout = 10000, int initialRetries = 3, int retryDelay = 500);
    Task<bool> WaitForNavigationOrUrlContainsAsync(string urlPart, int timeout = 15000, bool exactMatch = false);

    /// <summary>
    /// Waits until the URL contains ANY of <paramref name="urlParts"/> and returns the fragment
    /// that matched — for a conditionally-rendered step, where the caller cannot know in advance
    /// which page the app will land on.
    /// </summary>
    Task<string> WaitForNavigationOrUrlContainsAsync(IReadOnlyList<string> urlParts, int timeout = 15000);
    Task<IResponse> WaitForApiResponseAsync(
       Func<Task> action,
       string endpoint,
       int expectedStatus = 200,
       int timeoutMs = 30000,
       Func<IResponse, bool>? predicate = null);

    Task<string> GetValue(LocatorType locatorType, string locatorValue, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3);
    Task<PageHelper> InteractWithElement(
        LocatorType locatorType,
        LocatorSet locatorValues,
        ElementAction action,
        ElementInteractionOptions? options = null);
    Task<PageHelper> InteractWithElement(
        LocatorType locatorType,
        LocatorSet locatorValues,
        UIFieldType fieldType,
        string value,
        ElementInteractionOptions? options = null);
    Task<PageHelper> InteractWithElement(UIElement element);
    Task<PageHelper> InteractWithField(string fieldName, string? value = null);
    Task<PageHelper> InteractWithField(string fieldName, ElementInteractionOptions options);
    Task<string> GetFieldValue(UIElement element, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3);
    Task<string> GetFieldValue(string fieldName, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3);
    Task<string> GetFieldValue(LocatorType locatorType, string locatorValue, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3);
    Task<IReadOnlyList<string>> GetFieldDropdownListValues(string fieldName, int timeout = 5000, bool openDropdown = true);
    Task<bool> ElementExists(string fieldName, int timeout = 1000, string? expectedPlaceholder = null);
    Task<bool> ElementExists(LocatorType locatorType, string locatorValue, int timeout = 1000, string? expectedPlaceholder = null);
    Task<bool> ElementExists(UIElement field, int timeout = 1000, string? placeholderValue = null);
    Task<bool> IsFieldEnabled(string fieldName, int timeout = 3000);
    Task TriggerValidationAsync(string fieldName, int timeout = 10000);
    string GetValidationLocator(string fieldName);
    Task<List<string>> GetValidationMessagesAsync(string[]? customSelectors = null);
    Task<string?> GetFieldValidationMessageAsync(string fieldLocatorName);
    Task<string> GetFieldValidationTextAsync(string fieldName);
    Task<string> GetFieldValidationColorAsync(string fieldName);
    Task<string?> GetFieldAttribute(string fieldName, string attributeName, int timeout = 5000);
    Task<bool> IsSpecificColumnHaveData(string expectedData, string column);
    Task<List<string>> GetColumnData(string columnName);
    Task<bool> IsTableDisplayed(int timeoutMs = 30000);
    Task<int> GetRowCount();
    Task<Dictionary<string, string>> GetRowData(int rowIndex);
    Task<IReadOnlyList<string>> GetTableHeadersAsync(bool includeEmpty = false);

    /// <summary>Returns the correlation id from the most recent 4xx/5xx browser network response.</summary>
    string? GetLatestCorrelationId();

    /// <summary>Returns all 4xx/5xx browser network responses captured since the last page load.</summary>
    IReadOnlyList<NetworkCorrelationEntry> GetNetworkErrors();
}