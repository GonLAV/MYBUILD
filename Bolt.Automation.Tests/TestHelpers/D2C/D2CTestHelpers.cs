using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Executor.Helpers.PageCallbackManager;

namespace Bolt.Automation.Tests.TestHelpers.D2C
{
    public class D2CTestHelpers(
     IAutomationLogger _logger,
    IBrowserManager browserManager,
    IPageFactory pageFactory,
    IPageHelper pageHelper,
    IScopeContext scopeContext,
    IGetQuoteApi getQuoteApi,
    IMainQueries? mainQueries = null)
    {
        protected IMainQueries _mainQueries = mainQueries;
        protected IGetQuoteApi _getQuoteApi = getQuoteApi;

        // Creates an application via API using ApplicationRequestModel and navigates to the questionnaire URL
        public async Task CreateApplicationAndNavigate(
        IGetQuoteApi getQuoteApi,
        ApplicationRequestModel<PersonalLineData> requestData, string? appendToUrl = null)
        {
            var apiHelper = new GetQuoteApiHelper(getQuoteApi, scopeContext);
            var getQuestionnaireResp = await apiHelper.CreateApplication_GetQuestionnaire(requestData);
            var url = getQuestionnaireResp.Url;
            if (!string.IsNullOrEmpty(appendToUrl))
            {
                url += appendToUrl;
            }
            await browserManager.NavigateAsync(url);

            _logger.Debug($"Created application and navigated to: {url}");
        }

        public Dictionary<Type, PageCallbackConfig> CreateBundleSelectionCallback()
        {
            return PageCallbackManager.For<D2C_CrossSellInformationPage>(
                async page => await page.SelectBundleBox(),
                CallbackTiming.InsteadOfClickContinue
            );
        }

        /// <summary>Validates LOBs on rates page and handles buy separately display</summary>
        public async Task ValidateLobsOnRatesPage(D2C_RatesPage ratesPage, List<string> expectedLobs)
        {
            // Handle buy separately results if displayed
            if (await ratesPage.IsBuySeparatelyResultsDisplayed())
            {
                await ratesPage.ClickOnBuySeparatelyResults();
                _logger.Debug("Clicked on 'Buy Separately' results");
            }

            var listOfLobs = await ratesPage.ReturnAllLobsTitle();

            _logger.LogDataValidation("LOB Count",
                listOfLobs.Count == expectedLobs.Count,
                expectedLobs.Count.ToString(),
                listOfLobs.Count.ToString(),
                $"Expected {expectedLobs.Count} LOBs ({string.Join(" + ", expectedLobs)})");

            Assert.That(listOfLobs.Count, Is.EqualTo(expectedLobs.Count),
                $"Expected {expectedLobs.Count} LOBs but got {listOfLobs.Count}");

            foreach (var expectedLob in expectedLobs)
            {
                _logger.LogDataValidation($"{expectedLob} LOB Present",
                    listOfLobs.Contains(expectedLob),
                    "True",
                    listOfLobs.Contains(expectedLob).ToString(),
                    $"Should contain {expectedLob} LOB");

                Assert.That(listOfLobs, Does.Contain(expectedLob), $"Should contain {expectedLob} LOB");
            }
        }

        /// <summary>Validates SetLobs in policy data</summary>
        public async Task ValidateSetLobs(string friendlyId, string[] expectedLobs)
        {
            var setLobValues = await _mainQueries.PolicyDataLogic.GetNodeValuesListAsync(friendlyId, "SetLobs");

            _logger.LogDataValidation("SetLobs Validation",
                expectedLobs.All(lob => setLobValues.Contains(lob)),
                $"Contains: {string.Join(", ", expectedLobs)}",
                $"Actual: {string.Join(", ", setLobValues)}",
                $"Should contain all expected LOBs: {string.Join(", ", expectedLobs)}");

            foreach (var expectedLob in expectedLobs)
            {
                Assert.That(setLobValues, Does.Contain(expectedLob), $"SetLobs should contain {expectedLob}");
            }
        }

        /// <summary>Validates ownership type in policy data</summary>
        public async Task ValidateOwnershipType(string friendlyId, string expectedOwnership)
        {
            var ownership = await _mainQueries.PolicyDataLogic.GetNodeValueWithRetryAsync(friendlyId, "OwnershipType");

            _logger.LogDataValidation("Ownership Type",
                ownership?.Equals(expectedOwnership, StringComparison.OrdinalIgnoreCase) ?? false,
                expectedOwnership,
                ownership ?? "null",
                $"OwnershipType should be '{expectedOwnership}'");

            Assert.That(ownership, Is.Not.Null.And.EqualTo(expectedOwnership).IgnoreCase,
                $"Ownership value not found or not as expected '{expectedOwnership}', actual: '{ownership ?? "null"}'");
        }

        /// <summary> retrieval URL </summary>
        public async Task<string> OpenRetrievalUrl(string friendlyId)
        {
            var externalId = (await _mainQueries.Policy.GetPolicyByFriendlyIdAsync(friendlyId))?.ExternalId;
            var getQuestionnaireResp = await _getQuoteApi!.GetQuestionnaireAsync(externalId);
            var retrievalUrl = getQuestionnaireResp.Content.Url;

            _logger.Info($"Retrieved questionnaire URL for external ID: {externalId}");

            await browserManager.NavigateAsync(retrievalUrl);
            return retrievalUrl;
        }

        /// <summary>Validates validation messages contain expected error texts</summary>
        public async Task ValidateErrorMessages(IPageHelper pageHelper, Dictionary<string, string> expectedErrors)
        {
            var validationMessages = await pageHelper.GetValidationMessagesAsync();

            _logger.LogDataValidation("Validation Messages Count",
                validationMessages.Count > 0,
                "> 0",
                validationMessages.Count.ToString(),
                "Should have validation messages");

            Assert.That(validationMessages.Count, Is.GreaterThan(0), "Should have validation messages");

            foreach (var expectedError in expectedErrors)
            {
                var errorKey = expectedError.Key;
                var errorMessage = expectedError.Value;

                _logger.LogDataValidation($"{errorKey} Error",
                    validationMessages.Any(m => m.Contains(errorMessage)),
                    $"Contains: {errorMessage}",
                    string.Join(", ", validationMessages),
                    $"Should display {errorKey} validation error");

                Assert.That(validationMessages.Any(m => m.Contains(errorMessage)), Is.True,
                    $"Expected {errorKey} error message: '{errorMessage}'. Actual messages: [{string.Join(", ", validationMessages)}]");
            }
        }

        /// <summary>Validates validation messages state (present or absent)</summary>
        public async Task ValidateValidationMessages(
            IPageHelper pageHelper,
            bool shouldHaveMessages = true)
        {
            var validationMessages = await pageHelper.GetValidationMessagesAsync();

            if (shouldHaveMessages)
            {
                _logger.LogDataValidation("Validation Messages Present",
                    validationMessages.Count > 0,
                    "> 0",
                    validationMessages.Count.ToString(),
                    "Should have validation messages");

                Assert.That(validationMessages.Count, Is.GreaterThan(0),
                    "Expected validation messages but got none");
            }
            else
            {
                _logger.LogDataValidation("No Validation Messages",
                    validationMessages.Count == 0,
                    "0",
                    validationMessages.Count.ToString(),
                    "Should have no validation messages");

                Assert.That(validationMessages.Count, Is.EqualTo(0),
                    $"Expected no validation messages but found: {string.Join(", ", validationMessages)}");
            }
        }

        /// <summary>Handles buy separately toggle if bundle results are displayed</summary>
        public async Task SwitchToBuySeparatelyIfNeeded(D2C_RatesPage ratesPage)
        {
            if (await ratesPage.IsBundleResultsDisplayed())
            {
                await ratesPage.ClickOnBuySeparatelyResults();
                _logger.Debug("Switched to 'Buy Separately' results");
            }
            else
            {
                _logger.Debug("Buy separately already active or not applicable");
            }
        }

        /// <summary>Validates policy data node value with caching cleared</summary>
        public async Task ValidatePolicyDataNode(string friendlyId, string nodeName, string expectedValue)
        {
            var actualValue = await _mainQueries.PolicyDataLogic.GetNodeValueWithRetryAsync(friendlyId, nodeName);

            _logger.LogDataValidation(nodeName,
                actualValue?.Equals(expectedValue, StringComparison.OrdinalIgnoreCase) ?? false,
                expectedValue,
                actualValue ?? "null",
                $"{nodeName} should be '{expectedValue}'");

            Assert.Multiple(() =>
            {
                Assert.That(actualValue, Is.Not.Null, $"{nodeName} should not be null");
                Assert.That(actualValue, Is.EqualTo(expectedValue).IgnoreCase, $"Should be {expectedValue}");
            });
        }

        /// <summary>Validates address contains all expected components</summary>
        public async Task ValidateAddressComponents(string friendlyId, string addressNodeName, string[] expectedComponents)
        {
            var address = await _mainQueries.PolicyDataLogic.ValidateAddressAsync(friendlyId, addressNodeName);
            var missingComponents = expectedComponents
                .Where(c => !address.Contains(c, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(string.IsNullOrWhiteSpace(address), Is.False,
                    $"{addressNodeName} should not be empty");
                Assert.That(missingComponents, Is.Empty,
                    $"Missing address components: {string.Join(", ", missingComponents)}");
            });
        }

        /// <summary>Validates roof replacement data in policy</summary>
        public async Task ValidateRoofReplacementData(
            string friendlyId,
            string expectedRoofType,
            int yearsAgo)
        {
            var plRoofUpdatedForXml = await _mainQueries.PolicyDataLogic.GetNodeValueWithRetryAsync(friendlyId, "PLRoofUpdatedForXml");
            var roofUpdateYear = await _mainQueries.PolicyDataLogic.GetNodeValueWithRetryAsync(friendlyId, "RoofUpdatedYear");
            var expectedYear = (DateTime.Now.Year - yearsAgo).ToString();

            Assert.Multiple(() =>
            {
                Assert.That(plRoofUpdatedForXml, Is.EqualTo(expectedRoofType),
                    $"Roof update type should be {expectedRoofType}");
                Assert.That(roofUpdateYear, Is.EqualTo(expectedYear),
                    $"Roof year should be {expectedYear}");
            });
        }

        /// <summary>Validates email agreement toggle behavior (unchecked then checked)</summary>
        public async Task ValidateEmailAgreementToggle(string friendlyId)
        {
            var personalDetailsPage = pageFactory.CreatePage<D2C_PersonalDetailsPage>();
            await personalDetailsPage.FillForm(new Dictionary<string, string> { [FieldNames.AgreeToReceiveEmail] = "false" });
            await personalDetailsPage.ClickContinue();
            var policyPage = pageFactory.CreatePage<D2C_PolicyDatePickerPage>();

            _mainQueries.PolicyDataLogic.ClearXmlCache(friendlyId);
            var emailAgreement = await _mainQueries.PolicyDataLogic.GetNodeValueAsync(friendlyId, "IAgreeToReceiveEmailsByBolt");
            var isUnchecked = string.IsNullOrEmpty(emailAgreement) || emailAgreement.Equals("false", StringComparison.OrdinalIgnoreCase);

            _logger.LogDataValidation("Email Agreement Unchecked", isUnchecked, "null or 'false'", emailAgreement ?? "null", "Should be unchecked");
            Assert.That(isUnchecked, Is.True, $"Expected unchecked but got: '{emailAgreement}'");

            await pageHelper.ClickBrowserBackButton();
            personalDetailsPage = pageFactory.CreatePage<D2C_PersonalDetailsPage>();
            await personalDetailsPage.SelectDeSelectIAgreeToReceiveEmailsByBoltSelected(true);
            await personalDetailsPage.ClickContinue();

            policyPage = pageFactory.CreatePage<D2C_PolicyDatePickerPage>();
            _mainQueries.PolicyDataLogic.ClearXmlCache(friendlyId);
            emailAgreement = await _mainQueries.PolicyDataLogic.GetNodeValueAsync(friendlyId, "IAgreeToReceiveEmailsByBolt");

            _logger.LogDataValidation("Email Agreement Checked", emailAgreement == "true", "true", emailAgreement ?? "null", "Should be 'true' when checked");
            Assert.That(emailAgreement, Is.EqualTo("true"), "Should be 'true' when checked");
        }

        /// <summary>Validates policy has meaningful data (not empty or placeholder XML)</summary>
        public async Task<string> WaitForMeaningfulPolicyData(IPollyRetryService pollyRetry, string friendlyId)
        {
            var policyData = await pollyRetry.ExecuteWithExceptionAsync(async () =>
            {
                var policy = await _mainQueries.Policy.GetPolicyByFriendlyIdAsync(friendlyId);
                return policy?.PolicyData != null &&
                       !string.IsNullOrWhiteSpace(policy.PolicyData) &&
                       !policy.PolicyData.Trim().Equals("<PolicyData />", StringComparison.OrdinalIgnoreCase) &&
                       !policy.PolicyData.Trim().Equals("<PolicyData></PolicyData>", StringComparison.OrdinalIgnoreCase)
                    ? policy
                    : null;
            }, 10, $"Policy with meaningful PolicyData not found for friendlyId: {friendlyId}");

            _logger.LogDataValidation("Policy Data Exists", policyData != null, "Not Null", policyData != null ? "Present" : "null", "Policy data should exist");
            Assert.That(policyData, Is.Not.Null, "Policy data should exist");

            return friendlyId;
        }

        /// <summary>Validates DOB masking format (XX/XX/yyyy)</summary>
        public async Task ValidateDobMasking(IPageHelper pageHelper, string fieldName)
        {
            var dobValue = await pageHelper.GetFieldValue(fieldName);
            var isMasked = dobValue.Contains("XX/XX/");

            _logger.LogDataValidation("DOB Masking",
                isMasked,
                "Contains XX/XX/",
                dobValue,
                "DOB should be masked as XX/XX/yyyy");

            Assert.That(dobValue, Does.Contain("XX/XX/"),
                $"Expected DOB masking 'XX/XX/yyyy', got: [{dobValue}]");
        }

        /// <summary>Navigate back multiple times with delay</summary>
        public async Task NavigateBackMultipleTimes(IPageHelper pageHelper, int count, int delayMs = 500)
        {
            for (int i = 0; i < count; i++)
            {
                await pageHelper.ClickBrowserBackButton();
                await Task.Delay(delayMs);
            }

            _logger.Debug($"Navigated back {count} times with {delayMs}ms delay");
        }
    }
}
