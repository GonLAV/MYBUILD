using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using ADBXFieldNames = Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using InterviewFieldNames = Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.Tests.Tests
{
    public class SSOTests : UITestBase
    {
        protected IGetQuoteApi? _getQuoteApi;
        protected ISsoApiFactory _ssoApiFactory = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
        }

        [Test]
        [Tenant(USAA)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(212329)]
        public async Task SsoUsaaAgentToRegularInterview()
        {
            await _logger.ExecuteStepAsync("Setup USAA SSO Agent Context", () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                return Task.CompletedTask;
            }, "Agent user and Interview front end context are configured");

            var requestData = await _logger.ExecuteStepAsync("Create Application Via GetQuote API", async () =>
            {
                var data = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetPersonalAutoData(
                        AddressData.TX_Thomaston,
                        [Vehicles.Vin_JM3KFACM9K1246617],
                        [Drivers.UsaaTestDriver],
                        PersonalInfo.GetRandomPersonalInfo(),
                        PolicyTestData.UsaaPolicyTestData
                    )
                };
                data.Data.MailingAddress = AddressData.TX_Thomaston;

                var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(data);
                createApplicationResponse.EnsureSuccessContent($"Failed to create application : {createApplicationResponse.GetErrorContent()}");
                var applicationId = createApplicationResponse.Content!.Id;

                var getQuestionnaireResponse = await _getQuoteApi.GetQuestionnaireAsync(applicationId);
                var interviewUrl = getQuestionnaireResponse.Content?.Url;

                var relayStateTestData = new RelayStateTestData { Value = interviewUrl };
                ScopeContext.Set(ctx => ctx.CurrentRelayStateType, relayStateTestData);

                return data;
            }, "Application is created and the interview relay state is configured");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain USAA SSO Redirect URL", async () =>
            {
                var usaaSsoClient = _ssoApiFactory.CreateClient();
                var response = await usaaSsoClient.GetSsoResponse();
                var url = response.Content?.RedirectUrl;

                _logger.LogBusinessRule("SSO Redirect Contains Interview",
                    url?.Contains("interview") == true,
                    "SSO redirect should contain 'interview' in URL");
                Assert.That(url, Does.Contain("interview"));

                return url;
            }, "SSO redirect URL is returned and contains the interview path");

            var startPage = await _logger.ExecuteStepAsync("Navigate And Verify Pre-Populated Interview Data", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);
                var page = PageFactory.CreatePage<Product_StartPage>();

                var firstName = await _pageHelper.GetFieldValue(FieldNames.FirstName);
                var lastName = await _pageHelper.GetFieldValue(FieldNames.LastName);
                var address = await _pageHelper.GetFieldValue(InterviewFieldNames.InterviewAddress);

                var expectedAddress = $"{AddressData.TX_Thomaston.AddressLine1}, {AddressData.TX_Thomaston.City}, {AddressData.TX_Thomaston.State}, {AddressData.TX_Thomaston.ZipCode}";

                Assert.Multiple(() =>
                {
                    Assert.That(address, Is.EqualTo(expectedAddress),
                        $"Pre-populated address should match request data. Actual: {address}, expected :{expectedAddress}");
                    Assert.That(firstName, Is.EqualTo(requestData.Data.FirstName),
                        $"Pre-populated first name should match request data. Actual: {firstName}, expected :{requestData.Data.FirstName}");
                    Assert.That(lastName, Is.EqualTo(requestData.Data.LastName),
                        $"Pre-populated last name should match request data. Actual: {lastName}, expected :{requestData.Data.LastName}");
                });

                return page;
            }, "Pre-populated interview data matches the application request data");

            await _logger.ExecuteStepAsync("Navigate To LOB Selection And Verify", async () =>
            {
                await startPage.ClickContinue();
                var lobsPage = PageFactory.CreatePage<Product_LobsPage>();
                var selectedLobs = await lobsPage.GetSelectedLobs();

                Assert.Multiple(() =>
                {
                    Assert.That(selectedLobs, Has.Count.EqualTo(1),
                        $"Should have exactly one LOB selected. Actual: {selectedLobs.Count}, expected :1");
                    Assert.That(selectedLobs.First(), Is.EqualTo("Auto"),
                        $"Selected LOB should be Auto. Actual: {selectedLobs.First()}, expected :Auto");
                });
            }, "Auto LOB is selected on the LOB selection page");
        }

        [Test]
        [Tenant(USAA)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(212316)]
        public async Task SsoUsaaD2CAgentToD2CInterview()
        {
            await _logger.ExecuteStepAsync("Setup USAA SSO D2C Agent Context", () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.OnlineQuote;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
                return Task.CompletedTask;
            }, "OnlineQuote user and D2C front end context are configured");

            var requestData = await _logger.ExecuteStepAsync("Create D2C Application Via GetQuote API", async () =>
            {
                var data = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetPersonalAutoData(
                        AddressData.TX_Thomaston,
                        [Vehicles.Vin_JM3KFACM9K1246617],
                        [Drivers.UsaaTestDriver],
                        PersonalInfo.GetRandomPersonalInfo(),
                        PolicyTestData.UsaaPolicyTestData
                    )
                };
                data.Data.MailingAddress = AddressData.TX_Thomaston;

                _logger.LogJsonWithPreview("D2C Application Request Data", data);

                var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(data);
                createApplicationResponse.EnsureSuccessContent($"Failed to create application : {createApplicationResponse.GetErrorContent()}");
                var applicationId = createApplicationResponse.Content!.Id;

                var getQuestionnaireResponse = await _getQuoteApi.GetQuestionnaireAsync(applicationId);
                var interviewUrl = getQuestionnaireResponse.Content?.Url;

                var relayStateTestData = new RelayStateTestData { Value = interviewUrl };
                ScopeContext.Set(ctx => ctx.CurrentRelayStateType, relayStateTestData);

                return data;
            }, "D2C application is created and the interview relay state is configured");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain USAA D2C SSO Redirect URL", async () =>
            {
                var usaaSsoClient = _ssoApiFactory.CreateClient();
                var response = await usaaSsoClient.GetSsoResponse();
                var url = response.Content?.RedirectUrl;

                _logger.LogBusinessRule("D2C SSO Redirect Contains Interview",
                    url?.Contains("interview") == true,
                    "D2C SSO redirect should contain 'interview' in URL");
                Assert.That(url, Does.Contain("interview"));

                return url;
            }, "D2C SSO redirect URL is returned and contains the interview path");

            await _logger.ExecuteStepAsync("Navigate And Verify D2C Pre-Populated Address", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);
                var addressPage = PageFactory.CreatePage<D2C_YourAddressPage>();

                var actualAddress = await _pageHelper.GetFieldValue(FieldNames.OnlineAddress.ToString());
                var expectedAddress = $"{AddressData.TX_Thomaston.AddressLine1}, {AddressData.TX_Thomaston.City}, {AddressData.TX_Thomaston.State} {AddressData.TX_Thomaston.ZipCode}";

                Assert.That(actualAddress, Is.EqualTo(expectedAddress),
                    $"Pre-populated address should match request data in D2C flow. Actual: {actualAddress}, expected :{expectedAddress}");
            }, "Pre-populated D2C address matches the application request data");
        }

        [Test]
        [Tenant(USAA)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(28794)]
        public async Task SsoUsaaAgentToADBXHome()
        {
            await _logger.ExecuteStepAsync("Setup USAA SSO Agent Context", () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                return Task.CompletedTask;
            }, "Agent user context is configured");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain USAA SSO Redirect URL", async () =>
            {
                var usaaSsoClient = _ssoApiFactory.CreateClient();
                var response = await usaaSsoClient.GetSsoResponse();
                return response.Content?.RedirectUrl;
            }, "SSO redirect URL is returned");

            await _logger.ExecuteStepAsync("Navigate And Verify ADBX Home Page", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();

                Assert.That(homePage.Page.Url, Does.Contain("adbx"),
                    $"Agent should land on the ADBX home page. Actual: {homePage.Page.Url}, expected to contain :adbx");
            }, "Agent lands on the ADBX home page");
        }

        [Test]
        [Tenant(LIBERTYX)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(150540)]
        public async Task SsoLibertyXAgentToAdbxMCC()
        {
            var mccRelay = TestContextAccessor.CurrentRelayStateCollection.AgentMccRelayState;
            var mccRealUrl = mccRelay.Value.Aggregate(string.Empty, (current, next) => current + next);
            await _logger.ExecuteStepAsync("Setup LibertyX SSO Agent Context", () =>
            {
                
                ScopeContext.Set(ctx => ctx.CurrentRelayStateType, mccRelay);
                var user = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                return Task.CompletedTask;
            }, "Agent user and MCC relay state context are configured");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain LibertyX SSO Redirect URL", async () =>
            {
                var libertyXSsoClient = _ssoApiFactory.CreateClient();
                var response = await libertyXSsoClient.GetSsoResponse();
                return response.Content?.RedirectUrl;
            }, "SSO redirect URL is returned");

            await _logger.ExecuteStepAsync("Navigate And Verify ADBX MCC Page", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);
                var isNavigatedToMccPage = await _pageHelper!.WaitForNavigationOrUrlContainsAsync(mccRealUrl, 15000, true);
                Assert.That(isNavigatedToMccPage, Is.True,
                    $"Agent should land on an ADBX-hosted MCC page within 15000 ms. Actual: {_currentPage.Url}, expected to be: {mccRealUrl}");

            }, "Agent lands on the ADBX My Carrier Credentials page");
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [RunIn(includeProduction: true)]
        [TestCaseId(181135)]
        public async Task SsoPartnerPortalWFGAgentToLoginPage()
        {
            await _logger.ExecuteStepAsync("Setup BOLTAG SSO WFG Agent Context", () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.WFGAgent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                ScopeContext.Set(ctx => ctx.SamlTemplate, SamlTemplateType.SamlPartnerPortal);
                return Task.CompletedTask;
            }, "WFG agent user and Partner Portal SAML template context are configured");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain BOLTAG SSO Redirect URL", async () =>
            {
                var boltagSsoClient = _ssoApiFactory.CreateClient();
                var response = await boltagSsoClient.GetSsoResponse();
                return response.Content?.RedirectUrl;
            }, "SSO redirect URL is returned");

            await _logger.ExecuteStepAsync("Navigate And Verify Partner Portal Login Page", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);
                var loginPage = PageFactory.CreatePage<STS_LoginPage>();

                Assert.That(loginPage.Page.Url, Does.Contain("partnerportal"),
                    $"WFG agent should be redirected to the Partner Portal login page. Actual: {loginPage.Page.Url}, expected to contain :partnerportal");
            }, "WFG agent lands on the Partner Portal login page");
        }

        [Test]
        [Ignore("adbx-enable-permission-guard flag is temporary off + 253248")]
        [Tenant(COMPARION)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(150825)]
        [Description("Navigate as comparion agent via sso to users page and check Access Denied")]
        public async Task SsoComparionAgentUsersRelayStateAccessDeniedTest()
        {
            await _logger.ExecuteStepAsync("Setup COMPARION SSO Agent Context", () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
                ScopeContext.Set(ctx => ctx.CurrentRelayStateType, TestContextAccessor.CurrentRelayStateCollection.UsersRelayState);
                var user = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                return Task.CompletedTask;
            }, "Agent user and UsersRelayState context are configured for the ADBX front end");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain COMPARION SSO Redirect URL", async () =>
            {
                var comparionSsoClient = _ssoApiFactory.CreateClient();
                var response = await comparionSsoClient.GetSsoResponse();
                return response.Content?.RedirectUrl;
            }, "SSO redirect URL is returned");

            await _logger.ExecuteStepAsync("Navigate And Verify Access Denied", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);

                var accessDeniedPage = PageFactory.CreatePage<ADBX_AccessDeniedPage>();
                _logger.LogBusinessRule("AgentRedirectedToAccessDenied",
                    accessDeniedPage.Page.Url.Contains("access-denied"),
                    "SSO with UsersRelayState should redirect the agent to the access-denied page");
                Assert.That(accessDeniedPage.Page.Url, Does.Contain("access-denied"));
            }, "Agent is redirected to the access-denied page");
        }

        [Test]
        [Tenant(COMPARION)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(200973)]
        [Description("Navigate as comparion agent via sso to dummy adbx url and check error 203")]
        public async Task SsoComparionAgentKickoutTestError203()
        {
            const string expectedErrorCode = "203";
            var expectedErrorMessage = $"We're sorry, but there was an error processing your request. Please try again later or contact technical support for assistance. Error code: {expectedErrorCode}.";

            await _logger.ExecuteStepAsync("Setup COMPARION SSO Agent Kickout Context", () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
                ScopeContext.Set(ctx => ctx.CurrentRelayStateType, TestContextAccessor.CurrentRelayStateCollection.DummyAdbxRelayState);
                var user = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                return Task.CompletedTask;
            }, "Agent user and DummyAdbxRelayState context are configured for the ADBX front end");

            var redirectUrl = await _logger.ExecuteStepAsync("Obtain COMPARION SSO Redirect URL", async () =>
            {
                var comparionSsoClient = _ssoApiFactory.CreateClient();
                var response = await comparionSsoClient.GetSsoResponse();
                return response.Content?.RedirectUrl;
            }, "SSO redirect URL is returned");

            await _logger.ExecuteStepAsync("Navigate And Verify Kickout Error", async () =>
            {
                await BrowserManager.NavigateAsync(redirectUrl);

                var reachedKickout = await _pageHelper!.WaitForNavigationOrUrlContainsAsync("kickout");
                _logger.LogBusinessRule("NavigatedToKickout",
                    reachedKickout,
                    "Agent should be navigated to a kickout URL");
                Assert.That(reachedKickout, Is.True);

                var reachedAdbx = await _pageHelper!.WaitForNavigationOrUrlContainsAsync("adbx");
                _logger.LogBusinessRule("KickoutUrlIsAdbx",
                    reachedAdbx,
                    "Kickout URL should be hosted under adbx");
                Assert.That(reachedAdbx, Is.True);

                var actualErrorMessage = await _pageHelper!.GetFieldValue(ADBXFieldNames.KickoutErrorMessage);
                Assert.That(actualErrorMessage, Is.EqualTo(expectedErrorMessage),
                    $"Kickout error message should match error code {expectedErrorCode}. Actual: {actualErrorMessage}, expected :{expectedErrorMessage}");
            }, $"Agent is kicked out with error code {expectedErrorCode}");
        }

        [Test]
        [Tenant(COMPARION)]
        [Category("SSO")]
        [Category("CRM")]
        [Author(Author.Andrii)]
        [TestCaseId(200974)]
        public async Task SsoComparionAgentKickoutTestError109()
        {
            const string expectedErrorMessage =
                "We're sorry, but there was an error processing your request. Please try again later or contact technical support for assistance. Error code: 109.";

            await _logger.ExecuteStepAsync("Setup COMPARION ADBX Home Dashboard Context", () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
                return Task.CompletedTask;
            }, "ADBX front end context is configured");

            await _logger.ExecuteStepAsync("Navigate And Verify Kickout Error", async () =>
            {
                var homeDashboardUrl = TestContextAccessor.CurrentUrlCollection.FrontEnd.AdditionalUrls["HomeDashboard"];
                await BrowserManager.NavigateAsync(homeDashboardUrl);

                var reachedKickout = await _pageHelper!.WaitForNavigationOrUrlContainsAsync("kickout");
                _logger.LogBusinessRule("NavigatedToKickout",
                    reachedKickout,
                    "Agent should be navigated to a kickout URL");
                Assert.That(reachedKickout, Is.True);

                var reachedAdbx = await _pageHelper!.WaitForNavigationOrUrlContainsAsync("adbx");
                _logger.LogBusinessRule("KickoutUrlIsAdbx",
                    reachedAdbx,
                    "Kickout URL should be hosted under adbx");
                Assert.That(reachedAdbx, Is.True);

                var actualErrorMessage = await _pageHelper!.GetFieldValue(ADBXFieldNames.KickoutErrorMessage);
                Assert.That(actualErrorMessage, Is.EqualTo(expectedErrorMessage),
                    $"Kickout error message should match error code 109. Actual: {actualErrorMessage}, expected :{expectedErrorMessage}");
            }, "Agent is kicked out with error code 109");
        }
    }

}

