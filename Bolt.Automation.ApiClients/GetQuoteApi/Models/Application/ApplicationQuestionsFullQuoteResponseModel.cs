using Bolt.Automation.ApiClients.GetQuoteApi.Models.Common;
using Newtonsoft.Json.Linq;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class ApplicationQuestionsFullQuote
    {
        public string? Id { get; set; }
        public string? FriendlyId { get; set; }
        public string? ApplicantId { get; set; }
        public JObject? Data { get; set; }
        public JObject? FoundData { get; set; }
        public IEnumerable<ValidationError>? ValidationErrors { get; set; }
        public IEnumerable<Question>? Questions { get; set; }
    }
}
