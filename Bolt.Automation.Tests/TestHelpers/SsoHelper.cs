using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;

namespace Bolt.Automation.Tests.TestHelpers
{
    /// <summary>
    /// Reusable helper for SSO authentication: relay state construction and SSO login.
    /// </summary>
    public class SsoHelper(
        IAutomationLogger logger,
        IScopeContext scopeContext,
        ISsoApiFactory ssoApiFactory)
    {
        /// <summary>
        /// Builds a quote-retrieve relay state by appending the quote ID and tenant to the base relay state value.
        /// </summary>
        public RelayStateTestData CreateQuoteRetrieveRelayState(
            string quoteId, string tenant, RelayStateTestDataCollection relayStateCollection)
        {
            var baseRelayState = relayStateCollection.RetrieveQuoteRelayState?.Value;
            if (string.IsNullOrEmpty(baseRelayState))
                throw new InvalidOperationException("RetrieveQuoteRelayState value is not set in the relay state collection");

            return new RelayStateTestData { Value = $"{baseRelayState}{quoteId}&tenant={tenant}" };
        }

        /// <summary>
        /// Sets user and relay state in scope, calls the SSO API, and navigates to the redirect URL.
        /// </summary>
        public async Task LoginAsync(UserTestData user, RelayStateTestData relayState, IBrowserManager browserManager)
        {
            await logger.ExecuteStepAsync("Log in via SSO", async () =>
            {
                scopeContext.Set(ctx => ctx.CurrentUser, user);
                scopeContext.Set(ctx => ctx.CurrentRelayStateType, relayState);

                var ssoClient = ssoApiFactory.CreateClient();
                var response = await ssoClient.GetSsoResponse();
                var redirectUrl = response.Content?.RedirectUrl
                    ?? throw new InvalidOperationException("SSO response did not contain a redirect URL");

                logger.Info($"SSO authentication successful, redirecting to: {redirectUrl}");
                await browserManager.NavigateAsync(redirectUrl);
            }, "Expected result: Logged in via SSO and navigated to target page");
        }
    }
}
