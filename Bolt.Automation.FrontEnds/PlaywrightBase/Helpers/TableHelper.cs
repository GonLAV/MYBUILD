using Bolt.Automation.Common.Exceptions;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

public class TableHelper
{
    private readonly IPage _page;
    private const string TableLocator = "//ngx-datatable[contains(@class,'datatable')]";
    private const string TableHeaderCellLocator = "//datatable-header-cell";
    private const string TableRowLocator = "//datatable-body-row";
    private const string TableCellLocator = "//datatable-body-cell";
    private const string TableHeaderCheckboxLocator = "//datatable-header//input[@id='checkbox_all']";
    private const int DefaultGridTimeoutMs = 30000;

    public TableHelper(IPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    public async Task SelectTableRowAsync(int rowIndex, bool doubleClick = false, int timeoutMs = DefaultGridTimeoutMs)
    {
        if (rowIndex < 1)
            throw new ArgumentException("Row index must be at least 1.", nameof(rowIndex));

        var table = await WaitForGridReadyAsync(timeoutMs);

        var filteredRows = await GetFilteredTableRows(table);
        var rowCount = await filteredRows.CountAsync();
        if (rowCount == 0)
            throw new PageElementException("table", $"the grid rendered but holds no data rows (URL: '{_page.Url}')");
        if (rowIndex > rowCount)
            throw new Exception($"Row index {rowIndex} exceeds available rows ({rowCount}).");
        var targetRow = filteredRows.Nth(rowIndex - 1);
        if (doubleClick)
            await targetRow.DblClickAsync();
        else
        {
            var firstCell = targetRow.Locator("td").First;
            if (await firstCell.CountAsync() > 0)
                await firstCell.ClickAsync();
            else
                await targetRow.ClickAsync();
        }
    }

    public async Task ClickTableRowCheckboxAsync(string columnName, string columnValue)
    {
        var row = await FindRowByColumnValueAsync(columnName, columnValue);
        if (row == null)
            throw new PageElementException($"row {columnName}='{columnValue}' not found");

        var checkbox = await GetRowCheckboxAsync(row);
        await checkbox.ClickAsync();
    }

    public async Task ClickTableRowCheckboxByIndexAsync(int rowIndex)
    {
        var targetRow = await GetRowByIndexAsync(rowIndex);
        var checkbox = await GetRowCheckboxAsync(targetRow);
        await checkbox.ClickAsync();
    }

    public async Task ClickTableHeaderCheckboxAsync()
    {
        var table = await WaitForGridReadyAsync();

        var headerCheckbox = await GetHeaderCheckboxAsync(table);
        await headerCheckbox.ClickAsync();
    }

    public async Task<bool> IsAllTableRowCheckboxesCheckedAsync()
    {
        var table = await WaitForGridReadyAsync();

        var filteredRows = await GetFilteredTableRows(table);
        var rowCount = await filteredRows.CountAsync();
        if (rowCount == 0)
            throw new PageElementException("table", $"the grid rendered but holds no data rows (URL: '{_page.Url}')");

        for (var i = 1; i <= rowCount; i++)
        {
            if (!await IsTableRowCheckboxCheckedByIndexAsync(i))
                return false;
        }

        return true;
    }

    private async Task<ILocator> GetRowByIndexAsync(int rowIndex)
    {
        if (rowIndex < 1)
            throw new ArgumentException("Row index must be at least 1.", nameof(rowIndex));

        var table = await WaitForGridReadyAsync();

        var filteredRows = await GetFilteredTableRows(table);
        var rowCount = await filteredRows.CountAsync();
        if (rowCount == 0)
            throw new PageElementException("table", "no data rows found");
        if (rowIndex > rowCount)
            throw new Exception($"Row index {rowIndex} exceeds available rows ({rowCount}).");

        return filteredRows.Nth(rowIndex - 1);
    }

    private static async Task<ILocator> GetRowCheckboxAsync(ILocator row)
    {
        var checkboxLabel = row.Locator("label[for^='checkbox_single-']");
        if (await checkboxLabel.CountAsync() > 0)
            return checkboxLabel.First;

        var checkbox = row.Locator("input[type='checkbox'].row-checkbox");
        if (await checkbox.CountAsync() > 0)
            return checkbox.First;

        throw new PageElementException("checkbox", "not found in the row");
    }

    private static async Task<ILocator> GetHeaderCheckboxAsync(ILocator table)
    {
        var checkboxLabel = table.Locator("datatable-header label[for='checkbox_all']");
        if (await checkboxLabel.CountAsync() > 0)
            return checkboxLabel.First;

        var checkbox = table.Locator(TableHeaderCheckboxLocator);
        if (await checkbox.CountAsync() > 0)
            return checkbox.First;

        throw new PageElementException("header checkbox", "not found");
    }

    public async Task<bool> IsTableRowCheckboxCheckedByIndexAsync(int rowIndex)
    {
        var targetRow = await GetRowByIndexAsync(rowIndex);
        var checkbox = await GetRowCheckboxAsync(targetRow);
        return await checkbox.IsCheckedAsync();
    }

    public async Task<string> GetTableCellValueAsync(string columnName, string columnValue, string targetColumn)
    {
        var row = await FindRowByColumnValueAsync(columnName, columnValue);
        if (row == null)
            return null;

        var targetCellButton = row.Locator($".grid-prop-{targetColumn} button");
        if (await targetCellButton.CountAsync() > 0)
            return (await targetCellButton.InnerTextAsync()).Trim();

        var targetCellSpan = row.Locator($".grid-prop-{targetColumn} span");
        if (await targetCellSpan.CountAsync() > 0)
            return (await targetCellSpan.InnerTextAsync()).Trim();

        var targetCellDiv = row.Locator($".grid-prop-{targetColumn}");
        if (await targetCellDiv.CountAsync() > 0)
            return (await targetCellDiv.InnerTextAsync()).Trim();

        return null;
    }

    public async Task ClickTableCellButtonAsync(string columnName, string columnValue, string targetColumn)
    {
        var row = await FindRowByColumnValueAsync(columnName, columnValue);
        if (row == null)
            throw new PageElementException($"row {columnName}='{columnValue}'");

        var targetCellButton = row.Locator($".grid-prop-{targetColumn} button");
        if (await targetCellButton.CountAsync() > 0)
        {
            await targetCellButton.ClickAsync();
            return;
        }
        throw new PageElementException($"button in column '{targetColumn}' for row '{columnValue}'");
    }

    public async Task<int> GetRowCount()
    {
        var table = _page.Locator(TableLocator);
        await table.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
        
        var filteredRows = await GetFilteredTableRows(table);
        return await filteredRows.CountAsync();
    }

    public async Task<Dictionary<string, string>> GetRowData(int rowIndex)
    {
        if (rowIndex < 1)
            throw new ArgumentException("Row index must be at least 1.", nameof(rowIndex));

        var table = await WaitForGridReadyAsync();

        var filteredRows = await GetFilteredTableRows(table);
        var rowCount = await filteredRows.CountAsync();
        
        if (rowCount == 0)
            throw new Exception("No data rows found in the table.");
        
        if (rowIndex > rowCount)
            throw new Exception($"Row index {rowIndex} exceeds available rows ({rowCount}).");

        // Get headers to map column names
        var headers = table.Locator(TableHeaderCellLocator);
        var headerCount = await headers.CountAsync();
        var columnNames = new List<string>();

        for (int i = 0; i < headerCount; i++)
        {
            var header = headers.Nth(i);
            var headerText = await header.TextContentAsync();
            columnNames.Add(headerText?.Trim() ?? $"Column_{i + 1}");
        }

        // Get the specific row data
        var targetRow = filteredRows.Nth(rowIndex - 1);
        var cells = targetRow.Locator(TableCellLocator);
        var cellCount = await cells.CountAsync();

        var rowData = new Dictionary<string, string>();

        for (int i = 0; i < Math.Min(cellCount, columnNames.Count); i++)
        {
            var cell = cells.Nth(i);
            var cellText = await cell.TextContentAsync();
            rowData[columnNames[i]] = cellText?.Trim() ?? string.Empty;
        }

        return rowData;
    }

    public async Task<IReadOnlyList<string>> GetTableHeadersAsync(bool includeEmpty = false)
    {
        var table = await WaitForGridReadyAsync(requireContent: false);

        var headers = table.Locator(TableHeaderCellLocator);
        var headerCount = await headers.CountAsync();
        var columnNames = new List<string>();

        for (int i = 0; i < headerCount; i++)
        {
            var header = headers.Nth(i);
            if (!await header.IsVisibleAsync())
                continue;

            var headerText = await header.InnerTextAsync();
            var normalizedHeader = NormalizeHeaderText(headerText);

            if (includeEmpty || !string.IsNullOrWhiteSpace(normalizedHeader))
                columnNames.Add(normalizedHeader);
        }

        return columnNames;
    }

    private static string NormalizeHeaderText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Replace('\u00A0', ' ');
        return string.Join(" ", normalized.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private Task<ILocator> GetFilteredTableRows(ILocator table)
    {
        var contentRows = table.Locator(TableRowLocator);

        return Task.FromResult(contentRows.Filter(new() { HasNotText = "No Data Available" }).Filter(new() { HasNotText = "Open Closed Pending All" }));
    }

    private void ElementNullCheck(ILocator element, string fieldName)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element), $"Field [{fieldName}] element is null");
    }

    public virtual async Task<List<string>> GetColumnData(string columnName, ILocator table = null)
    {
        table ??= await WaitForGridReadyAsync();
        ElementNullCheck(table, "table");
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        List<string> colValues = new();

        await WaitForGridReadyAsync(table);

        var headers = table.Locator(TableHeaderCellLocator);
        var headerCount = await headers.CountAsync();

        int? targetColumnIndex = null;
        for (int i = 0; i < headerCount; i++)
        {
            var headerText = (await headers.Nth(i).InnerTextAsync())?.Trim();
            if (string.Equals(headerText, columnName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                targetColumnIndex = i;
                break;
            }
        }

        if (targetColumnIndex == null)
            return colValues;

        var rows = await GetFilteredTableRows(table);
        var rowElements = await rows.AllAsync();

        foreach (var row in rowElements)
        {
            var cell = row.Locator(TableCellLocator).Nth(targetColumnIndex.Value);
            if (await cell.CountAsync() == 0)
                continue;

            var cellText = (await cell.InnerTextAsync())?.Trim();
            if (!string.IsNullOrEmpty(cellText))
                colValues.Add(cellText);
        }

        return colValues;
    }
    public async Task<bool> IsSpecificColumnHaveData(string expectedData, string column)
    {
        if (string.IsNullOrWhiteSpace(expectedData))
            throw new ArgumentException("Expected data cannot be null or empty.", nameof(expectedData));
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(column));

        var table = await WaitForGridReadyAsync();

        var columnData = await GetColumnData(column, table);

        return columnData.Any(x => x.Contains(expectedData, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Waits for the grid to be both present and populated, and reports the current URL on failure
    /// instead of a bare Playwright locator timeout that names only the selector.
    /// ngx-datatable renders its shell before its rows arrive, so waiting only for the table to be
    /// visible lets the row read that follows return 0 and surface as a false "no data rows found".
    /// </summary>
    private Task<ILocator> WaitForGridReadyAsync(int timeoutMs = DefaultGridTimeoutMs, bool requireContent = true)
        => WaitForGridReadyAsync(_page.Locator(TableLocator), timeoutMs, requireContent);

    private async Task<ILocator> WaitForGridReadyAsync(ILocator table, int timeoutMs = DefaultGridTimeoutMs, bool requireContent = true)
    {
        try
        {
            await table.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException(
                $"The results grid ('{TableLocator}') was not visible after {timeoutMs}ms. Current URL: '{_page.Url}'. " +
                "Either the page was still navigating, or this page renders no grid at all.", ex);
        }

        if (requireContent)
            await WaitForTableContentAsync(table, timeoutMs);

        return table;
    }

    private async Task WaitForTableContentAsync(ILocator table, int timeoutMs = DefaultGridTimeoutMs)
    {
        var rows = table.Locator(TableRowLocator);
        var noDataRow = table.Locator($"{TableRowLocator}:has-text('No Data Available')");

        var rowsTask = rows.First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        var noDataTask = noDataRow.First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });

        // Exactly one of these always loses and faults on timeout. This runs on every row selection
        // now, so observe the loser rather than leaving a stream of unobserved task exceptions.
        Observe(rowsTask);
        Observe(noDataTask);

        await Task.WhenAny(rowsTask, noDataTask);

        if (!await rows.First.IsVisibleAsync() && !await noDataRow.First.IsVisibleAsync())
            throw new TimeoutException("Timed out waiting for table rows to render.");
    }

    private static void Observe(Task task) =>
        _ = task.ContinueWith(static t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);

    public async Task<bool> IsTableDisplayed(int timeoutMs = DefaultGridTimeoutMs)
    {
        var table = _page.Locator(TableLocator);
        try
        {
            await table.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeoutMs });
            return await table.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<ILocator?> FindRowByColumnValueAsync(string columnName, string columnValue)
    {
        var rows = _page.Locator(TableRowLocator);
        int rowCount = await rows.CountAsync();
        for (int i = 0; i < rowCount; i++)
        {
            var row = rows.Nth(i);
            var idCell = row.Locator($".grid-prop-{columnName} span").First;
            if (await idCell.CountAsync() > 0)
            {
                string idCellText = await idCell.InnerTextAsync();
                if (idCellText.Trim().Equals(columnValue, StringComparison.OrdinalIgnoreCase))
                    return row;
            }
        }
        return null;
    }
}
