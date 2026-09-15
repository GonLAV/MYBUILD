namespace Automation.Configuration.InternalServices.BoltServices
{
    public class MicroserviceOptions
    {
        public const string SectionKey = "BoltInfrastructure:Microservice";

        public string? Name { get; set; }
        public string? Address { get; set; }
    }
}
