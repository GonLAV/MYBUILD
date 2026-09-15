namespace Automation.Configuration.InternalServices.BoltServices
{
    public class AuthenticationOptions
    {
        public const string SectionKey = "BoltInfrastructure:Authentication";

        public int ExpirationMinutes { get; set; }
        public string? Secret { get; set; }
        public string? Issuer { get; set; }
        public string? SecurityAlgorithmSignature { get; set; }
    }
}
