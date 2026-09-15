using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.CommonHelpers;
using Bolt.Automation.FrontEnds.PlaywrightBase;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Globalization;
using static Bolt.Automation.Tests.TestHelpers.ADBX.AdbxTestHelper;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class AdbxPageGridTests : AdbxUITestBase
    {

        /// <summary>
        /// Leads the Open queue must hold for the pagination test to mean anything - it walks to
        /// page 2 and switches to a page size of 20.
        /// </summary>
        private const int MinimumLeadsRequired = 20;

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Author(Author.Sandy)]
        [TestCaseId(243689)]
        [Description("Check leads page grid sorting functionality")]
        public async Task BOLTAG_Leads_Grid_Sorting()
        {
            await AdbxHelper.LoginAsync(
             TestContextAccessor.CurrentUserCollection.Admin,
             ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadsPage = await _logger.ExecuteStepAsync("Navigate to Leads tab and select queue", async () =>
            {
                var leadsPage = await AdbxHelper.SelectQueueAsync<ADBX_LeadsTabPage>(NavigationType.Leads, "Open");
                return leadsPage;
            });

            await _logger.ExecuteStepAsync("Asserting sort functionality for column headers", async () =>
            {
                var leadsQueueRoute = "/leads/queue";
                await AdbxHelper.EnsureGridSortRequestsAsync(leadsPage, "Created Date", "createdDate", leadsQueueRoute);
                await AssertGridSortChangesAsync(leadsPage, "Named Insured", "name", leadsQueueRoute);
                await AssertGridSortChangesAsync(leadsPage, "State", "state", leadsQueueRoute);
                await AssertGridSortChangesAsync(leadsPage, "Stage", "stage", leadsQueueRoute);
                await AssertGridSortChangesAsync(leadsPage, "Product", "product", leadsQueueRoute);
                await AdbxHelper.EnsureGridSortRequestsAsync(leadsPage, "Source", "sourceDisplayName", leadsQueueRoute);
                await AdbxHelper.EnsureGridSortRequestsAsync(leadsPage, "Due Date", "dueDate", leadsQueueRoute);
                await AssertGridSortChangesAsync(leadsPage, "Min Premium", "minPremium", leadsQueueRoute);
                await AssertGridSortChangesAsync(leadsPage, "Score", "score", leadsQueueRoute);
            });

        }

        /// <summary>
        /// Sorts a column ascending then descending and asserts the top row actually changed.
        /// </summary>
        /// <remarks>
        /// Only meaningful when the column holds more than one distinct value: on UAT every lead
        /// has Stage "In Progress", so both directions put the same value first. Falls back to the
        /// request-level contract EnsureGridSortAsync has already verified.
        /// </remarks>
        private async Task AssertGridSortChangesAsync(
            ADBX_LeadsTabPage leadsPage,
            string columnDisplayName,
            string sortKey,
            string route)
        {
            await AdbxHelper.EnsureGridSortAsync(
                leadsPage,
                columnDisplayName,
                sortKey,
                route,
                GridSortDirection.Asc);

            var ascValues = await _pageHelper!.GetColumnData(columnDisplayName);

            await AdbxHelper.EnsureGridSortAsync(
                leadsPage,
                columnDisplayName,
                sortKey,
                route,
                GridSortDirection.Desc);

            var descValues = await _pageHelper!.GetColumnData(columnDisplayName);

            var distinctValues = ascValues.Distinct(StringComparer.Ordinal).Count();
            if (distinctValues <= 1)
            {
                _logger.Info(
                    $"'{columnDisplayName}' is '{ascValues.FirstOrDefault()}' in all {ascValues.Count} rows; " +
                    "reordering is unobservable, so the verified sort request stands as the check.");
                return;
            }

            _logger.Info($"First row value (asc) for '{columnDisplayName}': {ascValues[0]}");
            _logger.Info($"First row value (desc) for '{columnDisplayName}': {descValues[0]}");

            Assert.That(descValues[0], Is.Not.EqualTo(ascValues[0]),
                $"Sorting '{columnDisplayName}' descending put the same value first as ascending, " +
                $"even though the column holds {distinctValues} distinct values across {ascValues.Count} rows.");
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Author(Author.Sandy)]
        [TestCaseId(243930)]
        [Description("Check leads page grid default view, page size change, refresh and pagination functionality")]
        public async Task BOLTAG_Leads_Grid_Page_Size_Check_Refresh_And_Pagination()
        {
            await AdbxHelper.LoginAsync(
             TestContextAccessor.CurrentUserCollection.Admin,
             ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadsPage = await _logger.ExecuteStepAsync("Navigate to Leads tab and select queue", async () =>
            {
                var leadsPage = await AdbxHelper.SelectQueueAsync<ADBX_LeadsTabPage>(NavigationType.Leads, "Open");
                return leadsPage;
            });

            Assert.That(await _pageHelper!.IsTableDisplayed(), Is.True);

            await _logger.ExecuteStepAsync("Verify leads grid column headers are visible", async () =>
            {
                var expectedHeaders = new List<string>
                {
                    "Named Insured",
                    "State",
                    "Stage",
                    "Product",
                    "Source",
                    "Created Date",
                    "Assigned To",
                    "Due Date",
                    "Min Premium",
                    "Score"
                };

                var actualHeaders = await _pageHelper.GetTableHeadersAsync();
                Assert.That(actualHeaders, Is.EqualTo(expectedHeaders));
            });

            await _logger.ExecuteStepAsync("Verify default page size is 10, footer message and table row count is correct", async () =>
            {
                var defaultPageSize = (await _pageHelper.GetFieldValue(ADBX_FieldNames.PageGridSizeDropdown)).Trim();
                Assert.That(defaultPageSize, Is.EqualTo("10"));

                // Read the footer BEFORE asserting the row count. The footer is the grid's own
                // statement of what it is showing, so it separates the two ways this step can fail:
                // too few leads in the queue (a data precondition) from rows that have not finished
                // rendering (a race). Asserting the count first reported both as "expected 10, was 8".
                var defaultMessage = await leadsPage.ReadGridFooterAsync();
                var totalLeads = ADBX_LeadsTabPage.ParseFooterTotal(defaultMessage);

                Assert.That(totalLeads, Is.GreaterThanOrEqualTo(MinimumLeadsRequired),
                    $"The Open leads queue holds only {totalLeads} leads (footer: '{defaultMessage}'). " +
                    $"This test walks to page 2 and switches to a page size of 20, so it needs at least " +
                    $"{MinimumLeadsRequired}. That is a test-data precondition, not a grid defect.");

                Assert.That(defaultMessage, Does.Contain("Showing 1 - 10 of"));

                var rowCount = await _pageHelper.GetRowCount();
                Assert.That(rowCount, Is.EqualTo(10),
                    $"The grid footer says '{defaultMessage}' but {rowCount} rows rendered.");
            });

            await _logger.ExecuteStepAsync("Go to page 2 and verify footer message", async () =>
            {
                await _pageHelper.InteractWithField(ADBX_FieldNames.PageGridPagination, "2");

                var pageTwoMessage = await leadsPage.ReadGridFooterAsync();
                Assert.That(pageTwoMessage, Does.Contain("Showing 11 - 20 of"));

                var rowCount = await _pageHelper.GetRowCount();
                Assert.That(rowCount, Is.EqualTo(10),
                    $"The grid footer says '{pageTwoMessage}' but {rowCount} rows rendered.");

                await _pageHelper!.InteractWithField(ADBX_FieldNames.PageGridRefreshButton);

                pageTwoMessage = (await _pageHelper.GetFieldValue(ADBX_FieldNames.PageGridSizeMessage)).Replace('\u00A0', ' ').Trim();
                Assert.That(pageTwoMessage, Does.Contain("Showing 11 - 20 of"));
            });

            await _logger.ExecuteStepAsync("Refresh and verify footer message", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.PageGridRefreshButton);

                var pageTwoMessage = (await _pageHelper.GetFieldValue(ADBX_FieldNames.PageGridSizeMessage)).Replace('\u00A0', ' ').Trim();
                Assert.That(pageTwoMessage, Does.Contain("Showing 11 - 20 of"));
            });

            await _logger.ExecuteStepAsync("Go to page 1, change page size to 20 and verify footer message and table row count", async () =>
            {
                //need to ensure that going back to page 1 is completing before we select page size.
                await _pageHelper.WaitForApiResponseAsync(
                             async () => await _pageHelper.InteractWithField(ADBX_FieldNames.PageGridPagination, "1"),
                             "/leads/queue/",
                             expectedStatus: 200,
                             predicate: response =>
                             response.Url.Contains("skip=0", StringComparison.OrdinalIgnoreCase)
                             && response.Url.Contains("take=10", StringComparison.OrdinalIgnoreCase));

                await _pageHelper.InteractWithField(ADBX_FieldNames.PageGridSizeDropdown, "20");

                var updatedPageSize = (await _pageHelper.GetFieldValue(ADBX_FieldNames.PageGridSizeDropdown)).Trim();
                Assert.That(updatedPageSize, Is.EqualTo("20"));
                var updatedMessage = await leadsPage.ReadGridFooterAsync();
                Assert.That(updatedMessage, Does.Contain("Showing 1 - 20 of"));

                var rowCount = await _pageHelper.GetRowCount();
                Assert.That(rowCount, Is.EqualTo(20),
                    $"The grid footer says '{updatedMessage}' but {rowCount} rows rendered.");
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Author(Author.Sandy)]
        [TestCaseId(244150)]
        [Description("Check leads page grid checkbox, refresh, and export/download file functionality")]
        public async Task BOLTAG_Leads_Grid_CheckboxSelect_Export()
        {
            await AdbxHelper.LoginAsync(
             TestContextAccessor.CurrentUserCollection.Admin,
             ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadsPage = await _logger.ExecuteStepAsync("Navigate to Leads tab and select queue", async () =>
            {
                var leadsPage = await AdbxHelper.SelectQueueAsync<ADBX_LeadsTabPage>(NavigationType.Leads, "On Hold");
                return leadsPage;
            });

            await _logger.ExecuteStepAsync("Select first lead checkbox and verify selection", async () =>
            {
                await _pageHelper!.ClickTableRowCheckboxByIndexAsync(1);
                var isChecked = await _pageHelper.IsTableRowCheckboxCheckedByIndexAsync(1);
                Assert.That(isChecked, Is.True);
            });

            await _logger.ExecuteStepAsync("Select checkbox in the header and verify all leads are selected", async () =>
            {
                await _pageHelper!.ClickTableHeaderCheckboxAsync();
                var areAllChecked = await _pageHelper.IsAllTableRowCheckboxesCheckedAsync();
                Assert.That(areAllChecked, Is.True);
            });

            await _logger.ExecuteStepAsync("Unselect checkbox in the header and verify all leads are unselected", async () =>
            {
                await _pageHelper!.ClickTableHeaderCheckboxAsync();
                var areAllChecked = await _pageHelper.IsAllTableRowCheckboxesCheckedAsync();
                Assert.That(areAllChecked, Is.False);
            });

            await _logger.ExecuteStepAsync("Export/download leads and validate export API response", async () =>
            {
                var response = await _pageHelper!.WaitForApiResponseAsync(
                    async () => await _pageHelper.InteractWithField(ADBX_FieldNames.PageGridDownloadButton),
                    "/leads/views/",
                    expectedStatus: 200,
                    predicate: r => r.Url.Contains("/export", StringComparison.OrdinalIgnoreCase));

                Assert.That(response.Status, Is.EqualTo(200));
                Assert.That(response.Url, Does.Contain("/leads/views/"));
                Assert.That(response.Url, Does.Contain("/export"));
            });

            await _logger.ExecuteStepAsync("Click first lead", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
               _pageFactory!.CreatePage<ADBX_LeadSummaryPage>();
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Author(Author.Sandy)]
        [TestCaseId(242199)]
        [Description("Check leads page grid Source and Product filter functionality")]
        public async Task BOLTAG_Leads_Grid_Source_And_Product_Filters()
        {
            await AdbxHelper.LoginAsync(
                TestContextAccessor.CurrentUserCollection.Admin,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadsPage = await _logger.ExecuteStepAsync("Navigate to Leads tab and select Open queue", async () =>
            {
                return await AdbxHelper.SelectQueueAsync<ADBX_LeadsTabPage>(NavigationType.Leads, "Open");
            });

            await _logger.ExecuteStepAsync("Filter grid by Source 'Bolt PL' and verify results", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.LeadsGridFilterBySource, "Bolt PL");
                await leadsPage.WaitForGridLoadAsync();

                var rowCount = await _pageHelper.GetRowCount();
                Assert.That(rowCount, Is.GreaterThan(0), "Grid should display leads after filtering by 'Bolt PL' source");

                var firstRowData = await _pageHelper.GetRowData(1);
                Assert.That(firstRowData["Source"], Is.EqualTo("Bolt PL"), "Filtered grid should display only 'Bolt PL' leads");
            });

            await _logger.ExecuteStepAsync("Clear the Source filter", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.LeadsGridFilterSourceClear);
                await leadsPage.WaitForGridLoadAsync();
            });

            await _logger.ExecuteStepAsync("Filter grid by Product 'Homeowners' and verify results", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.LeadsGridFilterByProduct, "Homeowners");
                await leadsPage.WaitForGridLoadAsync();

                var rowCount = await _pageHelper.GetRowCount();
                Assert.That(rowCount, Is.GreaterThan(0), "Grid should display leads after filtering by 'Homeowners' product");

                var firstRowData = await _pageHelper.GetRowData(1);
                Assert.That(firstRowData["Product"], Does.Contain("Homeowners").Or.Contain("Various"), "Filtered grid should display 'Homeowners' or 'Various' leads");
            });

            var gridRowData = await _logger.ExecuteStepAsync("Apply both Source 'Bolt PL' and Product 'Homeowners' filters and verify combined results", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.LeadsGridFilterBySource, "Bolt PL");
                await leadsPage.WaitForGridLoadAsync();

                var rowCount = await _pageHelper.GetRowCount();
                Assert.That(rowCount, Is.GreaterThan(0), "Grid should display leads matching both Source and Product filters");

                var firstRowData = await _pageHelper.GetRowData(1);
                Assert.That(firstRowData["Source"], Is.EqualTo("Bolt PL"), "Grid should display only 'Bolt PL' leads when both filters are active");
                Assert.That(firstRowData["Product"], Does.Contain("Homeowners").Or.Contain("Various"), "Grid should display 'Homeowners' or 'Various' leads when both filters are active");

                return firstRowData;
            });

            await _logger.ExecuteStepAsync("Open first lead and verify Source field on lead detail matches grid value", async () =>
            {
                await _pageHelper!.SelectTableRowAsync(1);
                var leadSummaryPage = _pageFactory!.CreatePage<ADBX_LeadSummaryPage>();
                var summaryData = await leadSummaryPage.GetSummaryData();
                Assert.That(summaryData, Does.ContainKey("Source"), "Source field should be present on lead detail");
                Assert.That(summaryData["Source"], Is.EqualTo(gridRowData["Source"]), "Source on lead detail should match the grid value");
            });

        }
    }

}