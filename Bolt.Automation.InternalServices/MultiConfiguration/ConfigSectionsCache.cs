using System.Collections.Concurrent;

namespace Bolt.Automation.InternalServices.MultiConfiguration
{
    internal class ConfigSectionsCache
    {
        public string? FreshnessId { get; init; }
        public ConcurrentDictionary<string, Dictionary<string, ConfigSectionState>>? Cache { get; init; }
    }
}
