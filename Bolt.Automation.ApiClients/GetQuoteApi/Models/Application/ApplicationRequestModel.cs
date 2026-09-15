namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class ApplicationRequestModel<T>
    {
        public List<string>? Products { get; set; }
        public string? ApplicantId { get; set; }
        public T? Data { get; set; }
    }
}
