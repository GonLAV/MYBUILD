namespace Bolt.Automation.ApiClients.SSO.Saml
{
    public class SamlRecipient(string destination, string recipient, string audience)
    {
        public string? Destination { get; set; } = destination;
        public string? Recipient { get; set; } = recipient;
        public string? Audience { get; set; } = audience;
    }
}
