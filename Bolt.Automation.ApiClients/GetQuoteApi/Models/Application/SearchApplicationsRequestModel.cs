using Newtonsoft.Json;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class SearchApplicationsRequestModel
    {
        // The API expects the wrapper key in camelCase ("searchParameters") while the
        // inner criteria are PascalCase; the GetQuote Refit client uses DefaultContractResolver
        // (PascalCase), so the wrapper is pinned explicitly and the criteria pass through as-is.
        [JsonProperty("searchParameters")]
        public SearchApplicationsParameters SearchParameters { get; set; } = new();
    }

    public class SearchApplicationsParameters
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FriendlyId { get; set; }
        public string? ZipCode { get; set; }
        public string? Email { get; set; }
        public string? DateOfBirth { get; set; }
    }
}
