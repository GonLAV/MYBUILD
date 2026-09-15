namespace Automation.Configuration.InternalServices.MultiConfiguration
{
    public class MultiConfigurationOptions
    {
        public const string SectionKey = "BoltInfrastructure:MultiConfiguration";
        public const string DEFAULT_COMMONTENANT = "COMMON";

        public int RefreshRateSeconds { get; set; }
        public string? CommonTenant { get; set; }
    }
}
