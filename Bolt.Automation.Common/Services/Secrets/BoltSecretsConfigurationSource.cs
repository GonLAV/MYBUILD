using Microsoft.Extensions.Configuration;

namespace Bolt.Automation.Common.Services.Secrets;

public sealed class BoltSecretsConfigurationSource(string? path, string environment, bool optional = true)
    : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new BoltSecretsConfigurationProvider(path, environment, optional);
}
