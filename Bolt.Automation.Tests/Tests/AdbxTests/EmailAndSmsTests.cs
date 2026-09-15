using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.IntegrationHubApi.Interfaces;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.Models.Twilio;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.Base;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Communications;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.ApplicationTestData;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class EmailAndSmsTests : AdbxUITestBase
    {
        private IAdbxApiClientFactory _adbxApiFactory = null!;
        public IGetQuoteApi? _getQuoteApi;
        public IOutlookClient _outlookClient = null!;
        private IPollyRetryService _pollyRetryService = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
            //_mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _adbxApiFactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();
            _pollyRetryService = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("Sanity")]
        [Category("EmailProposal")]
        [RunIn(includeProduction: true, includeStaging: true)]
        [TestCaseId(236464)]
        [Author(Author.Sandy)]
        [Description("Send mail from interview results page and verify via outlook api")]
        public async Task BOLTAG_Email_Proposal()
        {
            var user = TestContextAccessor.CurrentUserCollection.ConsumerOrganicPL;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                Data = PersonalLineDataProvider.GetPersonalHomeData(AddressData.OH)
            };

            var insured = requestData.Data!.FirstName;
            requestData.Data!.PLFloorNumber = 2;
            requestData.Data!.PL_NumberOfFloors = 2;
            requestData.Data!.UnderMajorRenovation = false;

            await _logger.ExecuteStepAsync("Create and submit application via GetQuote API", async () =>
            {
                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(requestData);

            });
            
            user = TestContextAccessor.CurrentUserCollection.ServiceAgent;

            await AdbxHelper.LoginAndNavigateToQuoteAsync(user,
       ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl, ScopeContext.Data.FriendlyId);

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

        [Test]
        [Tenant(BOLTAG)]
        [Category("EmailSMSTemplates")]
        [TestCaseId(236062)]
        [Author(Author.Sandy)]
        [Description("Send mail from case summary with template")]
        public async Task BOLTAG_Cases_Send_Mail_With_Template()
        {
            await AdbxHelper.LoginAsync(
                    TestContextAccessor.CurrentUserCollection.ServiceManager,
                    ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var caseSummaryPage = await _logger.ExecuteStepAsync("Navigate to Cases tab,select queue and open first case", async () =>
            {
                var casesTabPage = await AdbxHelper.SelectQueueAsync<ADBX_CasesTabPage>(NavigationType.Service, "All");
                await _pageHelper!.SelectTableRowAsync(1);
                var caseSummaryPage = PageFactory.CreatePage<ADBX_CaseSummaryPage>();
                return caseSummaryPage;
            });

            var caseEmailTab = await AdbxHelper.NavigateInnerTabAsync<ADBX_CaseSummarySendEmailTab>(caseSummaryPage, NavigationType.SendEmail);

            await SendEmailWithTemplateAndVerifyAsync(caseEmailTab, 4);
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("EmailSMSTemplates")]
        [TestCaseId(236068)]
        [Author(Author.Sandy)]
        [Description("Send mail from lead summary with template")]
        public async Task BOLTAG_Leads_Send_Mail_With_Template()
        {

            await AdbxHelper.LoginAsync(
                    TestContextAccessor.CurrentUserCollection.ServiceManager,
                    ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadNumber = TestContextAccessor.GetTestSpecificValue("EmailTemplateLeadNumber");
            var leadSummaryPage = await _logger.ExecuteStepAsync($"Global search and open lead = {leadNumber}", async () =>
            {
                return await AdbxHelper.SearchAndOpenSummaryAsync<ADBX_LeadSummaryPage>(leadNumber!);
            }, "Expected result: Lead Summary page is displayed");

            await leadSummaryPage.NavigateCommnicationTab("email");

            await SendEmailWithTemplateAndVerifyAsync(leadSummaryPage, 4);
        }

        private async Task SendEmailWithTemplateAndVerifyAsync(
    ADBX_BasePage summaryPage,
    int attachmentCount)
        {
            var uniqueText = "AutoTest " + RandomManager.GetRandomString(8);
            var emailTemplateName = "AutomationForEmail";

            await _logger.ExecuteStepAsync("Send email using template", async () =>
            {
                await summaryPage.SendEmailWithTemplate(uniqueText, emailTemplateName, true);
                await _pageHelper!.RefreshPageAsync();
            });

            await _logger.ExecuteStepAsync("Verify timeline note existance and email received", async () =>
            {
                const string noteType = "Email";
                var isNoteExists = await summaryPage.HasTimelineNoteAsync(noteType, uniqueText);

                Assert.That(isNoteExists, Is.True,
                    $"Failed. note with subject '{noteType}' is not found");
                var isEmailReceived = await _outlookClient.IsEmailReceived(emailTemplateName, uniqueText, attachmentCount);
                Assert.That(isEmailReceived, Is.True, "Email was not received.");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("EmailSMSTemplates")]
        [TestCaseId(229726)]
        [Author(Author.Sandy)]
        [Description("Sms template UI validations test")]
        public async Task BOLTAG_SMS_Template_Validations()
        {
            await AdbxHelper.LoginAsync(
                    TestContextAccessor.CurrentUserCollection.Admin,
                    ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Navigate to SMS template section", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.ClickOnAdminMenuTab("Admin", "Communications Templates");

                var communicationsEmailTemplatesPage = PageFactory.CreatePage<ADBX_CommunicationsEmailTemplatePage>();
                await AdbxHelper.NavigateInnerTabAsync<ADBX_CommunicationsSmsTemplatePage>(communicationsEmailTemplatesPage, NavigationType.TextMessaging);

                await _pageHelper.InteractWithField(ADBX_FieldNames.NewMessageTemplateButton);
            });

            var newSmsTemplatesPage = PageFactory.CreatePage<ADBX_NewCommunicationsSmsTemplatePage>();

            var validationText = string.Empty;
            var validationColor = string.Empty;
            const string requiredText = "error Required";
            const string requiredColor = "rgb(223, 42, 42)";

            await _logger.ExecuteStepAsync("Triggering validation message and asserting error message and color for TemplateName,LineOfBusiness and UseType fields", async () =>
            {
                var requiredFields = new[]
                {
                    ADBX_FieldNames.TemplateName,
                    ADBX_FieldNames.TemplateLineOfBusiness,
                    ADBX_FieldNames.TemplateUseType,
                    FieldNames.FreeText
                };

                foreach (var fieldName in requiredFields)
                {
                    await _pageHelper!.TriggerValidationAsync(fieldName);
                    validationText = await _pageHelper.GetFieldValidationTextAsync(fieldName);
                    Assert.That(validationText, Is.EqualTo(requiredText));

                    validationColor = await _pageHelper.GetFieldValidationColorAsync(fieldName);
                    Assert.That(validationColor, Is.EqualTo(requiredColor));
                }
            });

            var counterText  = await _logger.ExecuteStepAsync("Preparing error message for template body: Set line of business, fill body, add variable, verify char counter", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateLineOfBusiness);
                var randomLongText = RandomManager.GetRandomString(450);
                await _pageHelper.InteractWithField(FieldNames.FreeText, randomLongText);
                await _pageHelper.InteractWithField(ADBX_FieldNames.VariableAgentEmail);
                var counterText = await newSmsTemplatesPage.Page
                .Locator("label[appformitem]:has(textarea[formcontrolname='templateBody']) + p.ng-star-inserted")
                .InnerTextAsync();

                return counterText;
            });

            await _logger.ExecuteStepAsync("Validating error message and color for template body", async () =>
            {
                Assert.That(counterText.Trim(), Is.EqualTo("462/450"));
                validationText = await _pageHelper.GetFieldValidationTextAsync(FieldNames.FreeText);
                Assert.That(validationText, Is.EqualTo("error Body should not be longer than 450 characters"));

                validationColor = await _pageHelper.GetFieldValidationColorAsync(FieldNames.FreeText);
                Assert.That(validationColor, Is.EqualTo(requiredColor));
            });

            await _logger.ExecuteStepAsync("Verifying create button is disabled", async () =>
            {
                Assert.That(await _pageHelper!.ElementExists(LocatorType.XPath, "//button[contains(text(),'Create')][@disabled]"), Is.True);
            });

        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("EmailSMSTemplates")]
        [TestCaseId(229768)]
        [Author(Author.Sandy)]
        [Description("Create SMS Template")]
        public async Task BOLTAG_Create_SMS_Template()
        {
            await AdbxHelper.LoginAsync(
                    TestContextAccessor.CurrentUserCollection.Admin,
                    ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Navigate to SMS template section and click on new", async () =>
            {

                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.ClickOnAdminMenuTab("Admin", "Communications Templates");

                var communicationsEmailTemplatesPage = PageFactory.CreatePage<ADBX_CommunicationsEmailTemplatePage>();
                await AdbxHelper.NavigateInnerTabAsync<ADBX_CommunicationsSmsTemplatePage>(communicationsEmailTemplatesPage, NavigationType.TextMessaging);

                await _pageHelper.InteractWithField(ADBX_FieldNames.NewMessageTemplateButton);

                PageFactory.CreatePage<ADBX_NewCommunicationsSmsTemplatePage>();
            });

            var templateNameText = "AutomationTest" + RandomManager.GetRandomString(8);
            var randomBodyText = RandomManager.GetRandomString(50);

            await _logger.ExecuteStepAsync("Verify Active toggle exists", async () =>
            {
                Assert.That(await _pageHelper!.ElementExists(ADBX_FieldNames.TemplateActiveToggle), Is.True);
            });

            await _logger.ExecuteStepAsync("Filling SMS template", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateName, templateNameText);
                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateLineOfBusiness,
                  new ElementInteractionOptions
                  {
                      Values = ["Personal", "Commercial"]
                  });
                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateUseType,
                new ElementInteractionOptions
                {
                    Values = ["Case", "Lead"]
                });
                await _pageHelper.InteractWithField(FieldNames.FreeText, randomBodyText);

            });

            await _logger.ExecuteStepAsync("Add groups to template and create", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateAddGroupButton);
                var communicationsAddGroupTemplate = PageFactory.CreatePage<ADBX_CommunicationsTemplateAddGroupPopUp>();

                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateSelectGroup,
                    new ElementInteractionOptions
                    {
                        Values = ["SALES", "SERVICES"]
                    });

                await _pageHelper.InteractWithField(ADBX_FieldNames.TemplateGroupsConfirmButton);
                await _pageHelper.InteractWithField(ADBX_FieldNames.TemplateCreateButton);
            });

            await _logger.ExecuteStepAsync("Search created template", async () =>
            {
                var communicationsSmsTemplatesPage = PageFactory.CreatePage<ADBX_CommunicationsSmsTemplatePage>();
                await communicationsSmsTemplatesPage.SearchRecentRecords(templateNameText);
            });

            await _logger.ExecuteStepAsync("Verify created template values", async () =>
            {
                var actualEnabled = (await _pageHelper!.GetColumnData("Enabled")).FirstOrDefault();
                _logger.Info($"Enabled column actual value: '{actualEnabled}'");
                Assert.That(actualEnabled, Does.Contain("Yes").IgnoreCase, $"Enabled is set to false - Actual: '{actualEnabled}'");

                var expectedBusinessLine = "PL,CL";
                var actualBusinessLine = (await _pageHelper!.GetColumnData("Business Line")).FirstOrDefault();
                _logger.Info($"Business Line column actual value: '{actualBusinessLine}'");
                Assert.That(actualBusinessLine, Does.Contain(expectedBusinessLine).IgnoreCase, $"Business line values not correct - Expected: '{expectedBusinessLine}' Actual: '{actualBusinessLine}'");

                var expectedUseType = "Case, Lead";
                var actualUseType = (await _pageHelper!.GetColumnData("Use Type")).FirstOrDefault();
                _logger.Info($"Use Type column actual value: '{actualUseType}'");
                Assert.That(actualUseType, Does.Contain(expectedUseType).IgnoreCase, $"Use type values not correct - Expected: '{expectedUseType}' Actual: '{actualUseType}'");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("EmailSMSTemplates")]
        [TestCaseId(229728)]
        [Author(Author.Sandy)]
        [Description("Cancel Editing SMS Template")]
        public async Task BOLTAG_Cancel_Editing_SMS_Template()
        {
            await AdbxHelper.LoginAsync(
                    TestContextAccessor.CurrentUserCollection.Admin,
                    ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            await _logger.ExecuteStepAsync("Navigate to SMS template section,search and click on edit", async () =>
            {
                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.ClickOnAdminMenuTab("Admin", "Communications Templates");

                var communicationsEmailTemplatesPage = PageFactory.CreatePage<ADBX_CommunicationsEmailTemplatePage>();

                var communicationsSmsTemplatesPage = await AdbxHelper.NavigateInnerTabAsync<ADBX_CommunicationsSmsTemplatePage>(communicationsEmailTemplatesPage, NavigationType.TextMessaging);
                await communicationsSmsTemplatesPage.SearchRecentRecords("CRMTEST");
                await _pageHelper!.InteractWithField(ADBX_FieldNames.EditButton);
                PageFactory.CreatePage<ADBX_EditCommunicationsSmsTemplatePage>();
            });

            var templateName = "CRMTEST1";

            await _logger.ExecuteStepAsync("Changing template name and click on cancel", async () =>
            {
                await _pageHelper!.InteractWithField(ADBX_FieldNames.TemplateName, templateName);
                await _pageHelper.InteractWithField(ADBX_FieldNames.TemplateCancelButton);

            });

            await _logger.ExecuteStepAsync("Search cancelled template and assert not exists", async () =>
            {
                var communicationsSmsTemplatesPage = PageFactory.CreatePage<ADBX_CommunicationsSmsTemplatePage>();
                await communicationsSmsTemplatesPage.SearchRecentRecords(templateName);
                Assert.That(await _pageHelper!.IsTableDisplayed(), Is.False, "Canceled template is still displayed");

            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("EmailSMSTemplates")]
        [TestCaseId(229730)]
        [Author(Author.Sandy)]
        [Description("Verify SMS template visibility by user group")]
        public async Task BOLTAG_SMS_Template_Visibility_By_User_Group()
        {
            // login with sales user
            await AdbxHelper.LoginAsync(
              TestContextAccessor.CurrentUserCollection.Agent,
              ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadNumber = TestContextAccessor.GetTestSpecificValue("SmsTemplateSalesLeadNumber");

            await _logger.ExecuteStepAsync("Verify SMS template exists for SALES user", async () =>
            {
                var isAvailable = await AdbxHelper.IsSmsTemplateAvailableAsync(leadNumber, "AutomationForSMS");
                Assert.That(isAvailable, Is.True);
            });

            // login with service user
            await AdbxHelper.LoginAsync(
              TestContextAccessor.CurrentUserCollection.ServiceAgent,
              ScopeContext.Data.UrlDataCollection.AdbxApi.LoginUrl);

            leadNumber = TestContextAccessor.GetTestSpecificValue("SmsTemplateServiceLeadNumber");

            await _logger.ExecuteStepAsync("Verify SMS template does not exist for SERVICE user", async () =>
            {
                var isAvailable = await AdbxHelper.IsSmsTemplateAvailableAsync(leadNumber, "AutomationForSMS");
                Assert.That(isAvailable, Is.False);
            });

        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("SMS")]
        [Category("Sanity")]
        [Category("TCPA")]
        [TestCaseId(235639)]
        [Author(Author.Sandy)]
        [Description("E2E SMS: send from Lead page UI, verify note, receive Twilio reply, verify note")]
        public async Task BOLTAG_Lead_SMS_Send_And_Reply()
        {
            await AdbxHelper.LoginAsync(
              TestContextAccessor.CurrentUserCollection.Agent,
              ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var leadNumber = TestContextAccessor.GetTestSpecificValue("SmsTemplateSalesLeadNumber");
            var leadSummaryPage = await _logger.ExecuteStepAsync($"Global search and open lead = {leadNumber}", async () =>
            {
                return await AdbxHelper.SearchAndOpenSummaryAsync<ADBX_LeadSummaryPage>(leadNumber!);
            }, "Expected result: Lead Summary page is displayed");

            var sentContent = "AutoTest " + RandomManager.GetRandomString(8);
            var replyContent = "AutoTest " + RandomManager.GetRandomString(8);

            await _logger.ExecuteStepAsync("Send SMS from UI", async () =>
            {
                await leadSummaryPage.NavigateCommnicationTab("sms");
                await leadSummaryPage.SendSms(sentContent);               
            });

            await _logger.ExecuteStepAsync("Verify sent SMS note appears in timeline", async () =>
            {
                var isNoteExists = await leadSummaryPage.HasTimelineNoteAsync("SMS", sentContent);
                Assert.That(isNoteExists, Is.True, $"Sent SMS timeline note with content '{sentContent}' was not found");
            });

            if (ScopeContext.Data.Environment == Common.Environment.Uat)
            {
                await _logger.ExecuteStepAsync("Verify SMS delivery status shows delivered", async () =>
                {
                    var isDelivered = await leadSummaryPage.HasSmsDeliveredStatusAsync(sentContent, _pollyRetryService);
                    Assert.That(isDelivered, Is.True, $"SMS delivered status icon not found for content '{sentContent}'");
                });
            }

            var twilio = TestContextAccessor.CurrentTwilioData!;
            ScopeContext.Set(ctx => ctx.TwilioData, twilio);

            var fromPhone = TestContextAccessor.GetTestSpecificValue("SmsFromPhoneNumber");

            await _logger.ExecuteStepAsync("Send SMS reply via Twilio webhook and verify reply note", async () =>
            {
                var api = _uiTestScope.ServiceProvider.GetRequiredService<ITwilioWebhookApi>();
                var fields = twilio.BuildSmsReplyPayload(replyContent, SmsContext.Lead, fromPhone);
                var response = await api.SendMessageReplyAsync(nameof(BOLTAG), fields);
                response.EnsureSuccessStatusCode();
                await _pageHelper!.RefreshPageAsync();

                var isReplyNoteExists = await leadSummaryPage.HasTimelineNoteAsync("SMS", replyContent);
                Assert.That(isReplyNoteExists, Is.True, $"Received SMS reply note with content '{replyContent}' was not found");
            });
        }

        [Test]
        [Tenant(BOLTAG)]
        [Category("SMS")]
        [Category("Sanity")]
        [Category("TCPA")]
        [TestCaseId(235706)]
        [Author(Author.Sandy)]
        [Description("E2E SMS: send from Case page UI, verify note, receive Twilio reply, verify note")]
        public async Task BOLTAG_CaseService_SMS_Send_And_Reply()
        {
            await AdbxHelper.LoginAsync(
              TestContextAccessor.CurrentUserCollection.ServiceManager,
              ScopeContext.Data.UrlDataCollection.FrontEnd.LoginUrl);

            var serviceCaseNumber = TestContextAccessor.GetTestSpecificValue("SmsServiceCaseNumber");
            var caseSummaryPage = await _logger.ExecuteStepAsync($"Global search and open service case = {serviceCaseNumber}", async () =>
            {
                return await AdbxHelper.SearchAndOpenSummaryAsync<ADBX_CaseSummaryPage>(serviceCaseNumber!);
            }, "Expected result: Case Summary page is displayed");

            var sentContent = "AutoTest " + RandomManager.GetRandomString(8);
            var replyContent = "AutoTest " + RandomManager.GetRandomString(8);

            var caseSmsTab = await _logger.ExecuteStepAsync("Navigate to Send SMS tab and send SMS", async () =>
            {
                var smsTab = await AdbxHelper.NavigateInnerTabAsync<ADBX_CaseSummarySendSmsTab>(caseSummaryPage, NavigationType.SendSms);
                await smsTab.SendSms(sentContent);
                return smsTab;
            });

            await _logger.ExecuteStepAsync("Verify sent SMS note appears in timeline", async () =>
            {
                var isNoteExists = await caseSmsTab.HasTimelineNoteAsync("SMS", sentContent);
                Assert.That(isNoteExists, Is.True, $"Sent SMS timeline note with content '{sentContent}' was not found");
            });

            if (ScopeContext.Data.Environment == Common.Environment.Uat)
            {
                await _logger.ExecuteStepAsync("Verify SMS delivery status shows delivered", async () =>
                {
                    var isDelivered = await caseSmsTab.HasSmsDeliveredStatusAsync(sentContent, _pollyRetryService);
                    Assert.That(isDelivered, Is.True, $"SMS delivered status icon not found for content '{sentContent}'");
                });
            }

            var twilio = TestContextAccessor.CurrentTwilioData!;
            ScopeContext.Set(ctx => ctx.TwilioData, twilio);

            var fromPhone = TestContextAccessor.GetTestSpecificValue("SmsFromPhoneNumber");

            await _logger.ExecuteStepAsync("Send SMS reply via Twilio webhook and verify reply note", async () =>
            {
                var api = _uiTestScope.ServiceProvider.GetRequiredService<ITwilioWebhookApi>();
                var fields = twilio.BuildSmsReplyPayload(replyContent, SmsContext.Case, fromPhone);
                var response = await api.SendMessageReplyAsync(nameof(BOLTAG), fields);
                response.EnsureSuccessStatusCode();
                await _pageHelper!.RefreshPageAsync();

                var isReplyNoteExists = await caseSmsTab.HasTimelineNoteAsync("SMS", replyContent);
                Assert.That(isReplyNoteExists, Is.True, $"Received SMS reply note with content '{replyContent}' was not found");
            });
        }
    }
}
