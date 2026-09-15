using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Screenshot;
using Microsoft.Playwright;
using ValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;

namespace Bolt.Automation.FrontEnds.PlaywrightBase;

public class PageHelper : IPageHelper, IDisposable
{
    private IPage _page;
    private readonly IAutomationLogger? _logger;
    private readonly IScopeContext? _scopeContext;
    private IScreenshotManager? _screenshotManager;
    private readonly IBrowserManager? _browserManager;

    private ElementInteractionHelper _elementInteraction;
    private ValueHelper _valueHelper;
    private WaitHelper _waitHelper;
    private NavigationHelper _navigationHelper;
    private PageValidationHelper _validationHelper;
    private TableHelper _tableHelper;
    private FieldInteractionHelper _fieldInteraction;
    private PendingRequestTracker? _requestTracker;
    private NetworkCorrelationCapture? _networkCapture;

    public IScopeContext? ScopeContext => _scopeContext;
    public PendingRequestTracker? RequestTracker => _requestTracker;
    public PageValidationHelper ValidationHelper => _validationHelper;

    public PageHelper(IPage page, IAutomationLogger? logger = null,
        IScreenshotManager? screenshotManager = null, IScopeContext? scopeContext = null, IBrowserManager? browserManager = null)
    {
        _page = page;
        _logger = logger;
        _scopeContext = scopeContext;
        _browserManager = browserManager;
        _screenshotManager = screenshotManager;

        InitializeHelpers();

        // Subscribe to page changes if BrowserManager is provided
        if (_browserManager != null)
        {
            _browserManager.PageChanged += OnPageChanged;
        }
    }

    private void InitializeHelpers()
    {
        _requestTracker?.Dispose();
        _requestTracker = new PendingRequestTracker(_page, _logger);
        _networkCapture?.Dispose();
        _networkCapture = new NetworkCorrelationCapture(_page, _logger);
        _elementInteraction = new ElementInteractionHelper(_page, _logger, _screenshotManager);
        _valueHelper = new ValueHelper(_page);
        _waitHelper = new WaitHelper(_page, _logger);
        _navigationHelper = new NavigationHelper(_page, _logger);
        _validationHelper = new PageValidationHelper(_page, _logger, _networkCapture);
        _tableHelper = new TableHelper(_page);
        _fieldInteraction = new FieldInteractionHelper(_page, _elementInteraction, _logger, _scopeContext);
    }

    private void OnPageChanged(IPage newPage)
    {
        try
        {
            _page = newPage;
            _screenshotManager = _browserManager?.GetScreenshotManager() ?? _screenshotManager;
            // Reinitialize all helpers with the new page
            InitializeHelpers();
            _logger?.Info($"PageHelper updated to new page: {newPage.Url}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error updating PageHelper page reference: {ex.Message}");
        }
    }

    #region Element Interaction Methods

    public Task<string?> GetFieldAttribute(string fieldName, string attributeName, int timeout = 5000)
        => _fieldInteraction.GetFieldAttribute(fieldName, attributeName, timeout);

    public Task<PageHelper> InteractWithElement(
        LocatorType locatorType,
        LocatorSet locatorValues,
        ElementAction action,
        ElementInteractionOptions? options = null)
        => _elementInteraction.InteractWithElement(locatorType, locatorValues, action, options, this);

    public Task<PageHelper> InteractWithElement(
        LocatorType locatorType,
        LocatorSet locatorValues,
        UIFieldType fieldType,
        string value,
        ElementInteractionOptions? options = null)
        => _elementInteraction.InteractWithElement(locatorType, locatorValues, fieldType, value, options, this);

    public Task<PageHelper> InteractWithElement(UIElement element)
        => _elementInteraction.InteractWithElement(element, this);

    public Task<PageHelper> InteractWithElement(UIElement element, string? value, PageHelper? pageHelper = null)
        => _elementInteraction.InteractWithElement(element, value, pageHelper ?? this);

    #endregion

    #region Field Interaction Methods

    public Task<PageHelper> InteractWithField(string fieldName, string? value = null)
        => _fieldInteraction.InteractWithField(fieldName, value, this);

    public Task<PageHelper> InteractWithField(string fieldName, ElementInteractionOptions options)
        => _fieldInteraction.InteractWithField(fieldName, options, this);

