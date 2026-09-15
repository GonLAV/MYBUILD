using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class PostApplicationResponseModel<T>
    {
        public string? Id { get; set; }
        public string? FriendlyId { get; set; }
        public string? ApplicantId { get; set; }
        public T? Data { get; set; }
        public List<PrefillIndicationItem>? PrefillIndication { get; set; }
        public IEnumerable<Question>? Questions { get; set; }
        public IEnumerable<ValidationError>? ValidationErrors { get; set; }
        public IEnumerable<Warning>? Warnings { get; set; }
    }

    public class PrefillIndicationItem
    {
        public string? Id { get; set; }
        public string? Source { get; set; }
    }

    public class Warning
    {
        public string? Id { get; set; }
        public List<string>? Errors { get; set; }
        public List<string>? ValidValues { get; set; }
    }
}
