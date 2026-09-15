using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.InfraTests.TestExtension.Attributes;
using Bolt.Automation.InfraTests.TestExtension.Base;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.ApplicationTestData;
using Bolt.Automation.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;

namespace Bolt.Automation.InfraTests.InfraTests
{
    public class GetQuoteApiTests : TestBase
    {
        private readonly IGetQuoteApi _getQuoteApi;
        private readonly IScopeContext _scopeContext;
        public GetQuoteApiTests() : base()
        {
            _scopeContext = _testScope.ServiceProvider.GetRequiredService<IScopeContext>();
            var refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();
        }

        [Test]
        [Tenant(BOLTAG)]
        [Property("TestCaseId", "03")]
        [Property("Category", "API")]
        [Property("Product", "PersonalAuto")]
        public async Task CreateApplicationPersonalAutoTest()
        {
            _logger.Info("Starting CreateApplicationPersonalAutoTest");

            var consumerUser = TestContextAccessor.CurrentUserCollection.ConsumerOrganicPL;
            ScopeContext.Set(ctx => ctx.CurrentUser, consumerUser);

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.PersonalAuto,
                Data = PersonalLineDataProvider.GetPersonalAutoData()
            };

            var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
            var submissionResponse = await apiHelper.CreateAndSubmitApplicationWithPollingAsync(requestData);

            _logger.Info($"Request data prepared: {JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true })}");
            var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData);
            _logger.Info($"Received response: StatusCode={createApplicationResponse.StatusCode}, Error={createApplicationResponse.GetErrorContent()}");
            createApplicationResponse.EnsureSuccessContent($"Failed to create application : {createApplicationResponse.GetErrorContent()}");
            Assert.That(createApplicationResponse, Is.Not.Null);
            _logger.Info("Successfully created PersonalAuto application");
        }

        [Test]
        [Tenant(BOLTAG)]
        [Property("TestCaseId", "04")]
        [Property("Category", "API")]
        [Property("Product", "Homeowners")]
        public async Task CreateApplicationPersonalHomeTest()
        {
            var consumerUser = TestContextAccessor.CurrentUserCollection.ConsumerOrganicPL;
            ScopeContext.Set(ctx => ctx.CurrentUser, consumerUser);

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Homeowners,
                Data = PersonalLineDataProvider.GetPersonalHomeData()
            };
            var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData);
            createApplicationResponse.EnsureSuccessContent($"Failed to create application : {createApplicationResponse.GetErrorContent()}");
            Assert.That(createApplicationResponse, Is.Not.Null);
            _logger.Info("Successfully created Homeowners application");
        }

        [Test]
        [Tenant(BOLTAG)]
        [Property("TestCaseId", "05")]
        [Property("Category", "API")]
        [Property("Product", "Renters")]
        public async Task CreateApplicationRentersTest()
        {
            var consumerUser = TestContextAccessor.CurrentUserCollection.ConsumerOrganicPL;
            ScopeContext.Set(ctx => ctx.CurrentUser, consumerUser);

            var requestData = new ApplicationRequestModel<PersonalLineData>
            {
                Products = ApplicationTestData.Products.Renters,
                Data = PersonalLineDataProvider.GetPersonalHomeData()
            };
            requestData.Data.PersonalLineGender = "Female";
            requestData.Data.OccupationStr = "Accountant/Auditor";
            requestData.Data.EmploymentIndustry = "Banking/Finance/RE";
            var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData);
            var errorContent = createApplicationResponse.GetErrorContent();
            createApplicationResponse.EnsureSuccessContent($"Failed to create application : {errorContent}");
            Assert.That(createApplicationResponse, Is.Not.Null);
            _logger.Info("Successfully created Renters application");
        }
    }
}
