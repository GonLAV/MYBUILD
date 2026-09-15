// using Bolt.Automation.ApiClients.GetQuoteApi.Extensions;
// using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
// using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
// using Bolt.Automation.ApiClients.Infrastructure;
// using Bolt.Automation.Common;
// using Bolt.Automation.Common.Enums;
// using Bolt.Automation.Common.Logging.Extentions;
// using Bolt.Automation.Common.Models.TestData.Data;
// using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
// using Bolt.Automation.Common.Utils;
// using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
// using Bolt.Automation.InternalServices.Database.Entities.Main;
// using Bolt.Automation.InternalServices.Database.Queries.Main;
// using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
// using Bolt.Automation.Tests.TestExtension.Attributes;
// using Bolt.Automation.Tests.TestExtension.Base;
// using Microsoft.Extensions.DependencyInjection;
// using NUnit.Framework;
// using static Bolt.Automation.Common.Tenant;
// using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
// using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

// namespace Bolt.Automation.Tests.Tests.Progressive
// {
// H.L 25/06 - Pending Product decision about GQ prefill behavior. Test is working.
//     public class PgrGqPrefillPolicyAttachmentsTests : UITestBase
//     {
//         private IGetQuoteApi _getQuoteApi = null!;
//         private IMainQueries? _mainQueries;

//         public PgrGqPrefillPolicyAttachmentsTests() : base()
//         {
//             ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
//         }

//         protected override void ResolveServices()
//         {
//             var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
//             _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
//             if (Environment != Common.Environment.Production)
//                 _mainQueries = _testScope.ServiceProvider.GetService<IMainQueries>();
//         }

//         // Address that returns NoHit from E2Value vendor but is served by Verisk
//         private static readonly Address NoHitE2ValueAddress = new()
//         {
//             AddressLine1 = "1809 Paradise Rd",
//             AddressLine2 = "3",
//             ZipCode = "85358",
//             City = "Modesto",
//             State = "CA"
//         };

//         [Test]
//         [RunIn]
//         [Tenant(PROGRESSIVEPL)]
//         [Category("GetQuoteApi")]
//         [Category("Prefill")]
//         [TestCaseId(245402)]
//         [Author(Author.Helen)]
//         [Description("Verifies policyAttachments behavior for DMPHQX source when E2Value returns NoHit: " +
//                      "after create only attachmentType 15 exists; after navigating to 3PQ, attachmentType 24 is not created " +
//                      "and attachmentType 9 has Verisk data only; after PATCH both vendors appear in type 9 and previous record is IsActive=0")]
//         public async Task PGR_GQ_Prefill_PolicyAttachments_DMPHQX_NoHit_From_E2Value()
//         {
//             // DMPHQX is a consumer source — SendAgentIdentity defaults to false, no X-Agent-Identity header is sent
//             ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.DMPHQX);

//             string externalId = string.Empty;

//             // ── Local helpers ─────────────────────────────────────────────────────
//             async Task AssertAttachmentType24AbsentAsync() =>
//                 await _logger.ExecuteStepAsync("Check policyAttachments - attachmentType 24 should NOT be created", async () =>
//                 {
//                     var attachments = await _mainQueries!.Policy.GetPolicyAttachmentsByExternalIdAndTypeAsync(externalId, 24);
//                     _logger.Info($"AttachmentType 24 records found: {attachments.Count}");
//                     Assert.That(attachments, Is.Empty, "AttachmentType 24 should not be found (DMPHQX NoHit from E2Value)");
//                 }, "Expected: AttachmentType 24 cannot be found");

//             async Task<List<PolicyAttachment>> GetAndLogAttachmentType9Async()
//             {
//                 var attachments = await _mainQueries!.Policy.GetPolicyAttachmentsByExternalIdAndTypeAsync(externalId, 9);
//                 foreach (var a in attachments)
//                     _logger.Info($"AttachmentType 9 — IsActive: {a.IsActive}, DateCreated: {a.Datecreated}");
//                 return attachments;
//             }
//             // ─────────────────────────────────────────────────────────────────────

//             await _logger.ExecuteStepAsync("Create a new application in DMPHQX source with NoHit address", async () =>
//             {
//                 var request = new ApplicationRequestModel<PersonalLineData>
//                 {
//                     Products = Products.Homeowners,
//                     Data = PersonalLineDataProvider.GetPersonalHomeData(NoHitE2ValueAddress)
//                 };