    public Task<string> GetFieldValue(UIElement element, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3)
        => _fieldInteraction.GetFieldValue(element.Strategy, element.Locators.First(), valueType, timeout, waitForVisibility, retries);

    public Task<string> GetFieldValue(string fieldName, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3)
        => _fieldInteraction.GetFieldValue(fieldName, valueType, timeout, waitForVisibility, retries);

    public Task<IReadOnlyList<string>> GetFieldDropdownListValues(string fieldName, int timeout = 5000, bool openDropdown = true)
        => _fieldInteraction.GetFieldDropdownListValues(fieldName, timeout, openDropdown);

    public Task<string> GetFieldValue(LocatorType locatorType, string locatorValue, ValueType valueType = ValueType.Text, int timeout = 0, bool waitForVisibility = false, int retries = 3)
        => _fieldInteraction.GetFieldValue(locatorType, locatorValue, valueType, timeout, waitForVisibility, retries);

    public Task<bool> ElementExists(string fieldName, int timeout = 1000, string? expectedPlaceholder = null)
        => _fieldInteraction.ElementExists(fieldName, timeout, expectedPlaceholder);

    public Task<bool> ElementExists(LocatorType locatorType, string locatorValue, int timeout = 1000, string? expectedPlaceholder = null)
        => _fieldInteraction.ElementExists(locatorType, locatorValue, timeout, expectedPlaceholder);

    public Task<bool> ElementExists(UIElement field, int timeout = 1000, string? placeholderValue = null)
        => _fieldInteraction.ElementExists(field, timeout, placeholderValue);

    public Task<bool> IsFieldEnabled(string fieldName, int timeout = 3000)
        => _fieldInteraction.IsFieldEnabled(fieldName, timeout);

    public Task TriggerValidationAsync(string fieldName, int timeout = 10000)
        => _fieldInteraction.TriggerValidationAsync(fieldName, timeout);
    public string GetValidationLocator(string fieldName)
    => _fieldInteraction.GetValidationLocator(fieldName);

    #endregion

    #region Value Methods

    public Task<string> GetValue(
        LocatorType locatorType,
        string locatorValue,
        ValueType valueType = ValueType.Text,
        int timeout = 0,
        bool waitForVisibility = false,
        int retries = 3)
        => _valueHelper.GetValue(locatorType, locatorValue, valueType, timeout, waitForVisibility, retries);

    #endregion

    #region Wait Methods

    // These timeout defaults MUST match IPageHelper exactly. C# binds an optional-arg default from
    // the CALL SITE static type, not the runtime type, so a value here that disagrees with the
    // interface silently applies to concrete-typed callers only - and nobody reading either file can
    // tell which one is in force. All four used to say 30000 against the interface's 10000/15000.
    public Task<ILocator?> WaitForElementAsync(ILocator locator, int timeout = 10000, bool waitForVisibility = false, int retries = 3)
        => _waitHelper.WaitForElementAsync(locator, timeout, waitForVisibility, retries);

    public Task<IReadOnlyList<string>> WaitForElementsValuesAsync(ILocator elementsLocator, int timeout = 10000)
        => _waitHelper.WaitForElementsValuesAsync(elementsLocator, timeout);

    public Task<bool> WaitForNavigationOrUrlContainsAsync(string urlPart, int timeout = 15000, bool exactMatch = false)
        => _waitHelper.WaitForNavigationOrUrlContainsAsync(urlPart, timeout, exactMatch);

    public Task<string> WaitForNavigationOrUrlContainsAsync(IReadOnlyList<string> urlParts, int timeout = 15000)
        => _waitHelper.WaitForNavigationOrUrlContainsAsync(urlParts, timeout);

    public Task<bool> WaitForElementToDisappearAsync(ILocator element, int timeout = 10000, int initialRetries = 3, int retryDelay = 500)
        => _waitHelper.WaitForElementToDisappearAsync(element, timeout, initialRetries, retryDelay);

    public Task<IResponse> WaitForApiResponseAsync(
    Func<Task> action,
    string endpoint,
    int expectedStatus = 200,
    int timeoutMs = 30000,
    Func<IResponse, bool>? predicate = null)
    => _waitHelper.WaitForApiResponseAsync(action, endpoint, expectedStatus, timeoutMs, predicate);

    #endregion

    #region Navigation Methods

    public Task ClickBrowserBackButton()
        => _navigationHelper.ClickBrowserBackButton();

