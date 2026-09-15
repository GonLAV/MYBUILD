namespace Automation.Configuration.ApiClients
{
    public class GetQuoteApiOptions : ApiOptionsBase
    {
        public const string ConfigSection = "GetQuoteApi";
        public string AuthBaseUrl { get; set; } = string.Empty;
    }
}