//                 var createResponse = await _getQuoteApi.CreateApplicationAsync(request).EnsureSuccessContentAsync();
//                 createResponse.MapIdentifiers(ScopeContext);
//                 externalId = createResponse.Id!;
//                 _logger.Info($"Application created. ExternalId: {externalId}, FriendlyId: {createResponse.FriendlyId}");
//             }, "Expected: Application is created");

//             await _logger.ExecuteStepAsync("Verify no attachments except type 15 exist immediately after create", async () =>
//             {
//                 var allAttachments = await _mainQueries!.Policy.GetPolicyAttachmentsByExternalIdAsync(externalId);
//                 _logger.Info($"Attachments after create: {string.Join(", ", allAttachments.Select(a => $"type={a.AttachmentType}"))}");

//                 Assert.That(allAttachments.All(a => a.AttachmentType == 15), Is.True,
//                     $"Only attachmentType 15 should exist after create. Found: {string.Join(", ", allAttachments.Select(a => a.AttachmentType))}");
//             }, "Expected: Only attachmentType 15 exists immediately after application creation");

//             await _logger.ExecuteStepAsync("Navigate to HQX Consumer interview via questionnaire URL — lands on 3PQ", async () =>
//             {
//                 var questionnaireResponse = await _getQuoteApi.GetQuestionnaireAsync(externalId).EnsureSuccessContentAsync();
//                 var interviewUrl = questionnaireResponse.Url
//                     ?? throw new InvalidOperationException("Questionnaire URL is null — cannot navigate to interview");
//                 _logger.Info($"Questionnaire URL: {interviewUrl}");

//                 await BrowserManager.NavigateAsync(interviewUrl);
//                 PageFactory.CreatePage<HQXConusmer_3PQ>(); // validates page identity (URL contains 'threeprefillquestions')
//             }, "Expected: Browser lands on 3PQ page");

//             // Attachment checks BEFORE filling 3PQ — navigation triggered prefill vendor calls
//             await AssertAttachmentType24AbsentAsync();

//             await _logger.ExecuteStepAsync("Check attachmentType 9 - should exist with quoteMappedValues from Verisk only", async () =>
//             {
//                 var attachments = await GetAndLogAttachmentType9Async();
//                 Assert.That(attachments, Is.Not.Empty, "AttachmentType 9 should exist after navigation to 3PQ");

//                 Assert.Multiple(() =>
//                 {
//                     Assert.That(attachments[0].Content, Does.Contain("Verisk"),
//                         "AttachmentType 9 content should contain Verisk quoteMappedValues");
//                     Assert.That(attachments[0].Content, Does.Not.Contain("E2Value"),
//                         "AttachmentType 9 content should NOT contain E2Value when E2Value returns NoHit");
//                 });
//             }, "Expected: AttachmentType 9 exists with quoteMappedValues only from Verisk vendor");

//             await _logger.ExecuteStepAsync("PATCH the application — update firstName to trigger re-prefill from both vendors", async () =>
//             {
//                 var patchRequest = new ApplicationRequestModel<PersonalLineData>
//                 {
//                     Data = new PersonalLineData { FirstName = "AaNexus" + RandomManager.GetRandomString(6) }
//                 };
//                 await _getQuoteApi.PatchApplicationAsync(externalId, patchRequest).EnsureSuccessContentAsync();
//             }, "Expected: Application's data has been updated");

//             await AssertAttachmentType24AbsentAsync();

//             await _logger.ExecuteStepAsync("Re-check attachmentType 9 — latest has both vendors; previous record IsActive=0", async () =>
//             {
//                 var attachments = await GetAndLogAttachmentType9Async();
//                 Assert.That(attachments.Count, Is.GreaterThanOrEqualTo(2),
//                     "There should be at least 2 attachmentType 9 records after PATCH");

//                 Assert.Multiple(() =>
//                 {
//                     Assert.That(attachments[0].Content, Does.Contain("Verisk"),
//                         "Latest AttachmentType 9 should contain Verisk quoteMappedValues");
//                     Assert.That(attachments[0].Content, Does.Contain("E2Value"),
//                         "Latest AttachmentType 9 should contain E2Value quoteMappedValues after PATCH");
//                     Assert.That(attachments[1].IsActive, Is.False,
//                         "Previous AttachmentType 9 record should be IsActive=0 after PATCH");
//                 });
//             }, "Expected: AttachmentType 9 exists with quoteMappedValues from both vendors; previous attachmentType 9 turned IsActive=0");
//         }
//     }
// }
