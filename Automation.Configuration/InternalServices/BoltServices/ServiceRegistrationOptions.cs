namespace Automation.Configuration.InternalServices.BoltServices
{
    public class ServiceRegistrationOptions
    {
        public const string SectionKey = "BoltInfrastructure:ServiceRegistration";

        public string? Address { get; set; }
    }
}
