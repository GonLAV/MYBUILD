using Automation.Configuration.InternalServices.BoltServices;
using Automation.Configuration.InternalServices.MultiConfiguration;

namespace Automation.Configuration.InternalServices
{
    public class BoltInfrastructureOptions
    {
        public const string SectionKey = "Infrastructure";

        public AuthenticationOptions Authentication { get; set; } = new();
        public MicroserviceOptions Microservice { get; set; } = new();
        public ServiceRegistrationOptions ServiceRegistration { get; set; } = new();
        public MultiConfigurationOptions MultiConfiguration { get; set; } = new();
    }
}