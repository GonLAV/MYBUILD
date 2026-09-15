using Bolt.Automation.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.InternalServices.Database.Queries.Payment;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TestBase = Bolt.Automation.InfraTests.TestExtension.Base.TestBase;

namespace Bolt.Automation.InfraTests.InfraTests
{
    public class DBTests : TestBase
    {
        private readonly IScopeContext _scopeContext;
        private readonly IMainQueries _mainQueries;
        private readonly IPaymentQueries _paymentQueries;

        public DBTests()
            : base()
        {
            _scopeContext = _testScope.ServiceProvider.GetRequiredService<IScopeContext>();
            _mainQueries = _testScope.ServiceProvider.GetRequiredService<IMainQueries>();
            _paymentQueries = _testScope.ServiceProvider.GetRequiredService<IPaymentQueries>();
        }

        [Test]
        //[Tenant(Tenant.PROGRESSIVEPL)]
        [Tenant(Tenant.BOLTAG)]
        public async Task Query_Policy()
        {
            try
            {
                // MainDB tests
                //Policy table
                var firstPolicy = await _mainQueries.Policy.GetFirstPolicyAsync();
                Assert.That(firstPolicy, Is.Not.Null);
                var policyExternalId = await _mainQueries.Policy.GetPolicyIdByFriendlyIdAsync(firstPolicy.FriendlyId);
                Assert.That(policyExternalId, Is.Not.Null);
                //Consumer table
                var firstConsumer = await _mainQueries.Consumer.GetFirstConsumerAsync();
                Assert.That(firstConsumer, Is.Not.Null);
                //Join example
                var firstRec = await _mainQueries.Policy.GetFirstFriendlyIdWithAttachmentAsync();
                var policyAndPolicyAttachment =
                    await _mainQueries.Policy.GetPolicyAndAttachmentsByFriendlyIdAsync(firstRec);

                Assert.That(policyAndPolicyAttachment, Is.Not.Null);

                //PaymentDB tests
                var tansactions = _paymentQueries.Transaction;
                var tansactionFirst = await tansactions.GetFirstTransactionAsync();
                Assert.That(tansactionFirst, Is.Not.Null);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}