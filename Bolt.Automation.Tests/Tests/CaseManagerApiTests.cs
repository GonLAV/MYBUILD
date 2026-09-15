using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.AdbxApi.Entities;
using Bolt.Automation.ApiClients.AdbxApi.Entities.Case.SaleCaseMessage;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.CreateConsumer;
using Bolt.Automation.ApiClients.CaseManagerApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.ExternalServices.CasePortal.Infrastructure;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.InternalServices.Extensions;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests
{
    public class CaseManagerApiTests : TestBase
    {
        private ICaseManagerApi? _caseManagerApi;
        public IGetQuoteApi _getQuoteApi = null!;
        private IAdbxApiClientFactory _adbxApifactory = null!;
        private IMainQueries? _mainQueries;
        private ICasePortalApiClientFactory _casePortalApiClientFactory = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
            _caseManagerApi = refitApiLocator.GetService<ICaseManagerApi>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _adbxApifactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
            _casePortalApiClientFactory = _testScope.ServiceProvider.GetRequiredService<ICasePortalApiClientFactory>();
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(197428)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a Personal Line consumer via CaseManager API and verifies it syncs to ADBX")]
        public async Task KraftLake_CreateConsumerPL()
        {
            var (lspUser, underwriter) = await _logger.ExecuteStepAsync("Set up test users", async () =>
            {
                var lsp = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return (lsp, uw);
            });

            var resp = await _logger.ExecuteStepAsync("Create consumer for KraftLake with Personal Line", async () =>
            {
                var createConsumerReq = CreateConsumerDataProvider.CreateConsumerData
                    ($"{lspUser.GroupExternalId}|{lspUser.UserExternalId}");

                var result = await _caseManagerApi.CreateConsumerAsync(nameof(KRAFTLAKEX), createConsumerReq)
                    .EnsureSuccessContentAsync("Failed to create consumer PL");
                return result;
            });

            var response = await _logger.ExecuteStepAsync("Verify consumer created in ADBX", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var result = await api.GetAccount(resp.ConsumerExternalId);
                return result;
            });

            await _logger.ExecuteStepAsync("Validate account response", async () =>
            {
                Assert.That(response.Content, Is.Not.Null);
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(197434)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a Commercial Line consumer via CaseManager API and verifies it syncs to ADBX")]
        public async Task KraftLake_CreateConsumerCL()
        {
            var (lspUser, underwriter) = await _logger.ExecuteStepAsync("Set up test users", async () =>
            {
                var lsp = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return (lsp, uw);
            });

            var resp = await _logger.ExecuteStepAsync("Create consumer for KraftLake with Commercial Line", async () =>
            {
                var createConsumerReq = CreateConsumerDataProvider.CreateConsumerData
                    ($"{lspUser.GroupExternalId}|{lspUser.UserExternalId}", ConsumerLine.Commercial);

                var result = await _caseManagerApi.CreateConsumerAsync(nameof(KRAFTLAKEX), createConsumerReq)
                    .EnsureSuccessContentAsync("Failed to create consumer CL");
                return result;
            });

            var response = await _logger.ExecuteStepAsync("Verify consumer created in ADBX", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var result = await api.GetAccount(resp.ConsumerExternalId);
                return result;
            });

            await _logger.ExecuteStepAsync("Validate account response", async () =>
            {
                Assert.That(response.Content, Is.Not.Null);
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(199444)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Updates a Commercial Line consumer via CaseManager API and verifies the email update in ADBX")]
        public async Task KraftLake_UpdateConsumerCL()
        {
            var (lspUser, underwriter) = await _logger.ExecuteStepAsync("Set up test users", async () =>
            {
                var lsp = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return (lsp, uw);
            });

            var updateConsumerReq = await _logger.ExecuteStepAsync("Update consumer for KraftLake with Commercial Line", async () =>
            {
                var consumerExternalId = TestContextAccessor.GetTestSpecificValue("CLConsumerExternalId");
                var updateReq = UpdateConsumerDataProvider.UpdateConsumerData
                    ($"{lspUser.GroupExternalId}|{lspUser.UserExternalId}", consumerExternalId);

                await _caseManagerApi.UpdateConsumerAsync(nameof(KRAFTLAKEX), updateReq)
                    .EnsureSuccessContentAsync("Failed to create consumer CL");
                return updateReq;
            });

            var response = await _logger.ExecuteStepAsync("Verify consumer updated in ADBX", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var consumerExternalId = TestContextAccessor.GetTestSpecificValue("CLConsumerExternalId");
                var result = await api.GetAccount(consumerExternalId);
                return result;
            });

            await _logger.ExecuteStepAsync("Validate account response and email", async () =>
            {
                Assert.That(response.Content, Is.Not.Null);
                Assert.That(response.Content?.Email, Is.EqualTo(updateConsumerReq.Email).Using(StringComparer.OrdinalIgnoreCase));
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198537)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a Commercial Line policy binder via CaseManager API and verifies it appears in ADBX lead policies")]
        public async Task KraftLake_CreatePolicyBinderCL()
        {
            var underwriter = await _logger.ExecuteStepAsync("Set casemanager user", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            });

            var (createPolicyReq, policyExternalId) = await _logger.ExecuteStepAsync("Create policy binder CL for KraftLake", async () =>
            {
                var quoteExternalId = TestContextAccessor.GetTestSpecificValue("CLQuoteExternalId");
                var policyReq = CreatePolicyBinderDataProvider.CreatePolicyBinderData(quoteExternalId, "BOP");

                var resp = await _caseManagerApi.CreatePolicyAsync(nameof(KRAFTLAKEX), policyReq)
                    .EnsureSuccessContentAsync("Failed to create policy ");

                var policyExtId = resp.PolicyExternalId ?? throw new InvalidOperationException("Policy not found");
                return (policyReq, policyExtId);
            });

            var (leadPolicies, latestQuotes) = await _logger.ExecuteStepAsync("Verify policy and quotes in ADBX", async () =>
            {
                var leadId = TestContextAccessor.GetTestSpecificValue("CLLeadId");
                var lspUser = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);

                var api = await _adbxApifactory.CreateApiClientAsync();
                var policies = await api.GetLeadPolicies(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead policies ");
                var quotes = await api.GetLeadQuotes(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead quotes ", true);
                return (policies, quotes);
            });

            await _logger.ExecuteStepAsync("Validate policy exists and no quotes on lead", async () =>
            {
                Assert.That(leadPolicies?.Any(x => x.PolicyNumber?.Equals(createPolicyReq.PolicyNumber) == true), Is.True,
                    $"Policy number= {createPolicyReq.PolicyNumber} is not found in lead policies.");
                Assert.That(latestQuotes.IsNullOrEmpty(), Is.True, "there are latest quotes seen on lead quotes.");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(197386)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a Personal Line sale case via CaseManager API and verifies communications appear in ADBX")]
        public async Task KraftLake_CreateSaleCasePL()
        {
            var (lspUser, underwriter) = await _logger.ExecuteStepAsync("Set up test users", async () =>
            {
                var lsp = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return (lsp, uw);
            });

            var (createCaseReq, resp) = await _logger.ExecuteStepAsync("Create sale case PL for KraftLake", async () =>
            {
                var accountExternalId = TestContextAccessor.GetTestSpecificValue("PLAccountExternalId");
                var cmCaseId = TestContextAccessor.GetTestSpecificValue("PLCmCaseId");
                var caseReq = CaseDataProvider.CreateCaseData($"{lspUser.GroupExternalId}|{lspUser.UserExternalId}", "HOME", cmCaseId, accountExternalId);

                var response = await _caseManagerApi.CreateCaseAsync(nameof(KRAFTLAKEX), caseReq)
                    .EnsureSuccessContentAsync("Failed to create sale case ");
                return (caseReq, response);
            });

            var leadId = await _logger.ExecuteStepAsync("Get lead ID from database", async () =>
            {
                var id = await _mainQueries.Policy.GetLeadIdByExternalIdAsync(resp.QuoteExternalId);
                return id;
            });

            var (leadCommunications, latestQuotes) = await _logger.ExecuteStepAsync("Get lead communications and quotes from ADBX", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var communications = await api.GetLeadCommunications(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead policies ");
                var quotes = await api.GetLeadQuotes(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead quotes ", true);
                return (communications, quotes);
            });

            await _logger.ExecuteStepAsync("Validate communications exist and no quotes on lead", async () =>
            {
                Assert.That(leadCommunications.CommunicationsList?.Count > 0, Is.True,
                    $"No communications found.");
                Assert.That(latestQuotes.IsNullOrEmpty(), Is.True, "there are latest quotes seen on lead quotes.");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(197439)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a Commercial Line sale case via CaseManager API and verifies communications appear in ADBX")]
        public async Task KraftLake_CreateSaleCaseCL()
        {
            var (lspUser, underwriter) = await _logger.ExecuteStepAsync("Set up test users", async () =>
            {
                var lsp = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return (lsp, uw);
            });

            var (createCaseReq, resp) = await _logger.ExecuteStepAsync("Create sale case CL for KraftLake", async () =>
            {
                var accountExternalId = TestContextAccessor.GetTestSpecificValue("CLAccountExternalId");
                var cmCaseId = TestContextAccessor.GetTestSpecificValue("CLCmCaseId");
                var caseReq = CaseDataProvider.CreateCaseData($"{lspUser.GroupExternalId}|{lspUser.UserExternalId}", "BOP", cmCaseId, accountExternalId);

                var response = await _caseManagerApi.CreateCaseAsync(nameof(KRAFTLAKEX), caseReq)
                    .EnsureSuccessContentAsync("Failed to create sale case ");
                return (caseReq, response);
            });

            var leadId = await _logger.ExecuteStepAsync("Get lead ID from database", async () =>
            {
                var id = await _mainQueries.Policy.GetLeadIdByExternalIdAsync(resp.QuoteExternalId);
                return id;
            });

            var (leadCommunications, latestQuotes) = await _logger.ExecuteStepAsync("Get lead communications and quotes from ADBX", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var communications = await api.GetLeadCommunications(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead policies ");
                var quotes = await api.GetLeadQuotes(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead quotes ", true);
                return (communications, quotes);
            });

            await _logger.ExecuteStepAsync("Validate communications exist and no quotes on lead", async () =>
            {
                Assert.That(leadCommunications.CommunicationsList?.Count > 0, Is.True,
                    $"No communications found.");
                Assert.That(latestQuotes.IsNullOrEmpty(), Is.True, "there are latest quotes seen on lead quotes.");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198984)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Sends a sale case message from CaseManager to ADBX and verifies it appears in lead communications")]
        public async Task KraftLake_SaleCase_Message_CM_ABDX()
        {
            var underwriter = await _logger.ExecuteStepAsync("Set casemanager user", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            });

            var createCaseMessageReq = await _logger.ExecuteStepAsync("Send sale case message from CM to ADBX", async () =>
            {
                var caseExternalId = TestContextAccessor.GetTestSpecificValue("MessageCaseExternalId");
                var messageReq = CreateCaseMessageDataProvider.CreateCaseMessageData(ScopeContext);

                var resp = await _caseManagerApi.CreateCaseMessageAsync(nameof(KRAFTLAKEX), caseExternalId, messageReq)
                    .EnsureSuccessContentAsync("Failed to create sale case message");
                return messageReq;
            });

            var leadCommunications = await _logger.ExecuteStepAsync("Get lead communications from ADBX", async () =>
            {
                var leadId = TestContextAccessor.GetTestSpecificValue("MessageLeadId");
                var lspUser = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);

                var api = await _adbxApifactory.CreateApiClientAsync();
                var communications = await api.GetLeadCommunications(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead policies ");
                return communications;
            });

            await _logger.ExecuteStepAsync("Validate matching message found", async () =>
            {
                Assert.That(leadCommunications.CommunicationsList?.Any(x => x.DateCreated.ToString("HH:mm:ss").Equals(createCaseMessageReq.DateCreated.ToString("HH:mm:ss"))), Is.True,
                    $"No matching message for DateCreated :{createCaseMessageReq.DateCreated}");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198986)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Sends a sale case message from ADBX to CaseManager and verifies it appears in portal service API")]
        public async Task KraftLake_SaleCase_Message_ADBX_CM()
        {
            var caseMessageReq = await _logger.ExecuteStepAsync("Send sale case message from ADBX to CM", async () =>
            {
                var lspUser = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);

                var api = await _adbxApifactory.CreateApiClientAsync();
                var caseId = TestContextAccessor.GetTestSpecificValue("MessageCaseId");

                var messageReq = new SendCaseMessageToUwrModel()
                {
                    Description = MessageData.MessageNoteData.Description,
                    Subject = MessageData.MessageNoteData.Subject
                };

                var resp = await api.SendMessageToUwr(caseId, messageReq)
                    .EnsureSuccessContentAsync();
                return messageReq;
            });

            var messages = await _logger.ExecuteStepAsync("Get messages from portal service API (case manager)", async () =>
            {
                var casePortalUser = TestContextAccessor.CurrentUserCollection.CasePortalUser;
                ScopeContext.Set(ctx => ctx.CurrentUser, casePortalUser);

                var caseExternalId = TestContextAccessor.GetTestSpecificValue("MessageCaseExternalId");
                var portalApi = await _casePortalApiClientFactory.CreateApiClient();
                var result = await portalApi.GetAllMessagesByCaseIdAsync(nameof(KRAFTLAKEX), caseExternalId, casePortalUser.UserExternalId)
                    .EnsureSuccessContentAsync();

                return result?.ObjectProcessed?.Messages;
            });

            await _logger.ExecuteStepAsync("Validate messages match description and subject", async () =>
            {
                Assert.That(messages?.Any(x => x?.Note?.Text == caseMessageReq.Description), Is.True,
                    "No matching message found for the given description");
                Assert.That(messages?.Any(x => x?.Note?.Subject != null && x.Note.Subject.Contains(caseMessageReq.Subject)), Is.True,
                    "No matching message found for the given subject");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198766)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Sends a service case message from CaseManager to ADBX and verifies it appears in case notes")]
        public async Task KraftLake_ServiceCase_Message_CM_ABDX()
        {
            var underwriter = await _logger.ExecuteStepAsync("Set casemanager user", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            });

            var createCaseMessageReq = await _logger.ExecuteStepAsync("Send service case message from CM to ADBX", async () =>
            {
                var caseExternalId = TestContextAccessor.GetTestSpecificValue("MessageServiceCaseExternalId");
                var messageReq = CreateCaseMessageDataProvider.CreateCaseMessageData(ScopeContext);

                var resp = await _caseManagerApi.CreateCaseMessageAsync(nameof(KRAFTLAKEX), caseExternalId, messageReq)
                    .EnsureSuccessContentAsync("Failed to create service case message");
                return messageReq;
            });

            var caseNotes = await _logger.ExecuteStepAsync("Get case notes from ADBX", async () =>
            {
                //var lspUser = TestContextAccessor.CurrentUserCollection.LSP1;
                var lspUser = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);

                var caseId = TestContextAccessor.GetTestSpecificValue("MessageServiceCaseId");
                var api = await _adbxApifactory.CreateApiClientAsync();

                var notes = await api.GetCaseNotes(caseId, PaginationQuery.Default)
                    .EnsureSuccessContentAsync("Failed to get case notes");
                return notes;
            });

            await _logger.ExecuteStepAsync("Validate matching message found", async () =>
            {
                Assert.That(caseNotes?.Notes.Any(x => x.DateCreated.ToString("HH:mm:ss").Equals(createCaseMessageReq.DateCreated.ToString("HH:mm:ss"))), Is.True,
                    $"No matching message for DateCreated :{createCaseMessageReq.DateCreated}");
            });
        }

        [Test]
        [Tenant(KRAFTLAKEX)]
        [TestCaseId(198612)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a service case for KraftLake via CaseManager API and verifies it syncs to ADBX")]
        public async Task KraftLake_Create_ServiceCase()
        {
            var (lspUser, underwriter) = await _logger.ExecuteStepAsync("Set up test users", async () =>
            {
                var lsp = Environment == Common.Environment.Uat
                    ? TestContextAccessor.CurrentUserCollection.Agent
                    : TestContextAccessor.CurrentUserCollection.LSP1;
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return (lsp, uw);
            });

            var (createCaseReq, resp) = await _logger.ExecuteStepAsync("Create service case for KraftLake", async () =>
            {
                var policyExternalId = TestContextAccessor.GetTestSpecificValue("PolicyExternalId");
                var caseReq = CaseDataProvider.CreateServiceCaseData($"{lspUser.GroupExternalId}|{lspUser.UserExternalId}", policyExternalId);

                var response = await _caseManagerApi.CreateCaseAsync(nameof(KRAFTLAKEX), caseReq)
                    .EnsureSuccessContentAsync();
                return (caseReq, response);
            });

            var caseId = await _logger.ExecuteStepAsync("Get case ID from database", async () =>
            {
                var id = await _mainQueries.CaseLogic.GetCaseIdByExternalIdAsync(resp.CaseExternalId);
                return id;
            });

            var caseResp = await _logger.ExecuteStepAsync("Verify service case in ADBX", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, lspUser);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var result = await api.GetCase(caseId).EnsureSuccessContentAsync();
                return result;
            });

            await _logger.ExecuteStepAsync("Validate case number matches", async () =>
            {
                Assert.That(caseResp.CaseNumber == createCaseReq.CmCaseId, Is.True, $"Service case not found with the cmcase id {createCaseReq.CmCaseId}");
            });
        }


        [Test]
        [Tenant(BOLTACCESS)]
        [TestCaseId(198640)]
        [Category("CaseManagerApi")]
        [Author(Author.Sandy)]
        [Description("Creates a service case for BoltAccess via CaseManager API and verifies it syncs to ADBX")]
        public async Task BoltAccess_Create_ServiceCase()
        {
            var underwriter = await _logger.ExecuteStepAsync("Set casemanager user", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            });

            var (createCaseReq, resp) = await _logger.ExecuteStepAsync("Create service case for BoltAccess", async () =>
            {
                var policyExternalId = TestContextAccessor.GetTestSpecificValue("PolicyExternalId");
                var caseReq = CaseDataProvider.CreateServiceCaseData(string.Empty, policyExternalId);

                var response = await _caseManagerApi.CreateCaseAsync(nameof(BOLTACCESS), caseReq)
                    .EnsureSuccessContentAsync();
                return (caseReq, response);
            });

            //get case Id from database
            _logger.Info($"Case created with ExternalId: {resp.CaseExternalId}");
            var caseId = await _logger.ExecuteStepAsync("Get case ID from database", async () =>
            {
                var id = await _mainQueries.CaseLogic.GetCaseIdByExternalIdAsync(resp.CaseExternalId);
                _logger.Info($"Case Id from database: {id}");
                return id;
            });

            var caseResp = await _logger.ExecuteStepAsync("Verify service case in ADBX", async () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.Admin;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);
                var api = await _adbxApifactory.CreateApiClientAsync();
                var result = await api.GetCase(caseId).EnsureSuccessContentAsync();
                return result;
            });

            await _logger.ExecuteStepAsync("Validate case number matches", async () =>
            {
                Assert.That(caseResp.CaseNumber == createCaseReq.CmCaseId, Is.True, $"Service case not found with the cmcase id {createCaseReq.CmCaseId}");
            });
        }

        [Test]
        [Tenant(UNIFY)]
        [TestCaseId(165369)]
        [Category("CaseManagerApi")]
        [Author(Author.Andrii)]
        [Description("Creates a Personal Line policy binder via CaseManager API and verifies it appears in ADBX lead and quote policies")]
        public async Task Unify_CreatePolicyBinderPL()
        {
            var underwriter = await _logger.ExecuteStepAsync("Set current user to Underwriter", async () =>
            {
                var uw = TestContextAccessor.CurrentUserCollection.Underwriter;
                ScopeContext.Set(ctx => ctx.CurrentUser, uw);
                return uw;
            }, "Expected result: Underwriter user is set as the current user for the CaseManager API call.");

            var createPolicyReq = await _logger.ExecuteStepAsync("Create Personal Line policy binder via CaseManager API", async () =>
            {
                var quoteExternalId = TestContextAccessor.GetTestSpecificValue("PLQuoteExternalId");
                var policyReq = CreatePolicyBinderDataProvider.CreatePolicyBinderData(quoteExternalId, "AUTOP");

                var resp = await _caseManagerApi.CreatePolicyAsync(nameof(UNIFY), policyReq)
                    .EnsureSuccessContentAsync("Failed to create policy ");

                _ = resp.PolicyExternalId ?? throw new InvalidOperationException("Policy not found");
                return policyReq;
            }, "Expected result: CaseManager API returns a successful response with a policy external id.");

            var (leadPolicies, leadTimeline, quotePolicies, quoteTimeline) = await _logger.ExecuteStepAsync("Get lead and quote policies and timelines from ADBX", async () =>
            {
                var leadId = TestContextAccessor.GetTestSpecificValue("PLLeadId");
                var quoteId = TestContextAccessor.GetTestSpecificValue("PLQuoteId");
                var agentUser = TestContextAccessor.CurrentUserCollection.TestAgent;
                ScopeContext.Set(ctx => ctx.CurrentUser, agentUser);

                var api = await _adbxApifactory.CreateApiClientAsync();
                var leadPolicies = await api.GetLeadPolicies(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead policies ");
                var leadTimeline = await api.GetLeadTimeline(leadId)
                    .EnsureSuccessContentAsync("Failed to get lead timeline ");

                var quotePolicies = await api.GetQuotePolicies(quoteId)
                    .EnsureSuccessContentAsync("Failed to get quote policies ");
                var quoteTimeline = await api.GetQuoteTimeline(quoteId)
                    .EnsureSuccessContentAsync("Failed to get quote timeline ");

                return (leadPolicies, leadTimeline, quotePolicies, quoteTimeline);
            }, "Expected result: ADBX returns lead policies, lead timeline, quote policies and quote timeline for the synced policy.");

            await _logger.ExecuteStepAsync("Validate policy exists in lead and quote policies", () =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(leadPolicies?.Any(x => x.PolicyNumber?.Equals(createPolicyReq.PolicyNumber) == true), Is.True,
                        $"Policy number= {createPolicyReq.PolicyNumber} is not found in lead policies.");
                    Assert.That(quotePolicies?.Any(x => x.PolicyNumber?.Equals(createPolicyReq.PolicyNumber) == true), Is.True,
                        $"Policy number= {createPolicyReq.PolicyNumber} is not found in quote policies.");
                });
                return Task.CompletedTask;
            }, $"Expected result: policy number {createPolicyReq.PolicyNumber} is present in both the ADBX lead policies and quote policies lists.");

            await _logger.ExecuteStepAsync("Validate Policy Ordered event in lead and quote timeline", () =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(leadTimeline?.Events?.Any(x => x.Description?.Contains(createPolicyReq.PolicyNumber) == true
                            && x.CreatorDisplayName == "Underwriting Team"), Is.True,
                        $"Policy Ordered event for policy {createPolicyReq.PolicyNumber} by Underwriting Team not found in lead timeline.");
                    Assert.That(quoteTimeline?.Events?.Any(x => x.Description?.Contains(createPolicyReq.PolicyNumber) == true
                            && x.CreatorDisplayName == "Underwriting Team"), Is.True,
                        $"Policy Ordered event for policy {createPolicyReq.PolicyNumber} by Underwriting Team not found in quote timeline.");
                });
                return Task.CompletedTask;
            }, $"Expected result: lead timeline and quote timeline each contain a Policy Ordered event whose description references policy {createPolicyReq.PolicyNumber} and whose creator display name is \"Underwriting Team\".");
        }
    }
}
