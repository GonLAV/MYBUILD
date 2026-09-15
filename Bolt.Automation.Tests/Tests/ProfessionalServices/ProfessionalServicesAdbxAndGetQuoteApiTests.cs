using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.ApplicationTestData;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using Bolt.Automation.Common;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.ProfessionalServices
{
    /// <summary>
    /// Smallest end-to-end Professional Services onboarding test — proves the
    /// <see cref="UIInjectionTestBase"/> foundation works against a newly onboarded tenant
    /// without depending on the static <c>(Tenant, Environment)</c>-keyed data stores.
    ///
    /// Ports the spirit of <c>BOLTAG_Agent_Sidebar_Logout_Test</c> from <c>AdbxSidebarNavigationTests</c>
    /// (smallest existing ADBX test that does login + a single UI action + assert + logging),
    /// but reads URL/credentials from <c>INJECTED_*</c> environment variables instead of
    /// <c>TestContextAccessor</c>.
    /// </summary>
    public class ProfessionalServicesAdbxAndGetQuoteApiTests : UIInjectionTestBase
    {
        private static readonly HashSet<string> _requiredSections = new()
        {
            nameof(InjectedTestConfig.Adbx),
            nameof(InjectedTestConfig.GetQuoteApi)
        };

        protected override IReadOnlySet<string> RequiredSections => _requiredSections;
        private AdbxTestHelper _adbxHelper = null!;
        public IOutlookClient _outlookClient = null!;
        public IGetQuoteApi? _getQuoteApi;
        protected override void ResolveServices()
        {
            _outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();

        }
        protected override void InitializeComponents()
        {
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
        }
        public ProfessionalServicesAdbxAndGetQuoteApiTests() : base()
        {
           
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
        }

        [Test]
        [Category("ProfessionalServices")]
        [TestCaseId(23646400)]
        [Author(Author.Sandy)]
        [Description("Send mail from interview results page and verify via outlook api")]
        public async Task ProfessionalServices_Email_Proposal()
        {
            var user = _injectedConfig.BuildAdbxWithGetQuoteApiUser();
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
   
            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                Data = PersonalLineDataProvider.GetPersonalHomeData(AddressData.CA)
            };

            var insured = requestData.Data!.FirstName;
            requestData.Data!.PLFloorNumber = 2;
            requestData.Data!.PL_NumberOfFloors = 2;

            await _logger.ExecuteStepAsync("Create and submit application via GetQuote API", async () =>
            {
                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(requestData);

            });

            await _adbxHelper.LoginAndNavigateToQuoteAsync(user,
       user.LoginUrl, ScopeContext.Data.FriendlyId);

            await _logger.ExecuteStepAsync("Send proposal email from results", async () =>
            {
                var quoteSummaryPage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();
                await quoteSummaryPage.ClickOnEditQuote();
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                var results = PageFactory.CreatePage<Product_ResultsPage>();
                var emailPopup = await results.ClickOnEmailQuotes();
                await emailPopup.FillForm();
                await emailPopup.ClickContinue();
                await emailPopup.ClickPopupConfirm();
            });

            await _logger.ExecuteStepAsync("Verify proposal email received", async () =>
            {
                const string subject = "Your Insurance Quote Proposal";
                const string sender = "Bolt";
                var isEmailReceived = await _outlookClient.IsEmailReceived(sender, subject, insured);
                Assert.That(isEmailReceived, Is.True, "Proposal email was not received.");
            });

        }

        //used external variables
//        <RunSettings>
//	<RunConfiguration>
//		<EnvironmentVariables>
//			<INJECTED_TENANT>BOLTAG</INJECTED_TENANT>
//			<INJECTED_ENVIRONMENT>Qa</INJECTED_ENVIRONMENT>
//			<INJECTED_ADBX_LOGIN_URL>https://sts-qa-boltag.boltqa.com/Login/Login</INJECTED_ADBX_LOGIN_URL>
//			<INJECTED_ADBX_USERNAME>Service @Agent.com</INJECTED_ADBX_USERNAME>
//			<INJECTED_ADBX_PASSWORD></INJECTED_ADBX_PASSWORD>
//			<INJECTED_GETQUOTE_API_KEY>An3cuc4BrTR35E0tBvhQIvXQ8dEdBzqGCE13VImW</INJECTED_GETQUOTE_API_KEY>
//			<INJECTED_GETQUOTE_AGENT_IDENTITY>Basic U2VydmljZUE6U0VSVklDRVM =</ INJECTED_GETQUOTE_AGENT_IDENTITY >

//    </ EnvironmentVariables >

//    </ RunConfiguration >
//</ RunSettings >
    }
}
