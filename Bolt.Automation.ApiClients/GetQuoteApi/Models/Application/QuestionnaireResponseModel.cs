namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Application
{
    public class QuestionnaireResponseModel
    {
        public string? Url { get; set; }

        public IEnumerable<MessageModel>? Messages { get; set; }
    }

    public class MessageModel
    {
        public string? Message { get; set; }
        public string? Code { get; set; }

    }
}
