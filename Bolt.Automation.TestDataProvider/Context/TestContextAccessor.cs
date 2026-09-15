using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Models.CarrierBridge;
using Bolt.Automation.Common.Models.Database;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Twilio;
using Bolt.Automation.Common.Models.Urls;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.Common.Services.Secrets;
using Bolt.Automation.TestDataProvider.Repositories.DataStores;

namespace Bolt.Automation.TestDataProvider.Context
{
    public class TestContextAccessor(IScopeContext scopeContext)
    {
        private readonly IScopeContext _scopeContext = scopeContext;

        public string? GetTestSpecificValue(string key)
        {
            return TestSpecificDataStore.GetValue(_scopeContext.Data.Tenant, _scopeContext.Data.Environment, key);
        }

        public UrlTestDataCollection CurrentUrlCollection
        {
            get
            {
                var tenant = _scopeContext.Get(ctx => ctx.Tenant);
                var environment = _scopeContext.Get(ctx => ctx.Environment);
                if (tenant == null)
                    throw new TestSetupException("Tenant is not set in the test context.");
                if (environment == null)
                    throw new TestSetupException("Environment is not set in the test context.");
                return UrlDataStore.All[(tenant.Value, environment.Value)];
            }
        }

        public UserTestDataCollection CurrentUserCollection
        {
            get
            {
                var tenant = _scopeContext.Get(ctx => ctx.Tenant);
                var environment = _scopeContext.Get(ctx => ctx.Environment);
                if (tenant == null)
                    throw new TestSetupException("Tenant is not set in the test context.");
                if (environment == null)
                    throw new TestSetupException("Environment is not set in the test context.");

                // Credentials live in the secrets bundle (Tier-2). Overlay them onto a private clone
                // so the shared static store is never mutated and no credential leaks across tests.
                var collection = UserDataStore.All[(tenant.Value, environment.Value)];
                var secrets = SecretsStore.Instance.IsLoaded
                    ? SecretsStore.Instance.GetUserSecrets(tenant.Value, environment.Value)
                    : null;
                return collection.HydrateFrom(secrets);
            }
        }

        public CarrierBridgeUrlCollection CarrierBridgeUrls
        {
            get
            {
                var environment = _scopeContext.Get(ctx => ctx.Environment);
                if (environment == null)
                    throw new TestSetupException("Environment is not set in the test context.");
                return CarrierBridgeUrlDataStore.All[environment.Value];
            }
        }

        public RelayStateTestDataCollection CurrentRelayStateCollection
        {
            get
            {
                var tenant = _scopeContext.Get(ctx => ctx.Tenant);
                var environment = _scopeContext.Get(ctx => ctx.Environment);
                if (tenant == null)
                    throw new TestSetupException("Tenant is not set in the test context.");
                if (environment == null)
                    throw new TestSetupException("Environment is not set in the test context.");
                return SsoRelayStateDataStore.All[(tenant.Value, environment.Value)];
            }
        }

        public DatabaseTestDataCollection CurrentDatabaseConnectionCollection
        {
            get
            {
                var tenant = _scopeContext.Get(ctx => ctx.Tenant);
                var environment = _scopeContext.Get(ctx => ctx.Environment);
                if (tenant == null)
                    throw new TestSetupException("Tenant is not set in the test context.");
                if (environment == null)
                    throw new TestSetupException("Environment is not set in the test context.");

                var connections = DatabaseConnectionsDataStore.Connections[(tenant.Value, environment.Value)];
                return new DatabaseTestDataCollection(connections, environment.Value, tenant.Value);
            }
        }

        public TwilioTestData? CurrentTwilioData
        {
            get
            {
                var tenant = _scopeContext.Get(ctx => ctx.Tenant);
                var environment = _scopeContext.Get(ctx => ctx.Environment);
                if (tenant == null)
                    throw new TestSetupException("Tenant is not set in the test context.");
                if (environment == null)
                    throw new TestSetupException("Environment is not set in the test context.");
                var subtenant = _scopeContext.Data.CurrentUser?.Subtenant ?? string.Empty;
                var twilio = TwilioDataStore.Get(tenant.Value, environment.Value, subtenant);
                if (twilio == null)
                    return null;

                // AuthToken/AccountSid live in the secrets bundle (Tier-2); overlay onto the record.
                if (SecretsStore.Instance.IsLoaded &&
                    SecretsStore.Instance.GetTwilioSecrets(tenant.Value, environment.Value, subtenant) is { } secrets)
                {
                    twilio = twilio with
                    {
                        AuthToken = secrets.AuthToken ?? twilio.AuthToken,
                        AccountSid = secrets.AccountSid ?? twilio.AccountSid,
                    };
                }

                return twilio;
            }
        }
    }
}
