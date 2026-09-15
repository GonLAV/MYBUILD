using Bolt.Automation.Common.Context;
using System.Net;

namespace Bolt.Automation.ApiClients.IntegrationHubApi.Handlers
{
    public class TwilioSignatureHandler(IScopeContext scopeContext) : DelegatingHandler
    {
        private const string HeaderName = "X-Twilio-Signature";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Content is not null)
            {
                var authToken = scopeContext.Data.TwilioData?.AuthToken
                    ?? throw new InvalidOperationException(
                        "TwilioData is not set on ScopeContext. Set ScopeContext.Data.TwilioData before making Twilio API calls.");

                var url = request.RequestUri?.ToString()
                    ?? throw new InvalidOperationException("Request URI is required.");

                var fields = await ReadFormFieldsAsync(request.Content, cancellationToken);
                var signature = TwilioSignatureGenerator.Generate(url, fields, authToken);

                request.Headers.Remove(HeaderName);
                request.Headers.Add(HeaderName, signature);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static async Task<IReadOnlyDictionary<string, string?>> ReadFormFieldsAsync(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            var contentString = await content.ReadAsStringAsync(cancellationToken);
            return ParseFormFields(contentString);
        }

        private static IReadOnlyDictionary<string, string?> ParseFormFields(string content)
        {
            var fields = new Dictionary<string, string?>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(content))
            {
                return fields;
            }

            foreach (var pair in content.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2, StringSplitOptions.None);
                var name = WebUtility.UrlDecode(parts[0]);
                var value = parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : string.Empty;

                fields[name] = value;
            }

            return fields;
        }
    }
}