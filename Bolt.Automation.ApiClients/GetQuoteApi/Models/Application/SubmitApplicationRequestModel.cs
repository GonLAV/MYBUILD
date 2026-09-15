namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class SubmitApplicationRequestModel
    {
        public Credentials? Credentials { get; set; }
    }

    public class Credentials
    {
        public string? Carrier { get; set; }
        public List<string>? Lob { get; set; }
        public Details? Details { get; set; }
}

    public class  Details
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ProducerCode { get; set; }
    }
}
