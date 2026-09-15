using Refit;

namespace Bolt.Automation.ApiClients.IntegrationHubApi.Interfaces
{
    public interface ITwilioWebhookApi
    {
        [Post("/twilio/message-reply?tenant={tenant}")]
        Task<HttpResponseMessage> SendMessageReplyAsync(
            [AliasAs("tenant")] string tenant,
            [Body(BodySerializationMethod.UrlEncoded)] IDictionary<string, string> formFields);
    }
}
