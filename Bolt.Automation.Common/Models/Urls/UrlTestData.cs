
namespace Bolt.Automation.Common.Models.Urls
{
    public class UrlTestData
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string D2CUrl { get; set; } = string.Empty;
        public string EndpointSso { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string CCPAProgressive { get; set; } = string.Empty;
        public string CCPARedirect { get; set; } = string.Empty;
        public string CANotice { get; set; } = string.Empty;
        public string DoNotSell { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalUrls { get; set; } = new();
        public string MarketsLib { get; set; } = string.Empty;

    }
}
