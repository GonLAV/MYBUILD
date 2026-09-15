namespace Bolt.Automation.InternalServices.MultiConfiguration
{
    public class GetFreshnessIdResponse
    {
        public string? FreshnessId { get; set; }
    }

    public class AllTenantsSection
    {
        public string? FreshnessId { get; set; }
        public Section? Value { get; set; }
    }

    public class AllTenantsManySections
    {
        public string? FreshnessId { get; set; }
        public Dictionary<string, Section>? Value { get; set; }
    }

    public class Section
    {
        public Dictionary<string, string>? Sources { get; set; }
    }
}
