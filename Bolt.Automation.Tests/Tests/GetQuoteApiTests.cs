using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests
{

    public class GetQuoteApiTests : TestBase
    {
        private IGetQuoteApi _getQuoteApi = null!;
        private GetQuoteApiHelper _getQuoteApiHelper = null!;

        protected override void ResolveServices()
        {
            base.ResolveServices();
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _getQuoteApiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
        }

        [Test]
        [Tenant(USAA)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [TestCaseId(237188)]
        [Description("Verifies GET questions/fullquote honours the QuestionSet query parameter: QuestionSet=All returns " +
                     "the applicant questions FirstName and Email, while omitting the parameter returns the filtered set without them.")]
        [Author(Author.Gil)]
        public async Task GetQuestions_FullQuote_QuestionSet_FullOrFilteredByQueryParameter()
        {
            const string carrier = nameof(CarrierEnums.Progressive);
            const string product = "PersonalAuto";
            // Applicant-level questions that belong to the full set only — the filtered set must not return them.
            const string firstNameQuestion = "FirstName";
            const string emailQuestion = "Email";

            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Agent);

            var applicationId = await _logger.ExecuteStepAsync("Create a PL Auto application via GetQuote API", async () =>
            {
                var request = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetPersonalAutoData(
                        AddressData.CA,
                        [Vehicles.SCFAD02E19GB11912],
                        [Drivers.UsaaTestDriver],
                        PersonalInfo.GetRandomPersonalInfo(),
                        PolicyTestData.UsaaPolicyTestData)
                };
                request.Data.MailingAddress = AddressData.CA;

                var application = await _getQuoteApi.CreateApplicationAsync(request)
                    .EnsureSuccessContentAsync("Failed to create the PL Auto application");
                return application.Id!;
            }, "PL Auto application is created and its id is captured");

            await _logger.ExecuteStepAsync("Submit the application", async () =>
                await _getQuoteApi.SubmitApplicationAsync(applicationId)
                    .EnsureSuccessContentAsync($"Failed to submit application '{applicationId}'", allowNullContent: true),
                "Application is submitted successfully");

            await _logger.ExecuteStepAsync("GET questions/fullquote with QuestionSet=All and verify the full question set", async () =>
            {
                var questionIds = await _getQuoteApiHelper.GetQuestionIdsFullQuoteAsync(applicationId, carrier, product, fullQuestionSet: true);

                Assert.Multiple(() =>
                {
                    Assert.That(questionIds, Does.Contain(firstNameQuestion),
                        $"'{firstNameQuestion}' should be returned when QuestionSet=All");
                    Assert.That(questionIds, Does.Contain(emailQuestion),
                        $"'{emailQuestion}' should be returned when QuestionSet=All");
                });
            }, $"Full question set contains '{firstNameQuestion}' and '{emailQuestion}'");

            await _logger.ExecuteStepAsync("GET questions/fullquote without QuestionSet and verify the filtered question set", async () =>
            {
                var questionIds = await _getQuoteApiHelper.GetQuestionIdsFullQuoteAsync(applicationId, carrier, product);

                Assert.Multiple(() =>
                {
                    Assert.That(questionIds, Does.Not.Contain(firstNameQuestion),
                        $"'{firstNameQuestion}' should not be returned when QuestionSet is omitted");
                    Assert.That(questionIds, Does.Not.Contain(emailQuestion),
                        $"'{emailQuestion}' should not be returned when QuestionSet is omitted");
                });
            }, $"Filtered question set does not contain '{firstNameQuestion}' or '{emailQuestion}'");
        }
    }
}
