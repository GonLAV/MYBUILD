using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.TestDataProvider.Providers.AdbxApiDataProvider;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class CaseViewTests : AdbxUITestBase
    {
        public FrontEndType? FrontEnd { get; set; }
        protected IAdbxApiClientFactory _adbxApifactory = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _adbxApifactory = refitApiLocator.GetRequiredService<IAdbxApiClientFactory>();
        }

        private async Task DeleteViewIfExistsAsync(string viewName)
        {
            var api = await _adbxApifactory.CreateApiClientAsync();
            var getQueuesResponse = await api.GetCaseQueues();
            var existingQueueIds = getQueuesResponse.Content.PinnedQueues
                .Concat(getQueuesResponse.Content.UnpinnedQueues)
                .Where(q => q.Name == viewName)
                .Select(q => q.Id)
                .ToList();

            foreach (var queueId in existingQueueIds)
            {
                var deleteResponse = await api.DeleteCaseView(queueId.ToString());
                await deleteResponse.EnsureSuccessStatusCodeAsync();
            }
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(243552)]
        [Description("Create New Service Request view, check grid and verify view is displayed")]
         public async Task BOLTAG_Create_Service_Request_View()
        {
            var viewName = "Auto_CreateView";
            var adminUser = TestContextAccessor.CurrentUserCollection.Admin;
            ScopeContext.Set(ctx => ctx.CurrentUser, adminUser);

            await _logger.ExecuteStepAsync("Cleanup: Delete view if exists", async () =>
            {
                await DeleteViewIfExistsAsync(viewName);
            });

            await AdbxHelper.LoginAsync(
                adminUser,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await AdbxHelper.NavigateToMenuAsync<ADBX_CasesTabPage>(homePage, NavigationType.Service);

            await _logger.ExecuteStepAsync("Create new service request view with conditions", async () =>
            {
                await _pageHelper.InteractWithField(ADBX_FieldNames.AddViewButton);
                var addViewPopup = PageFactory.CreatePage<ADBX_AddEditViewPopup>();
                await addViewPopup.SetViewName(viewName);
                await addViewPopup.AddCondition("Status", "==", "Open");
                await addViewPopup.AddCondition("Assigned To", "==", "Service Agent");
                await addViewPopup.AddCondition("Severity", "==", "Low");
                await addViewPopup.ClickPopupCreate();
            });

            var (isGridDisplayed, isQueueDisplayed, severityColumnValues, statusColumnValues, assignedToColumnValues, mismatchedSeverityValues, mismatchedStatusValues, mismatchedAssignedToValues) = await _logger.ExecuteStepAsync("Verify grid and column data", async () =>
            {
                var gridDisplayed = await _pageHelper.IsTableDisplayed();

                var severityValues = await _pageHelper.GetColumnData("Severity");
                var mismatchedSeverity = severityValues
                    .Where(value => !value.Equals("Low", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var statusValues = await _pageHelper.GetColumnData("Status");
                var mismatchedStatus = statusValues
                    .Where(value => !value.Equals("Open", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var assignedToValues = await _pageHelper.GetColumnData("Assigned To");
                var mismatchedAssignedTo = assignedToValues
                    .Where(value => !value.Equals("Service Agent", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var casesPage = PageFactory.CreatePage<ADBX_CasesTabPage>();
                var queueDisplayed = await casesPage.IsQueueDisplayed(viewName);
                return (gridDisplayed, queueDisplayed, severityValues, statusValues, assignedToValues, mismatchedSeverity, mismatchedStatus, mismatchedAssignedTo);
            });

            Assert.Multiple(() =>
            {
                Assert.That(isGridDisplayed, Is.True, "Grid/table should be displayed");
                Assert.That(isQueueDisplayed, Is.True, "Queue 'autotest1' should be displayed");
                Assert.That(severityColumnValues.Count, Is.GreaterThan(0), "Grid should contain at least one row");
                Assert.That(mismatchedSeverityValues, Is.Empty,
                    $"All rows should have Severity = 'Low'. Found mismatched values: {string.Join(", ", mismatchedSeverityValues)}");
                Assert.That(statusColumnValues.Count, Is.GreaterThan(0), "Grid should contain at least one row");
                Assert.That(mismatchedStatusValues, Is.Empty,
                    $"All rows should have Status = 'Open'. Found mismatched values: {string.Join(", ", mismatchedStatusValues)}");
                Assert.That(assignedToColumnValues.Count, Is.GreaterThan(0), "Grid should contain at least one row");
                Assert.That(mismatchedAssignedToValues, Is.Empty,
                    $"All rows should have Assigned To = 'Service Agent'. Found mismatched values: {string.Join(", ", mismatchedAssignedToValues)}");
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [TestCaseId(243934)]
        [Author(Author.Andrii)]
        [Description("Create Service Request View via API and verify it can be deleted from UI")]
        public async Task BOLTAG_Delete_Service_Request_View()
        {
            var viewName = "Auto_DeleteView";
            var adminUser = TestContextAccessor.CurrentUserCollection.Admin;
            ScopeContext.Set(ctx => ctx.CurrentUser, adminUser);

            await _logger.ExecuteStepAsync($"Cleanup: Delete view {viewName} if exists", async () =>
            {
                await DeleteViewIfExistsAsync(viewName);
            });

            var createdViewId = await _logger.ExecuteStepAsync($"Create case view {viewName} via API", async () =>
            {
                var api = await _adbxApifactory.CreateApiClientAsync();
                var caseViewPayload = CaseViewDataProvider.GetCreateCaseViewPayload();
                caseViewPayload.Referred = viewName;
                caseViewPayload.Clauses.First(e => e.FieldType == "caseAssignedTo").Value = new
                {
                    name = "Service Agent",
                    value = TestContextAccessor.CurrentUserCollection.ServiceAgent.Id
                };
                var createResponse = await api.PostCaseView(caseViewPayload);
                await createResponse.EnsureSuccessStatusCodeAsync();

                var viewId = createResponse.Content?.Id;
                Assert.That(viewId, Is.Not.Null, "Created case view ID should not be null");
                Assert.That(viewId, Is.Not.EqualTo(Guid.Empty), "Created case view ID should not be empty");
                _logger.Info($"Case view '{viewName}' created via API with ID: {viewId}");
                return viewId;
            });

            await AdbxHelper.LoginAsync(
                adminUser,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await AdbxHelper.NavigateToMenuAsync<ADBX_CasesTabPage>(homePage, NavigationType.Service);

            await _logger.ExecuteStepAsync($"Delete view {viewName} via UI", async () =>
            {
                var casesPage = PageFactory.CreatePage<ADBX_CasesTabPage>();
                var editPopup = await casesPage.ClickEditView(viewName);
                await editPopup.ClickPopupDelete();
                await editPopup.ClickPopupConfirm();
            });

            await _logger.ExecuteStepAsync($"Verify view {viewName} is deleted", async () =>
            {
                var casesPage = PageFactory.CreatePage<ADBX_CasesTabPage>();
                var isQueueDisplayed = await casesPage.IsQueueDisplayed(viewName);
                Assert.That(isQueueDisplayed, Is.False, $"Queue '{viewName}' should not be displayed");
            });

        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(243953)]
        [Description("Create Service Request View via API, edit it via UI to add condition and verify grid shows filtered results")]
        public async Task BOLTAG_Edit_Service_Request_View()
        {
            var viewName = "Auto_EditView";
            var adminUser = TestContextAccessor.CurrentUserCollection.Admin;
            ScopeContext.Set(ctx => ctx.CurrentUser, adminUser);

            await _logger.ExecuteStepAsync($"Cleanup: Delete view {viewName} if exists", async () =>
            {
                await DeleteViewIfExistsAsync(viewName);
            });

            var createdViewId = await _logger.ExecuteStepAsync("Create case view via API", async () =>
            {
                var api = await _adbxApifactory.CreateApiClientAsync();
                var caseViewPayload = CaseViewDataProvider.GetCreateCaseViewPayload();
                caseViewPayload.Referred = viewName;
                caseViewPayload.Clauses.First(e => e.FieldType == "caseAssignedTo").Value = new
                {
                    name = "Service Agent",
                    value = TestContextAccessor.CurrentUserCollection.ServiceAgent.Id
                };
                var createResponse = await api.PostCaseView(caseViewPayload);
                await createResponse.EnsureSuccessStatusCodeAsync();
                var viewId = createResponse.Content?.Id;
                Assert.That(viewId, Is.Not.Null, "Created case view ID should not be null");
                Assert.That(viewId, Is.Not.EqualTo(Guid.Empty), "Created case view ID should not be empty");
                _logger.Info($"Case view '{viewName}' created via API with ID: {viewId}");
                return viewId;
            });

            await AdbxHelper.LoginAsync(
                adminUser,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await AdbxHelper.NavigateToMenuAsync<ADBX_CasesTabPage>(homePage, NavigationType.Service);

            await _logger.ExecuteStepAsync($"Edit {viewName} view and add Stage condition", async () =>
            {
                var casesPage = PageFactory.CreatePage<ADBX_CasesTabPage>();
                var editPopup = await casesPage.ClickEditView(viewName);
                await editPopup.AddCondition("Stage", "==", "New Request");
                await editPopup.ClickPopupSave();
            });

            var (stageColumnValues, mismatchedValues) = await _logger.ExecuteStepAsync("Verify Stage column data", async () =>
            {
                var stageValues = await _pageHelper.GetColumnData("Stage");
                var mismatched = stageValues
                    .Where(value => !value.Equals("New Request", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return (stageValues, mismatched);
            });

            _logger.LogDataValidation("StageColumnMatchesFilter",
                mismatchedValues.Count == 0,
                "New Request",
                string.Join(", ", mismatchedValues),
                "All rows should have Stage = 'New Request'");

            Assert.Multiple(() =>
            {
                Assert.That(stageColumnValues.Count, Is.GreaterThan(0), "Grid should contain at least one row");
                Assert.That(mismatchedValues, Is.Empty,
                    $"All rows should have Stage = 'New Request'. Found mismatched values: {string.Join(", ", mismatchedValues)}");
            });
        }

        [Test]
        [Tenant(Tenant.BOLTAG)]
        [Category("ADBX")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(243545)]
        [Description("Create Service Request view with Smart selection, verify Status = Open and Assigned To Smart == #ServiceGroupUsers")]
        public async Task BOLTAG_Create_Service_Request_View_With_Smart_Selection()
        {
            var viewName = "Auto_SmartView";
            var adminUser = TestContextAccessor.CurrentUserCollection.Admin;
            ScopeContext.Set(ctx => ctx.CurrentUser, adminUser);

            await _logger.ExecuteStepAsync("Cleanup: Delete view if exists", async () =>
            {
                await DeleteViewIfExistsAsync(viewName);
            });

            await AdbxHelper.LoginAsync(
                adminUser,
                ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var homePage = PageFactory.CreatePage<ADBX_HomePage>();
            await AdbxHelper.NavigateToMenuAsync<ADBX_CasesTabPage>(homePage, NavigationType.Service);

            await _logger.ExecuteStepAsync("Create view with Smart selection", async () =>
            {
                await _pageHelper.InteractWithField(ADBX_FieldNames.AddViewButton);
                var addViewPopup = PageFactory.CreatePage<ADBX_AddEditViewPopup>();
                await addViewPopup.SetViewName(viewName);
                await addViewPopup.AddCondition("Status", "==", "Open");
                await addViewPopup.AddCondition("Assigned To", "==", "#ServiceGroupUsers");
                await addViewPopup.ClickPopupCreate();
            });

            var (isGridDisplayed, isQueueDisplayed, assignedToColumnValues) = await _logger.ExecuteStepAsync("Verify grid and Smart selection results", async () =>
            {
                var gridDisplayed = await _pageHelper.IsTableDisplayed();
                var casesPage = PageFactory.CreatePage<ADBX_CasesTabPage>();
                var queueDisplayed = await casesPage.IsQueueDisplayed(viewName);
                var assignedToValues = await _pageHelper.GetColumnData("Assigned To");
                return (gridDisplayed, queueDisplayed, assignedToValues);
            });

            _logger.LogBusinessRule("SmartSelectionIncludesServiceAgent",
                assignedToColumnValues.Any(e => e.Equals("Service Agent", StringComparison.OrdinalIgnoreCase)),
                "At least one row should have Assigned To = 'Service Agent'");

            Assert.Multiple(() =>
            {
                Assert.That(isGridDisplayed, Is.True, "Grid/table should be displayed");
                Assert.That(isQueueDisplayed, Is.True, $"View '{viewName}' should be displayed");
                Assert.That(assignedToColumnValues.Count, Is.GreaterThan(0), "Grid should contain at least one row");
                Assert.That(assignedToColumnValues.Any(e => e.Equals("Service Agent", StringComparison.OrdinalIgnoreCase)), Is.True,
                    "At least 1 should have Assigned To = 'Service Agent'");

            });
        }
    }
}
