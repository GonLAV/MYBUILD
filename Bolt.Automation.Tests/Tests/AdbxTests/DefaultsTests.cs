using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.FrontEnds.CommonHelpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    internal class DefaultsTests : AdbxUITestBase
    {
        private DefaultsTestHelper? _defaultsHelperField;
        private DefaultsTestHelper DefaultsHelper => _defaultsHelperField ??= new DefaultsTestHelper(_logger, PageFactory, _pageHelper!);

        [Test]
        [Tenant(Tenant.UNIFY)]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Defaults")]
        [TestCaseId(228117)]
        [Author(Author.Andrii)]
        [Description("Download default records from the system for Unify and verify file has correct fields")]
        public async Task Unify_Download_Defaults()
        {
            var expectedFileName = $"unify_default_values_export_{DateTime.Today.ToString("yyyy-MM-dd")}.csv";
            var expectedColumns = new List<string>()
                {
                    "Source", "Name", "Flow", "LOB", "State", "Field", "Value", "LastUpdated"
                };

            var homePage = await AdbxHelper.LoginToHomeAsync(
                TestContextAccessor.CurrentUserCollection.RootAdmin,
                TestContextAccessor.CurrentUserCollection.RootAdmin.LoginUrl!);

            await AdbxHelper.NavigateToMenuAsync<ADBX_DefaultsManagementPage>(homePage, NavigationType.DefaultsManagement);

            await _logger.ExecuteStepAsync("Download the Defaults records", async () =>
            {
                await _pageHelper!.InteractWithField(DefaultsGridSearchButton);
                var fileDownloadValidator = new FileDownloadValidatorHelper(await BrowserManager.GetPageAsync(), _logger);
                var downloadResult = await fileDownloadValidator.ValidateFileDownload(async () => await _pageHelper!.InteractWithField(DefaultsGridDownloadButton));

                Assert.That(downloadResult.FileName, Is.EqualTo(expectedFileName),
                    $"Downloaded file name should match the expected export file name. Actual: {downloadResult.FileName} Expected: {expectedFileName} ");

                Assert.That(downloadResult.RowCount, Is.GreaterThan(0), "Should have at least 1 row");

                _logger.LogDataValidation("Downloaded File Columns",
                    downloadResult.Columns.SequenceEqual(expectedColumns),
                    string.Join(", ", expectedColumns),
                    string.Join(", ", downloadResult.Columns),
                    "Downloaded file columns should match the expected columns");

                Assert.That(downloadResult.Columns, Is.EqualTo(expectedColumns));
            }, $"Expected result: file name {expectedFileName}, columns {expectedColumns}, file should have at least 1 row");
        }

        // TestCaseSource for multiple tenants including SSO tenants
        public static IEnumerable<TestCaseData> DefaultsUpdateRecordTestData
        {
            get
            {
                yield return new TestCaseData(Tenant.UNIFY)
                    .SetProperty("TestCaseId", "228118");
                yield return new TestCaseData(Tenant.BOLTAG)
                    .SetProperty("TestCaseId", "241459");
                yield return new TestCaseData(Tenant.LIBERTYX)
                    .SetProperty("TestCaseId", "228463");
                yield return new TestCaseData(Tenant.COMPARION)
                    .SetProperty("TestCaseId", "228658");
            }
        }

        [Test]
        [TestCaseSource(nameof(DefaultsUpdateRecordTestData))]
        [Category("ADBX")]
        [Category("CRM")]
        [Category("Defaults")]
        [Author(Author.Andrii)]
        [Description("Test defaults record update by toggling updateable fields only")]

        public async Task Defaults_Update_Record(Tenant tenant)
        {
            ScopeContext.Set(ctx => ctx.Tenant, tenant);
            ScopeContext.Set(ctx => ctx.UrlDataCollection, TestContextAccessor.CurrentUrlCollection);

            var dataSetA = new Dictionary<string, string>
            {
                [DefaultsPopupSourceDrodpodwn] = "test",
                [DefaultsPopupFlowDrodpodwn] = "Agent",
                [DefaultsPopupLobDrodpodwn] = "PersonalAuto",
                [DefaultsPopupStateDrodpodwn] = "UT",
                [DefaultsPopupFieldDrodpodwn] = "BI",
                [DefaultsPopupValueDrodpodwn] = "cov10to20"
            };

            var dataSetB = new Dictionary<string, string>
            {
                [DefaultsPopupSourceDrodpodwn] = "test",
                [DefaultsPopupFlowDrodpodwn] = "Consumer",
                [DefaultsPopupLobDrodpodwn] = "PersonalHome",
                [DefaultsPopupStateDrodpodwn] = "RI",
                [DefaultsPopupFieldDrodpodwn] = "BI",
                [DefaultsPopupValueDrodpodwn] = "cov15to30"
            };

            var editableFields = new HashSet<string>
            {
                DefaultsPopupFlowDrodpodwn,
                DefaultsPopupLobDrodpodwn,
                DefaultsPopupStateDrodpodwn,
                DefaultsPopupValueDrodpodwn
            };

            var searchCriteria = new Dictionary<string, string>
            {
                [DefaultsGridSourceDrodpodwn] = "test",
                [DefaultsGridFieldDrodpodwn] = "BI"
            };

            var homePage = await LoginForTenantAsync(tenant);
            await AdbxHelper.NavigateToMenuAsync<ADBX_DefaultsManagementPage>(homePage, NavigationType.DefaultsManagement);

            var currentRowData = await _logger.ExecuteStepAsync("Search defaults grid for existing record", async () =>
                await DefaultsHelper.SearchDefaultsAsync(searchCriteria));

            var (targetDataSet, shouldEdit) = DetermineToggleTarget(currentRowData, dataSetA, dataSetB);

            await _logger.ExecuteStepAsync("Create or update defaults record", async () =>
            {
                if (shouldEdit)
                    await DefaultsHelper.UpdateDefaultsRecordAsync(
                        currentValue: currentRowData!.GetValueOrDefault("Value", ""),
                        editableFields: targetDataSet
                            .Where(kvp => editableFields.Contains(kvp.Key))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                else
                    await DefaultsHelper.CreateDefaultsRecordAsync(targetDataSet);
            });

            await _logger.ExecuteStepAsync("Verify defaults record values", async () =>
                await DefaultsHelper.VerifyDefaultsRecordAsync(searchCriteria, new Dictionary<string, string>
                {
                    ["Flow"] = targetDataSet[DefaultsPopupFlowDrodpodwn],
                    ["Value"] = targetDataSet[DefaultsPopupValueDrodpodwn],
                    ["LOB"] = targetDataSet[DefaultsPopupLobDrodpodwn],
                    ["State"] = targetDataSet[DefaultsPopupStateDrodpodwn]
                }));
        }

        /// <summary>
        /// Logs in the way this tenant reaches ADBX and returns the page to navigate from: the Home
        /// page for the login-URL tenants, null for the SSO ones, which land on the target page
        /// already — <see cref="AdbxTestHelper.NavigateToMenuAsync{T}"/> reads that null as
        /// "skip navigation".
        /// </summary>
        private async Task<ADBX_HomePage?> LoginForTenantAsync(Tenant tenant)
        {
            var (user, loginUrl, relayState) = GetTenantLoginData(tenant);

            if (relayState == null)
                return await AdbxHelper.LoginToHomeAsync(user, loginUrl!);

            await SsoHelper.LoginAsync(user, relayState, BrowserManager);
            return null;
        }

        private (UserTestData user, string? loginUrl, RelayStateTestData? relayState) GetTenantLoginData(Tenant tenant) =>
            tenant switch
            {
                Tenant.UNIFY => (
                    TestContextAccessor.CurrentUserCollection.RootAdmin,
                    TestContextAccessor.CurrentUserCollection.RootAdmin.LoginUrl,
                    null),
                Tenant.BOLTAG => (
                    TestContextAccessor.CurrentUserCollection.Admin,
                    TestContextAccessor.CurrentUrlCollection.AdbxApi.LoginUrl,
                    null),
                Tenant.LIBERTYX => (
                    TestContextAccessor.CurrentUserCollection.Admin,
                    null,
                    TestContextAccessor.CurrentRelayStateCollection.DefaultsRelayState),
                Tenant.COMPARION => (
                    TestContextAccessor.CurrentUserCollection.Admin,
                    null,
                    TestContextAccessor.CurrentRelayStateCollection.DefaultsRelayState),
                _ => throw new NotSupportedException($"Tenant {tenant} is not supported for this test")
            };

        private (Dictionary<string, string> targetDataSet, bool shouldEdit) DetermineToggleTarget(
            Dictionary<string, string>? rowData,
            Dictionary<string, string> dataSetA,
            Dictionary<string, string> dataSetB)
        {
            if (rowData == null)
            {
                _logger.Info("No data found, will create with dataSetA");
                return (dataSetA, false);
            }

            var currentFlow = rowData.GetValueOrDefault("Flow", "");
            var currentValue = rowData.GetValueOrDefault("Value", "");
            _logger.Info($"Current: Flow='{currentFlow}', Value='{currentValue}'");

            if (currentFlow.Contains(dataSetA[DefaultsPopupFlowDrodpodwn]) && currentValue == dataSetA[DefaultsPopupValueDrodpodwn])
            {
                _logger.Info("Record matches dataSetA, will update to dataSetB");
                return (dataSetB, true);
            }

            if (currentFlow.Contains(dataSetB[DefaultsPopupFlowDrodpodwn]) && currentValue == dataSetB[DefaultsPopupValueDrodpodwn])
            {
                _logger.Info("Record matches dataSetB, will update to dataSetA");
                return (dataSetA, true);
            }

            _logger.Warning("Record doesn't match expected patterns, will create new with dataSetA");
            return (dataSetA, false);
        }

    }
}
