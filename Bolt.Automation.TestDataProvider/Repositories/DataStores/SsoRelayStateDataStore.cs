using Bolt.Automation.Common;
using Bolt.Automation.Common.Models.RelayStates;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    public static class SsoRelayStateDataStore
    {
        public static readonly Dictionary<(Tenant, Environment), RelayStateTestDataCollection> All = new()
        {
            // COMPARION QA Environment
            [(Tenant.COMPARION, Environment.Qa)] = new RelayStateTestDataCollection
            {
                AccountsRelayState = new RelayStateTestData { Value = "https://adbx-qa-comparion.boltqa.com/accounts/" },
                UsersRelayState = new RelayStateTestData { Value = "https://adbx-qa-comparion.boltqa.com/users/new-user" },
                AgentMccRelayState = new RelayStateTestData { Value = "https://adbx-qa-comparion.boltqa.com/carrier-credentials/agent" },
                ConsumerMccRelayState = new RelayStateTestData { Value = "https://adbx-qa-comparion.boltqa.com/carrier-credentials/consumer" },
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://interviewapi-qa-comparion.boltqa.com/QuoteEntrance/Retrieve?quoteId=" },
                DummyAdbxRelayState = new RelayStateTestData { Value = "https://adbx-qa-comparion.boltqa.com/aad75c90-0161-ee11-8c16-005056b5be9d/timeline" },
                DefaultsRelayState = new RelayStateTestData { Value = "https://adbx-qa-comparion.boltqa.com/defaults-management" }
            },

            // COMPARION UAT Environment
            [(Tenant.COMPARION, Environment.Uat)] = new RelayStateTestDataCollection
            {
                AccountsRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/accounts/" },
                UsersRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/users/new-user" },
                AgentMccRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/carrier-credentials/agent" },
                ConsumerMccRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/carrier-credentials/consumer" },
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/QuoteEntrance/Retrieve?quoteId=" },
                DummyAdbxRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/aad75c90-0161-ee11-8c16-005056b5be9d/timeline" },
                DefaultsRelayState = new RelayStateTestData { Value = "https://adbx-comparion.bolttest.com/defaults-management" }
            },

            // USAA QA Environment
            [(Tenant.USAA, Environment.Qa)] = new RelayStateTestDataCollection
            {
                DummyD2CRelayState = new RelayStateTestData { Value= "https://d2cinterview.boltqa.com/OnlineQuote" },
                D2CRelayState = new RelayStateTestData { Value = "https://d2cinterview-qa.boltqa.com/OnlineQuote" }
            },

            // USAA Uat Environment
            [(Tenant.USAA, Environment.Uat)] = new RelayStateTestDataCollection
            {
                DummyD2CRelayState = new RelayStateTestData { Value = "https://d2cinterviewtest.bolttest.com/OnlineQuote" },
                D2CRelayState = new RelayStateTestData { Value = "https://d2cinterview.bolttest.com/OnlineQuote" }
            },

            [(Tenant.PROGRESSIVEPL, Environment.Dev)] = new RelayStateTestDataCollection
            {
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://platformapi-dev-progressive.boltqa.com/QuoteEntrance/Retrieve?quoteId=" },
            },

            [(Tenant.PROGRESSIVEPL, Environment.Qa)] = new RelayStateTestDataCollection
            {
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://interviewapi-qa.progressive.boltqa.com/QuoteEntrance/Retrieve?quoteId=" },
            },

            [(Tenant.PROGRESSIVEPL, Environment.Uat)] = new RelayStateTestDataCollection
            {
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://interviewapi-progressive.bolttest.com/QuoteEntrance/Retrieve?quoteId=" },
            },

            [(Tenant.PROGRESSIVEPL, Environment.Production)] = new RelayStateTestDataCollection
            {
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://progressive-home-api.boltinc.com/QuoteEntrance/Retrieve?quoteId=" },
            },

            // LIBERTYX QA Environment
            [(Tenant.LIBERTYX, Environment.Qa)] = new RelayStateTestDataCollection
            {
                AccountsRelayState = new RelayStateTestData { Value = "https://adbx-qa-libertyx.boltqa.com/accounts/" },
                UsersRelayState = new RelayStateTestData { Value = "https://adbx-qa-libertyx.boltqa.com/users/new-user" },
                AgentMccRelayState = new RelayStateTestData { Value = "https://adbx-qa-libertyx.boltqa.com/carrier-credentials/agent" },
                ConsumerMccRelayState = new RelayStateTestData { Value = "https://adbx-qa-libertyx.boltqa.com/carrier-credentials/consumer" },
                RetrieveQuoteRelayState = new RelayStateTestData { Value= "https://interviewapi-qa-libertyx.boltqa.com/QuoteEntrance/Retrieve?quoteId=" },
                DefaultsRelayState = new RelayStateTestData { Value = "https://adbx-qa-libertyx.boltqa.com/defaults-management" }
            },

            // LIBERTYX UAT Environment
            [(Tenant.LIBERTYX, Environment.Uat)] = new RelayStateTestDataCollection
            {
                AccountsRelayState = new RelayStateTestData { Value = "https://adbx-libertyx.bolttest.com/accounts/" },
                UsersRelayState = new RelayStateTestData { Value = "https://adbx-libertyx.bolttest.com/users/new-user" },
                AgentMccRelayState = new RelayStateTestData { Value = "https://adbx-libertyx.bolttest.com/carrier-credentials/agent" },
                ConsumerMccRelayState = new RelayStateTestData { Value = "https://adbx-libertyx.bolttest.com/carrier-credentials/consumer" },
                RetrieveQuoteRelayState = new RelayStateTestData { Value = "https://interviewapi-libertyx.bolttest.com/QuoteEntrance/Retrieve?quoteId=" },
                DefaultsRelayState = new RelayStateTestData { Value = "https://adbx-libertyx.bolttest.com/defaults-management" }
            },

            // BOLTAG QA Environment
            [(Tenant.BOLTAG, Environment.Qa)] = new RelayStateTestDataCollection()
            
        };
    }
}