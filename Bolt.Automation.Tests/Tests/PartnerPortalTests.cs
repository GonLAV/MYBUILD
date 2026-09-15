using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces;
using Bolt.Automation.Common;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.D2C.Flows;
using Bolt.Automation.FrontEnds.Projects.D2C.FormData;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages;
using Bolt.Automation.TestDataProvider.TestData.PartnerPortalTestData;
using Bolt.Automation.Tests.TestData.PartnerPortal;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Bolt.Automation.Tests.TestHelpers.PartnerPortal;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Globalization;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using BoltEnvironment = Bolt.Automation.Common.Environment;
using FieldNamesInterview = Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;
using Microsoft.Playwright;

namespace Bolt.Automation.Tests.Tests
{
    public class PartnerPortalTests : UITestBase
    {
        public const string EmailTitle = "Your quote is waiting!";
        public string EmailFrom = "bolt";
        public FrontEndType? FrontEnd { get; set; }
        public IOutlookClient _outlookClient = null!;
        private AdbxTestHelper _adbxHelper = null!;
        private PartnerPortalTestHelper _partnerPortalHelper = null!;
        private IPartnerPortalApi _partnerPortalApi = null!;
        private IPollyRetryService _pollyRetryService = null!;

        public PartnerPortalTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.PartnerPortal);
        }

        protected override void ResolveServices()
        {
            _outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
            _partnerPortalApi = _testScope.ServiceProvider.GetRequiredService<IPartnerPortalApi>();
            _pollyRetryService = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        protected override void InitializeComponents()
        {
            _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
            _partnerPortalHelper = new PartnerPortalTestHelper(_logger, PageFactory, _pageHelper!);
        }


        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [Category("Sanity")]
        [TestCaseId(139990)]
        [Author(Author.Sandy)]
        [Description("Create invite via UI and check email receive")]
        public async Task PartnerPortal_SendInvite_EmailReceive()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            var firstNameValue = FieldRegistryPartnerPortal.Fields["FirstName"].DefaultValue;

            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            var referralsCount = await _logger.ExecuteStepAsync("Read referrals count (before)", async () =>
            {
                var referralsCountText = await _pageHelper!.GetFieldValue(PartnerPortal_FieldNames.ReferralsCount);
                int.TryParse(referralsCountText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count);
                _logger?.Debug($"Referrals count (before): '{referralsCountText}' => {count}");
                return count;
            });

            await _logger.ExecuteStepAsync("Create invite", async () =>
            {
                var popup = await _partnerPortalHelper.SendInviteFromHomePageAsync(homePage);
                await popup.ClosePopup();

                var invitePage = PageFactory.CreatePage<PartnerPortal_InvitePage>();
                await invitePage.ReturnToHomePage();
            });

            var referralsCountNew = await _logger.ExecuteStepAsync("Read referrals count (after)", async () =>
            {
                var referralsCountNewText = await _pageHelper!.GetFieldValue(PartnerPortal_FieldNames.ReferralsCount);
                int.TryParse(referralsCountNewText?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count);
                _logger?.Debug($"Referrals count (after): '{referralsCountNewText}' => {count}");
                return count;
            });

            await _logger.ExecuteStepAsync("Verify email received", async () =>
            {
                var resp = await _outlookClient.IsEmailReceived(EmailFrom, EmailTitle, firstNameValue);
                Assert.That(resp, Is.True, $"Expected to receive email with title: {EmailTitle} from: {EmailFrom}, but did not receive it.");
            });

            await _logger.ExecuteStepAsync("Verify referrals count increased", async () =>
            {
                Assert.That(referralsCountNew, Is.GreaterThan(referralsCount),
                    $"Expected referrals count to increase by at least 1 after sending invite, but it did not. Before: {referralsCount}, After: {referralsCountNew}");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(232882)]
        [Author(Author.Sandy)]
        [Description("Create CL invite via api, click from email response deeplink and  verify pre-filled fields")]
        public async Task PartnerPortal_SendInvite_ConsumerSite_CL()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            var firstNameValue = FieldRegistryPartnerPortal.Fields["FirstName"].DefaultValue;
            var lastNameValue = FieldRegistryPartnerPortal.Fields["LastName"].DefaultValue;
            var addressValue = FieldRegistryPartnerPortal.Fields["OnlineAddress"].DefaultValue;
            var formData = new Dictionary<string, string>
            {
                [Lob] = "Business Owners",
            };
            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            await _logger.ExecuteStepAsync("Create invite and verify in leads page", async () =>
            {
                var popup = await _partnerPortalHelper.SendInviteFromHomePageAsync(homePage, formData);
                var leadsPage = await popup.ClickOnGoToLeads();
                await leadsPage.SearchRecentRecords(firstNameValue);
                var customerExpected = firstNameValue + " " + lastNameValue;
                await VerifyLeadsTableAsync(leadsPage, customerExpected, _pollyRetryService);
            });

            var quoteLink = await _logger.ExecuteStepAsync("Verify email received and extract deeplink", async () =>
            {
                var resp = await _outlookClient.GetSpecificEmail(EmailFrom, EmailTitle, firstNameValue);
                var emailContent = resp?.Body?.Content;
                Assert.That(emailContent, Is.Not.Null.And.Not.Empty, "Email body content was empty.");

                var link = _outlookClient.ExtractQuoteLink(emailContent!);
                Assert.That(link, Is.Not.Null.And.Not.Empty, "Quote link was not found in email body.");
                return link;
            });

            await _logger.ExecuteStepAsync("Navigate via deeplink and verify pre-filled fields on consumer site", async () =>
            {
                await BrowserManager.NavigateAsync(quoteLink!);

                var startPage = PageFactory.CreatePage<Product_StartPage>();
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                var firstName = await startPage.PageHelper.GetFieldValue(FirstName);
                Assert.That(firstName, Is.EqualTo(firstNameValue), $"Expected First Name field value: '{firstNameValue}', but got: '{firstName}'");
                var lastName = await startPage.PageHelper.GetFieldValue(LastName);
                Assert.That(lastName, Is.EqualTo(lastNameValue), $"Expected Last Name field value: '{lastNameValue}', but got: '{lastName}'");
                var address = await startPage.PageHelper.GetFieldValue(FieldNamesInterview.InterviewAddress);
                var normalizedExpectedAddress = TextHelper.NormalizeAddress(addressValue);
                var normalizedAddress = TextHelper.NormalizeAddress(address);
                Assert.That(normalizedAddress, Is.EqualTo(normalizedExpectedAddress), $"Expected Online Address field value: '{addressValue}', but got: '{address}'");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(233441)]
        [Author(Author.Sandy)]
        [Description("Create PL invite via api, click from email response deeplink and  verify site mappings")]
        public async Task PartnerPortal_SendInvite_ConsumerSite_PL()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            var firstNameValue = FieldRegistryPartnerPortal.Fields["FirstName"].DefaultValue;
            var lastNameValue = FieldRegistryPartnerPortal.Fields["LastName"].DefaultValue;
            var addressValue = FieldRegistryPartnerPortal.Fields["OnlineAddress"].DefaultValue;
            var formData = new Dictionary<string, string>
            {
                [Lob] = "Renters",
            };
            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            await _logger.ExecuteStepAsync("Create invite and verify in leads page", async () =>
            {
                var popup = await _partnerPortalHelper.SendInviteFromHomePageAsync(homePage, formData);
                var leadsPage = await popup.ClickOnGoToLeads();
                await leadsPage.SearchRecentRecords(firstNameValue);
                var customerExpected = firstNameValue + " " + lastNameValue;
                await VerifyLeadsTableAsync(leadsPage, customerExpected, _pollyRetryService);
            });

            var quoteLink = await _logger.ExecuteStepAsync("Verify email received and extract deeplink", async () =>
            {
                var resp = await _outlookClient.GetSpecificEmail(EmailFrom, EmailTitle, firstNameValue);
                var emailContent = resp?.Body?.Content;
                Assert.That(emailContent, Is.Not.Null.And.Not.Empty, "Email body content was empty.");

                var link = _outlookClient.ExtractQuoteLink(emailContent!);
                Assert.That(link, Is.Not.Null.And.Not.Empty, "Quote link was not found in email body.");
                return link;
            });

            await _logger.ExecuteStepAsync("Navigate via deeplink and verify pre-filled fields on consumer site", async () =>
            {
                await BrowserManager.NavigateAsync(quoteLink!);

                var startPage = PageFactory.CreatePage<Product_StartPage>();
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
                var firstName = await startPage.PageHelper.GetFieldValue(FirstName);
                Assert.That(firstName, Is.EqualTo(firstNameValue), $"Expected First Name field value: '{firstNameValue}', but got: '{firstName}'");
                var lastName = await startPage.PageHelper.GetFieldValue(LastName);
                Assert.That(lastName, Is.EqualTo(lastNameValue), $"Expected Last Name field value: '{lastNameValue}', but got: '{lastName}'");
                var address = await startPage.PageHelper.GetFieldValue(FieldNamesInterview.InterviewAddress);
                var normalizedExpectedAddress = TextHelper.NormalizeAddress(addressValue);
                var normalizedAddress = TextHelper.NormalizeAddress(address);
                Assert.That(normalizedAddress, Is.EqualTo(normalizedExpectedAddress), $"Expected Online Address field value: '{addressValue}', but got: '{address}'");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(232212)]
        [Author(Author.Sandy)]
        [Description("Create Condo invite via UI, click email deeplink and verify D2C site pre-fill mappings")]
        public async Task PartnerPortal_SendInvite_D2C_Deeplink()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            var firstNameValue = FieldRegistryPartnerPortal.Fields["FirstName"].DefaultValue;
            var lastNameValue = FieldRegistryPartnerPortal.Fields["LastName"].DefaultValue;
            var addressValue = FieldRegistryPartnerPortal.Fields["OnlineAddress"].DefaultValue;
            var formData = new Dictionary<string, string>
            {
                [Lob] = "Condominium",
            };
            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            await _logger.ExecuteStepAsync("Create invite and verify in leads page", async () =>
            {
                var popup = await _partnerPortalHelper.SendInviteFromHomePageAsync(homePage, formData);
                var leadsPage = await popup.ClickOnGoToLeads();
                await leadsPage.SearchRecentRecords(firstNameValue);
                var customerExpected = firstNameValue + " " + lastNameValue;
                await VerifyLeadsTableAsync(leadsPage, customerExpected, _pollyRetryService);
            });

            var quoteLink = await _logger.ExecuteStepAsync("Verify email received and extract deeplink", async () =>
            {
                var resp = await _outlookClient.GetSpecificEmail(EmailFrom, EmailTitle, firstNameValue);
                var emailContent = resp?.Body?.Content;
                Assert.That(emailContent, Is.Not.Null.And.Not.Empty, "Email body content was empty.");

                var link = _outlookClient.ExtractQuoteLink(emailContent!);
                Assert.That(link, Is.Not.Null.And.Not.Empty, "Quote link was not found in email body.");
                return link;
            });

            await _logger.ExecuteStepAsync("Navigate via deeplink and verify pre-filled address on D2C site", async () =>
            {
                await BrowserManager.NavigateAsync(quoteLink!);
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
                var startPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                var address = await startPage.PageHelper.GetFieldValue(OnlineAddress);
                var normalizedExpectedAddress = TextHelper.NormalizeAddress(addressValue);
                var normalizedAddress = TextHelper.NormalizeAddress(address);
                Assert.That(normalizedAddress, Is.EqualTo(normalizedExpectedAddress), $"Expected Online Address field value: '{addressValue}', but got: '{address}'");
            });
        }


        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(140032)]
        [Author(Author.Sandy)]
        [Description("Bind pl policy and verify resend status")]
        public async Task PartnerPortal_SendInvite_PL_PolicyBinder()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var request = SendInviteRequestTestData.SendPLInviteRequest;

            await _logger.ExecuteStepAsync("Send PL invite via Partner Portal API", async () =>
            {
                await _partnerPortalApi.SendInviteAsync(request)
                    .EnsureSuccessContentAsync("Failed to send partner portal invite");
            });

            var policiesCountBefore = await _logger.ExecuteStepAsync("Get PL invite details via Partner Portal API and capture policies number", async () =>
            {
                var resp = await _partnerPortalApi.GetInvitationDetailsAsync()
               .EnsureSuccessContentAsync("Failed to get invitation details");
                return resp.TotalIssued;
            });

            user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
            await _adbxHelper.LoginAsync(user,
       ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Search for invited lead and open quote in ADBX", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.SearchFor(request.FirstName);
                PageFactory.CreatePage<ADBX_SearchResultsPage>();
                await _pageHelper!.InteractWithField(QuotesTabSearchResults);
                await _pageHelper!.SelectTableRowAsync(1);
            });

            await _logger.ExecuteStepAsync("Create policy", async () =>
            {
                var (policySummaryPage, policyNumber) = await _adbxHelper.CreatePolicyAsync();
            });

            user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Verify invite status is Issued and shouldResend is false via Partner Portal API", async () =>
            {
                var matchingInvitation = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var requestInviteDetails = ProgressPaginationRequestTestData.Create("Issued");
                    var respInviteDetails = await _partnerPortalApi.GetInvitationDetailsByAgentAsync(requestInviteDetails)
                        .EnsureSuccessContentAsync();
                    return respInviteDetails.Invitations?.FirstOrDefault(x => x.FirstName == request.FirstName);
                }, timeOutSeconds: 30);
                Assert.That(matchingInvitation, Is.Not.Null,
                    $"Expected to find an invitation with First Name '{request.FirstName}', but it was not found.");
                Assert.That(matchingInvitation!.ShouldResend, Is.False,
                    $"Expected ShouldResend=false for First Name '{request.FirstName}', but got '{matchingInvitation.ShouldResend}'.");
                Assert.That(matchingInvitation.PolicyIssuedDate, Is.Not.Null,
                    $"Expected PolicyIssuedDate to be set for First Name '{request.FirstName}', but it was null.");
                Assert.That(matchingInvitation.PolicyIssuedDate!.StartsWith(DateTime.Today.ToString("MM/dd/yyyy")), Is.True,
                    $"Expected PolicyIssuedDate to start with today's date '{DateTime.Today:MM/dd/yyyy}' for First Name '{request.FirstName}', but got '{matchingInvitation.PolicyIssuedDate}'.");
            });

            var policiesCountAfter = await _logger.ExecuteStepAsync("Get PL invite details via Partner Portal API and capture policies number after creating policy", async () =>
            {
                var resp = await _partnerPortalApi.GetInvitationDetailsAsync()
               .EnsureSuccessContentAsync("Failed to get invitation details");
                return resp.TotalIssued;
            });

            await _logger.ExecuteStepAsync("Verify policies count increased at least by 1 after policy creation", async () =>
            {
                Assert.That(policiesCountAfter, Is.GreaterThanOrEqualTo(policiesCountBefore + 1),
                    $"Expected policies count to increase by at least 1 after creating policy, but it did not. Before: {policiesCountBefore}, After: {policiesCountAfter}");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(232077)]
        [Author(Author.Sandy)]
        [Description("Create Homeowners invite via UI as agent and verify admin sees lead with correct Invitation origin and referrer via API")]
        public async Task PartnerPortal_Admin_Groups_Visibility_Lead_Referrer_Origin_Invitation()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            var firstNameValue = FieldRegistryPartnerPortal.Fields["FirstName"].DefaultValue;
            var lastNameValue = FieldRegistryPartnerPortal.Fields["LastName"].DefaultValue;
            var formData = new Dictionary<string, string>
            {
                [Lob] = "Homeowners",
            };
            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            var popup = await _logger.ExecuteStepAsync("Click on envelope icon and create invite", async () =>
            {
                var popup = await _partnerPortalHelper.SendInviteViaEnvelopeIconAsync(firstNameValue, lastNameValue, formData);
                return popup;
            });

            await _logger.ExecuteStepAsync("Closing popup and  getting empty invite page", async () =>
            {
                await _partnerPortalHelper.ClosePopupAndWaitForPageResetAsync(popup);
                var value = await _pageHelper.GetFieldValue(FirstName);
                Assert.That(value, Is.Null.Or.Empty, $"Expected First Name field to be empty after closing popup, but got: '{value}'");
            });

            user = TestContextAccessor.CurrentUserCollection.PartnerPortalAdmin;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Verify invitation has Invitation origin and referrer via Partner Portal API", async () =>
            {
                var matchingInvitation = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var requestInviteDetails = ProgressPaginationRequestTestData.Create("Referred");
                    var respInviteDetails = await _partnerPortalApi.GetInvitationDetailsByAgentAsync(requestInviteDetails)
                        .EnsureSuccessContentAsync();
                    return respInviteDetails.Invitations?.FirstOrDefault(x => x.FirstName == firstNameValue);
                }, timeOutSeconds: 30);
                Assert.That(matchingInvitation, Is.Not.Null,
                    $"Expected to find an invitation with First Name '{firstNameValue}', but it was not found.");
                Assert.That(matchingInvitation!.Origin, Is.EqualTo("Invitation"),
                    $"Expected Origin 'Invitation' for First Name '{firstNameValue}', but got '{matchingInvitation.Origin}'.");
                Assert.That(matchingInvitation.Referrer, Is.EqualTo("automationpp Agent"),
                    $"Expected Referrer 'automationpp Agent' for First Name '{firstNameValue}', but got '{matchingInvitation.Referrer}'.");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(232087)]
        [Author(Author.Sandy)]
        [Description("Send invite via API, start Homeowners quote in ADBX as service agent, and verify admin sees invitation with Agent origin and referrer")]
        public async Task PartnerPortal_Admin_Groups_Visibility_Lead_Referrer_Origin_Agent()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var request = SendInviteRequestTestData.SendPLInviteRequest;
            request.Email = RandomManager.GetRandomEmail();

            await _logger.ExecuteStepAsync("Send invite via Partner Portal API", async () =>
            {
                await _partnerPortalApi.SendInviteAsync(request)
                    .EnsureSuccessContentAsync("Failed to send partner portal invite");
            });

            user = TestContextAccessor.CurrentUserCollection.ServiceAgent;
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
            await _adbxHelper.LoginAsync(user,
            ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Search for invited customer and start Homeowners quote in ADBX", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.SearchFor(request.Email);
                PageFactory.CreatePage<ADBX_SearchResultsPage>();
                await _pageHelper!.SelectTableRowAsync(1);
                var accountSummaryPage = PageFactory.CreatePage<ADBX_AccountSummaryPage>();
                await accountSummaryPage.ClickOnNewQuoteFromExistingAccount();
                var startPage = PageFactory.CreatePage<Product_StartPage>();
                await _pageHelper!.InteractWithField(DateOfBirth);
                await startPage.SelectLob("Homeowners");
                await startPage.ClickContinue();
                var marketsPage = PageFactory.CreatePage<Product_MarketsPage>();
                await marketsPage.ClickContinue();
                PageFactory.CreatePage<Product_HomePage>();
            });

            user = TestContextAccessor.CurrentUserCollection.PartnerPortalAdmin;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Verify invitation has origin and referrer via Partner Portal API", async () =>
            {
                var matchingInvitation = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var requestInviteDetails = ProgressPaginationRequestTestData.Create("Referred");
                    var respInviteDetails = await _partnerPortalApi.GetInvitationDetailsByAgentAsync(requestInviteDetails)
                        .EnsureSuccessContentAsync();
                    return respInviteDetails.Invitations?.FirstOrDefault(x => x.FirstName == request.FirstName);
                }, timeOutSeconds: 30);
                Assert.That(matchingInvitation, Is.Not.Null,
                    $"Expected to find an invitation with First Name '{request.FirstName}', but it was not found.");
                Assert.That(matchingInvitation!.Origin, Is.EqualTo("Agent"),
                    $"Expected Origin 'Agent' for First Name '{request.FirstName}', but got '{matchingInvitation.Origin}'.");
                Assert.That(matchingInvitation.Referrer, Is.EqualTo("automationpp Agent"),
                    $"Expected Referrer 'automationpp Agent' for First Name '{request.FirstName}', but got '{matchingInvitation.Referrer}'.");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [TestCaseId(232096)]
        [Author(Author.Sandy)]
        [Description("Use agent consumer link to start D2C auto flow and verify admin sees invitation with origin link and referrer via API")]
        public async Task PartnerPortal_Admin_Groups_Visibility_Lead_Referrer_Origin_Agent_Link()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;

            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            var consumerUrl = await _logger.ExecuteStepAsync("Capture consumer flow URL from page, click copy and confirm the copied notice", async () =>
            {
                var url = await _pageHelper!.GetFieldValue(PartnerPortal_FieldNames.ConsumerFlowUrl);
                await homePage.ClickCopyLinkAsync();
                return url;
            });

            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
            var firstName = NameSelector.GetFirstName();
            var lastName = NameSelector.GetLastName();

            await _logger.ExecuteStepAsync("Navigate to agent consumer link and execute D2C auto flow", async () =>
            {
                await Executor.Execute<D2C_YourAddressPage, D2C_AdditionalDriversPage>(
                    FlowType.D2CAutoFlow,
                    new Dictionary<string, string>
                    {
                        [FirstName] = firstName,
                        [LastName] = lastName
                    },
                    fillForms: true,
                    startUrl: consumerUrl
                );
            });

            user = TestContextAccessor.CurrentUserCollection.PartnerPortalAdmin;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Verify invitation has origin and referrer via Partner Portal API", async () =>
            {
                var matchingInvitation = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var requestInviteDetails = ProgressPaginationRequestTestData.Create("Referred");
                    var respInviteDetails = await _partnerPortalApi.GetInvitationDetailsByAgentAsync(requestInviteDetails)
                        .EnsureSuccessContentAsync();
                    return respInviteDetails.Invitations?.FirstOrDefault(x => x.FirstName == firstName);
                }, timeOutSeconds: 30);
                Assert.That(matchingInvitation, Is.Not.Null,
                    $"Expected to find an invitation with First Name '{firstName}', but it was not found.");
                Assert.That(matchingInvitation!.Origin, Is.EqualTo("Link"),
                    $"Expected Origin 'Link' for First Name '{firstName}', but got '{matchingInvitation.Origin}'.");
                Assert.That(matchingInvitation.ShouldResend, Is.False,
                    $"Expected ShouldResend=false for First Name '{firstName}', but got '{matchingInvitation.ShouldResend}'.");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [RunIn(includeProduction: true, includeStaging: true)]
        [Category("PartnerPortal")]
        [Author(Author.Sandy)]
        [Description("Verify activity counts navigate to leads and show expected invitations")]
        [TestCaseSource(typeof(PartnerPortalTestCases), nameof(PartnerPortalTestCases.ClickableActivities))]
        public async Task PartnerPortal_Clickable_Activities(string status, string buttonFieldName)
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAdmin;

            await _adbxHelper.LoginAsync(
                   user,
                   user.LoginUrl);

            PageFactory.CreatePage<PartnerPortal_HomePage>();
            var leadsPage = await _logger.ExecuteStepAsync($"Open {status} leads", async () =>
            {
                await _pageHelper!.InteractWithField(buttonFieldName);
                return PageFactory.CreatePage<PartnerPortal_LeadsPage>();
            });

            await _logger.ExecuteStepAsync("Validate leads search and status", async () =>
            {
                var searchValue = await _pageHelper.GetFieldValue(PartnerPortal_FieldNames.RecentSearch);
                Assert.That(searchValue.Equals(status), Is.True, $"Search value is not '{status}'");
                var isStatusExist = await leadsPage.IsSpecificColumnHaveDataInTableAsync(status, "Status", _pollyRetryService);
                Assert.That(isStatusExist, Is.True, $"Status '{status}' is not found in the table");
            });

            await _logger.ExecuteStepAsync("Validate invitations via partner portal api", async () =>
            {
                var request = ProgressPaginationRequestTestData.Create(status);
                var resp = await _partnerPortalApi.GetInvitationDetailsByAgentAsync(request)
                    .EnsureSuccessContentAsync();
                Assert.That(resp.Invitations, Is.Not.Null.And.Not.Empty, "Invitations list was empty.");
                Assert.That(
                    resp.Invitations.All(invitation => string.Equals(invitation.InviteStatus, status, StringComparison.OrdinalIgnoreCase)),
                    Is.True,
                    $"Expected all invitations to have InviteStatus '{status}'.");
            });
        }

        [Test]
        [Tenant(UNIFY)]
        [RunIn(BoltEnvironment.Staging)]
        [Category("PartnerPortal")]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(252441)]
        [Author(Author.Gil)]
        [Description("Open the D2C link from the Agencytwo admin partner portal home page and quote Pets for an NV address through to rates")]
        public async Task Unify_PartnerPortal_D2CLink_Pets_NV()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAdmin;
            await _adbxHelper.LoginAsync(user, user.LoginUrl);
            var homePage = PageFactory.CreatePage<PartnerPortal_HomePage>();

            var consumerUrl = await _logger.ExecuteStepAsync("Capture the D2C consumer link from the home page and click copy", async () =>
            {
                var url = await _pageHelper!.GetFieldValue(PartnerPortal_FieldNames.ConsumerFlowUrl);
                await homePage.ClickCopyLinkAsync();
                return url;
            });

            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
            var ratesPage = await _logger.ExecuteStepAsync("Run the D2C Pets flow from the consumer link through to rates", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    FlowType.PetsFlow,
                    new Dictionary<string, string>
                    {
                        [OnlineAddress] = "4455 Paradise Rd, Las Vegas, NV 89119",
                        [PetName] = "Toto",
                        [PetBreedType] = "Mixed",
                        [PetBreed] = "Bagel",
                        [FirstName] = "Tamy",
                        [LastName] = "LOPEZ",
                        [Email] = "LOPEZ@epos.com",
                    },
                    fillForms: true,
                    startUrl: consumerUrl);
            });

            await _logger.ExecuteStepAsync("Verify at least one carrier rated", async () =>
            {
                var ratedCarriers = await ratesPage.GetAllRatedCarriers();
                Assert.That(ratedCarriers, Is.Not.Empty, "Expected at least one rated carrier on the Pets rates page, but none were returned.");
            });
        }

        private static async Task VerifyLeadsTableAsync(PartnerPortal_LeadsPage leadsPage, string customerExpected, IPollyRetryService pollyRetryService)
        {
            var isCustomerNameExist = await leadsPage.IsSpecificColumnHaveDataInTableAsync(customerExpected, "Customer Name", pollyRetryService);
            Assert.That(isCustomerNameExist, Is.True, $"Customer Name '{customerExpected}' is not found in the table");
            var isStatusExist = await leadsPage.IsSpecificColumnHaveDataInTableAsync("Referred", "Status", pollyRetryService);
            Assert.That(isStatusExist, Is.True, $"Status 'Referred' is not found in the table");
            var isOriginExist = await leadsPage.IsSpecificColumnHaveDataInTableAsync("Invitation", "Origin", pollyRetryService);
            Assert.That(isOriginExist, Is.True, $"Origin 'Invitation' is not found in the table");
            var isResendVisible = await leadsPage.IsResendInviteVisibleInTableAsync(pollyRetryService);
            Assert.That(isResendVisible, Is.True, "RESEND INVITE button was not found in the table");
        }


    }

}
