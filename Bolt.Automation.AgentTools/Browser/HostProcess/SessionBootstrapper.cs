using Automation.Configuration.FrontEnds;
using Bolt.Automation.AgentTools.SessionState;
using Bolt.Automation.Common; // Tenant enum
using Bolt.Automation.Common.Context;
using Bolt.Automation.Core.Infrastructure;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.TestDataProvider.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.AgentTools.Browser.HostProcess;

/// <summary>
/// Boots one browser session by constructing the same DI graph a UI test does
/// (<see cref="TestInfrastructure"/>), then wiring the executor to the session's
/// own <see cref="IBrowserManager"/> so navigation and screenshots share one
/// browser. The scope is handed to the <see cref="BrowserSession"/> and kept
/// alive until the session ends — disposing it closes the browser. A fresh scope
/// per session also isolates <c>FieldRegistryProvider</c> static state.
/// </summary>
internal static class SessionBootstrapper
{
    /// <summary>
    /// Constructs the session (DI scope, scope context, browser manager, executor).
    /// The browser itself launches lazily on the first navigate. Throws with a
    /// clear message on bad tenant/env or config-load failure.
    /// </summary>
    public static BrowserSession Boot(string id, string tenant, string envName, bool headed, ScopeSnapshot snapshot)
    {
        ConfigLocator.EnsureConfigPath();

        var (config, environment) = TestInfrastructure.GetConfiguration(
            new Dictionary<string, string> { ["Tenant"] = tenant, ["Environment"] = envName });

        var serviceProvider = TestInfrastructure.CreateServiceProvider(config, environment);
        var scope = serviceProvider.CreateScope();

        try
        {
            var provider = scope.ServiceProvider;

            var scopeContext = provider.GetRequiredService<IScopeContext>();
            scopeContext.StartAsyncChildScope();
            scopeContext.Set(ctx => ctx.Environment, environment);

            if (!Enum.TryParse<Tenant>(tenant, ignoreCase: true, out var tenantEnum))
                throw new ArgumentException($"Unknown tenant '{tenant}'. Expected a Tenant enum value (e.g. BOLTAG).");
            scopeContext.Set(ctx => ctx.Tenant, tenantEnum);

            // Per-tenant/env URL collection — StartUrlResolver reads this.
            var accessor = provider.GetRequiredService<TestContextAccessor>();
            scopeContext.Set(ctx => ctx.UrlDataCollection, accessor.CurrentUrlCollection);

            // Browser options, forced visible when --headed.
            var options = (provider.GetService<BrowserOptions>()
                           ?? provider.GetRequiredService<IOptions<BrowserOptions>>().Value).Clone();
            if (headed) options.Headless = false;

            var browserManager = provider.GetRequiredService<IBrowserManager>();
            browserManager.Configure(options);

            // Bind the executor to THIS browser manager (UITestBase pattern) so
            // navigate + screenshot operate on the same page.
            var executor = PlaywrightExecutorFactory.CreateWithBrowserManagerFactory(provider, () => browserManager);

            var channel = options.Channel ?? options.GetBrowserChannel();
            return new BrowserSession(id, scope, browserManager, executor, scopeContext, snapshot,
                string.IsNullOrEmpty(channel) ? null : channel);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}