    public Task RefreshPageAsync(double waitSeconds = 0)
        => _navigationHelper.RefreshPageAsync(waitSeconds);

    public Task ScrollPageAsync(string position)
        => _navigationHelper.ScrollPageAsync(position);

    #endregion

    #region Table Methods

    public Task SelectTableRowAsync(int rowIndex, bool doubleClick = false, int timeoutMs = 30000)
        => _tableHelper.SelectTableRowAsync(rowIndex, doubleClick, timeoutMs);

    public Task ClickTableRowCheckboxAsync(string columnName, string value)
        => _tableHelper.ClickTableRowCheckboxAsync(columnName, value);

    public Task ClickTableRowCheckboxByIndexAsync(int rowIndex)
        => _tableHelper.ClickTableRowCheckboxByIndexAsync(rowIndex);

    public Task ClickTableHeaderCheckboxAsync()
        => _tableHelper.ClickTableHeaderCheckboxAsync();

    public Task<bool> IsTableRowCheckboxCheckedByIndexAsync(int rowIndex)
        => _tableHelper.IsTableRowCheckboxCheckedByIndexAsync(rowIndex);

    public Task<bool> IsAllTableRowCheckboxesCheckedAsync()
        => _tableHelper.IsAllTableRowCheckboxesCheckedAsync();

    public Task<string> GetTableCellValueAsync(string columnName, string columnValue, string targetColumn)
        => _tableHelper.GetTableCellValueAsync(columnName, columnValue, targetColumn);

    public Task ClickTableCellButtonAsync(string columnName, string columnValue, string targetColumn)
        => _tableHelper.ClickTableCellButtonAsync(columnName, columnValue, targetColumn);

    public Task<bool> IsSpecificColumnHaveData(string expectedData, string column)
        => _tableHelper.IsSpecificColumnHaveData(expectedData, column);

    public Task<List<string>> GetColumnData(string columnName)
        => _tableHelper.GetColumnData(columnName);

    public Task<bool> IsTableDisplayed(int timeoutMs = 30000)
        => _tableHelper.IsTableDisplayed(timeoutMs);

    public Task<int> GetRowCount()
        => _tableHelper.GetRowCount();

    public Task<Dictionary<string, string>> GetRowData(int rowIndex)
        => _tableHelper.GetRowData(rowIndex);

    public Task<IReadOnlyList<string>> GetTableHeadersAsync(bool includeEmpty = false)
       => _tableHelper.GetTableHeadersAsync(includeEmpty);

    #endregion

    #region Validation Methods

    public Task ValidatePageReadyAsync(string expectedUrlPart, string pageName, int timeout = 30000)
        => _validationHelper.ValidatePageReadyAsync(expectedUrlPart, pageName, timeout);

    public Task<List<string>> GetValidationMessagesAsync(string[]? customSelectors = null)
        => _validationHelper.GetValidationMessagesAsync(customSelectors);

    public Task<string?> GetFieldValidationMessageAsync(string fieldLocatorName)
        => _validationHelper.GetFieldValidationMessageAsync(fieldLocatorName);

    public async Task<string> GetFieldValidationTextAsync(string fieldName)
    {
        var validationPath = GetValidationLocator(fieldName);
        var validationLocator = _page.Locator(validationPath);
        var validationText = await validationLocator.InnerTextAsync();
        return validationText.Replace('\u00A0', ' ').Trim();
    }

    public async Task<string> GetFieldValidationColorAsync(string fieldName)
    {
        var validationPath = GetValidationLocator(fieldName);
        var validationLocator = _page.Locator(validationPath);
        return await validationLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).color");
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>Returns the correlation id from the most recent 4xx/5xx browser network response.</summary>
    public string? GetLatestCorrelationId() => _networkCapture?.LatestCorrelationId;

    /// <summary>Returns the full list of 4xx/5xx browser network responses captured since the last page load.</summary>
    public IReadOnlyList<NetworkCorrelationEntry> GetNetworkErrors() => _networkCapture?.History ?? [];

    public void Dispose()
    {
        // Unsubscribe from page changes to prevent memory leaks
        if (_browserManager != null)
        {
            _browserManager.PageChanged -= OnPageChanged;
        }
        _requestTracker?.Dispose();
        _networkCapture?.Dispose();
    }

    #endregion
}
