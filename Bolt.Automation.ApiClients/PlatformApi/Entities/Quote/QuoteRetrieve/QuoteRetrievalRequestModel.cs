namespace Bolt.Automation.ApiClients.PlatformApi.Entities.Quote.QuoteRetrieve
{
    public record RetrievalRequestWrapperModel
    {
        public QuoteRetrievalRequestModel RetrievalRq { get; set; }
    }
    public record QuoteRetrievalRequestModel
    {
        public string? AppId { get; init; }
        public string? LastName { get; init; }
        public string? ZipCode { get; init; }
        public Guid RqUID { get; init; }
    }
}

