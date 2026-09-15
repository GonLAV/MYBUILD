using Bolt.Automation.ApiClients;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.ExternalServices;
using Bolt.Automation.FrontEnds;
using Bolt.Automation.InternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.Core.Infrastructure
{
    /// <summary>
    /// DI registration for runtime-injected tests (<see cref="InjectedTestConfig"/> consumers,
    /// e.g. Professional Services onboarding suites).
    /// Mirrors <see cref="InfrastructureServiceCollectionExtensions.AddInfrastructureServices"/>
    /// but deliberately skips:
    ///   * <c>AddTestDataProvider</c> — its <c>TestContextAccessor</c> reads tenant-keyed data stores.
    ///   * <c>AddApiClients</c> — its Refit handlers carry tenant-aware auth logic.
    ///   * <c>AddExternalServices</c> — not needed for the UI-driven onboarding flow.
    /// Keeps logging, Mongo reporting, browser/Playwright, memory cache, and IScopeContext.
    /// </summary>
    public static class InjectedServiceCollectionExtensions
    {
        public static IServiceCollection AddInjectedInfrastructureServices(this IServiceCollection services,
                    IConfiguration configuration, IConfiguration loggingConfiguration, InjectedTestConfig injectedConfig)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(loggingConfiguration);
            ArgumentNullException.ThrowIfNull(injectedConfig);

            services.AddSingleton(injectedConfig);

            services.AddCommonServices(configuration, loggingConfiguration);
            services.AddStandardLogging(loggingConfiguration);
            services.AddAutomationLogger();
            services.AddMongoReporting(configuration);

            services.AddFrontEndServices(configuration);
            services.AddMemoryCache();
            services.AddInternalServices(configuration);
            services.AddExternalServices(configuration);
            services.AddApiClients(configuration);
            services.AddSingleton(configuration);

            return services;
        }
    }
}
