using Microsoft.Extensions.Configuration;

namespace Bolt.Automation.Common.Services.Secrets;

public static class BoltSecretsConfigurationExtensions
{
    public static IConfigurationBuilder AddBoltSecrets(
        this IConfigurationBuilder builder,
        string? path,
        string environment,
        bool optional = true)
        => builder.Add(new BoltSecretsConfigurationSource(path, environment, optional));
}
