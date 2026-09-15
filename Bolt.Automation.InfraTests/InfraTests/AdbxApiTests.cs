//using System.Net;
//using Bolt.Automation.ApiClients.AdbxApi;
//using Bolt.Automation.Common.Context;
//using Bolt.Automation.Common.Enums;
//using Bolt.Automation.TestDataProvider.Builders;
//using Bolt.Automation.Common.Enums;
//using Bolt.Automation.Tests.TestExtension.Attributes;
//using Bolt.Automation.Tests.TestExtension.Base;
//using Microsoft.Extensions.DependencyInjection;
//using NUnit.Framework;
////using static Bolt.Automation.Common.Tenant;

//namespace Bolt.Automation.Tests.InfraTests
//{
//    public class AdbxApiTests : TestBase
//    {
//        private readonly IAdbxApiClientFactory _adbxApifactory;
//        private readonly IScopeContext _scopeContext;

//        public AdbxApiTests() : base()
//        {
//            _scopeContext = _testScope.ServiceProvider.GetRequiredService<IScopeContext>();
//            _adbxApifactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
//        }

//        [Test]
//        [Tenant(BOLTAG)]
//        [Property("TestCaseId", "005")]
//        [Property("Category", "API")]
//        public async Task AdbxGetQuoteByIdBoltag()
//        {
//            // Using new test data system
//            var agent = TestData.GetUser(UserRole.Agent);
//            _scopeContext.SetTestValue("User", agent.Username);

//            // If agent has API key in attributes, set it
//            if (agent.Attributes.TryGetValue("ApiKey", out var apiKey))
//            {
//                _scopeContext.SetTestValue("ApiKey", apiKey.ToString());
//            }

//            var quoteId = TestData.GetQuoteId("ValidQuote");

//            var api = await _adbxApifactory.CreateApiClientAsync();
//            var response = await api.GetQuoteByQuoteId(quoteId);

//            Assert.That(response.Content, Is.Not.Null);
//        }

//        [Test]
//        [Tenant(UNIFY)]
//        [Property("TestCaseId", "005")]
//        [Property("Category", "API")]
//        public async Task AdbxGetQuoteByIdUnify()
//        {
//            // Using new test data system
//            var testAgent = TestData.GetUser(UserRole.TestAgent);
//            _scopeContext.SetTestValue("User", testAgent.Username);

//            var quoteId = TestData.GetQuoteId("ValidQuote");

//            var api = await _adbxApifactory.CreateApiClientAsync();
//            var response = await api.GetQuoteByQuoteId(quoteId);

//            Assert.That(response.Content, Is.Not.Null);
//        }

//        [Test]
//        [Tenant(BOLTAG)]
//        [Property("TestCaseId", "005")]
//        [Property("Category", "API")]
//        public async Task AdbxGetQuoteByIdBoltagNotFound()
//        {
//            // Using new test data system
//            var agent = TestData.GetUser(UserRole.Agent);
//            _scopeContext.SetTestValue("User", agent.Username);

//            var quoteId = TestData.GetQuoteId("InvalidQuote");

//            var api = await _adbxApifactory.CreateApiClientAsync();
//            var response = await api.GetQuoteByQuoteId(quoteId);

//            Assert.That(response.StatusCode.Equals(HttpStatusCode.NotFound), Is.True, "Response was not successful");
//        }

//        [Test]
//        [Tenant(BOLTAG)]
//        [Property("TestCaseId", "006")]
//        [Property("Category", "API")]
//        public async Task CreateCustomUserAndUseInApi()
//        {
//            // Example of using builder
//            var customUser = CreateBuilder<UserDataBuilder>()
//                .FromCurrentContext()
//                .WithRole(UserRole.Agent)
//                .WithCredentials("custom.test", "Custom123!")
//                .WithEmail("custom@test.com")
//                .WithPermissions("api.read", "api.write")
//                .Build();

//            _scopeContext.SetTestValue("User", customUser.Username);

//            // Use the custom user in API test
//            var api = await _adbxApifactory.CreateApiClientAsync();
//            // ... rest of test
//        }
//    }
//}