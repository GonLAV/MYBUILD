using Automation.Configuration.ExternalServices;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bolt.Automation.ExternalServices.LaunchDarkly
{
    public static class LaunchDarklyServiceExtensions
    {
        public static IServiceCollection AddLaunchDarklyServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<LaunchDarklyClientOptions>(configuration.GetSection(LaunchDarklyClientOptions.ConfigSection));
            services.AddSingleton<LdClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<LaunchDarklyClientOptions>>().Value;
                if (string.IsNullOrEmpty(options.SdkKey))
                    throw new InvalidOperationException("LaunchDarkly SDK key is not configured.");
                return new LdClient(options.SdkKey);
            });
            services.AddScoped<ILaunchDarklyUserContext, LaunchDarklyUserContext>();
            services.AddScoped<IFeatureFlagService, LaunchDarklyFeatureService>();
            return services;
        }
    }
}

