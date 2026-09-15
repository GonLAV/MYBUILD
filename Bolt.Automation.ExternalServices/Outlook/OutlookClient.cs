using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.Common.PollyRetry;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net;
using System.Text.RegularExpressions;

namespace Bolt.Automation.ExternalServices.Outlook
{
    public class OutlookClient : IOutlookClient
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IAutomationLogger _logger;
        private readonly IPollyRetryService _pollyRetryService;

        public OutlookClient(TokenProvider tokenProvider, IHttpClientFactory httpClientFactory, IAutomationLogger logger, IPollyRetryService pollyRetryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pollyRetryService = pollyRetryService ?? throw new ArgumentNullException(nameof(pollyRetryService));
            var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
            var httpClient = httpClientFactory.CreateClient("GraphClient");
            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
            _graphClient = new GraphServiceClient(adapter);
        }

        public async Task<MessageCollectionResponse> GetMessages(string userId = "boltautomation@boltinc.com")
        {
            _logger.Info("Fetching outlook messages");
            var messages = await _graphClient.Users[userId].Messages
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Top = 10;
                    requestConfiguration.QueryParameters.Select = new[]
                        { "subject", "body", "sender", "receivedDateTime", "toRecipients" };
                    requestConfiguration.QueryParameters.Expand = new[]
                        { "attachments" };
                });

            _logger.Info($"Retrieved {messages?.Value?.Count ?? 0} messages");
            return messages;
        }

        public async Task<Message> GetSpecificEmail(string sender, string subject, string uniqueIdentifier)
        {
            var matchingEmail = await _pollyRetryService.ExecuteWithExceptionAsync(
            async () =>
            {
                var messages = await GetMessages();
                return messages.Value.FirstOrDefault(x =>
                    x.Body.Content.Contains(uniqueIdentifier) &&
                    x.Subject.Equals(subject) &&
                    x.Sender.EmailAddress.Name.Equals(sender));
            }, 120,
            $"Email with subject '{subject}' sent to '{uniqueIdentifier}' not found"
            );

            return matchingEmail;
        }

        public async Task<Message> GetSpecificEmail(string sender, string subject, DateTimeOffset receivedAfter)
        {
            var errorMessage = $"Email from '{sender}' with subject '{subject}' received after {receivedAfter:u} not found in inbox";

            var matchingEmail = await _pollyRetryService.ExecuteWithExceptionAsync(
            async () =>
            {
                var messages = await GetMessages();
                return messages.Value.FirstOrDefault(x =>
                    x.Subject.Equals(subject) &&
                    x.Sender.EmailAddress.Name.Equals(sender) &&
                    x.ReceivedDateTime.HasValue &&
                    x.ReceivedDateTime.Value >= receivedAfter);
            }, 120,
            errorMessage
            );

            return matchingEmail ?? throw new Exception(errorMessage);
        }

        // Recipient-filtered overload: distributed test pods share one automation inbox, so concurrent
        // MFA logins can produce same-sender/same-subject emails in the same window; recipient disambiguates them.
        public async Task<Message> GetSpecificEmail(string sender, string recipient, string subject, DateTimeOffset receivedAfter)
        {
            var errorMessage = $"Email from '{sender}' to '{recipient}' recipient with subject '{subject}' received after {receivedAfter:u} not found in inbox";

            var matchingEmail = await _pollyRetryService.ExecuteWithExceptionAsync(
            async () =>
            {
                var messages = await GetMessages();
                return messages.Value.FirstOrDefault(x =>
                    x.Subject.Equals(subject) &&
                    x.Sender.EmailAddress.Name.Equals(sender) &&
                    x.ToRecipients.Any(r => r.EmailAddress.Address.Equals(recipient, StringComparison.OrdinalIgnoreCase)) &&
                    x.ReceivedDateTime.HasValue &&
                    x.ReceivedDateTime.Value >= receivedAfter);
            }, 120,
            errorMessage
            );

            return matchingEmail ?? throw new Exception(errorMessage);
        }


        public async Task<bool> IsEmailReceived(string sender, string subject, string uniqueIdentifier)
        {
            _logger.Info($"Searching mail with subject {subject}, email from {sender} and unique identifier as {uniqueIdentifier}");
            var isReceived = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
            {
                var messages = await GetMessages();
                return messages?.Value?.Any(x =>
                    x.Body?.Content?.Contains(uniqueIdentifier) == true &&
                    x.Subject?.Equals(subject) == true &&
                    x.Sender?.EmailAddress?.Name?.Equals(sender) == true) == true;
            }, 100);

            return isReceived;
        }

        public async Task<bool> IsEmailReceived(string subject, string uniqueIdentifier)
        {
            _logger.Info($"Searching mail with subject {subject} and unique identifier as {uniqueIdentifier}");
            var isReceived = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
            {
                var messages = await GetMessages();
                return messages?.Value?.Any(x =>
                    x.Body?.Content?.Contains(uniqueIdentifier) == true &&
                    x.Subject?.Equals(subject) == true) == true;
            }, 100);

            return isReceived;
        }

        public async Task<bool> IsEmailReceived(string subject, string uniqueIdentifier, int attachmentCount)
        {
            _logger.Info($"Searching mail with subject {subject} and unique identifier as {uniqueIdentifier}");
            var isReceived = await _pollyRetryService.ExecuteWithRetryAsync(async () =>
            {
                var messages = await GetMessages();
                return messages?.Value?.Any(x =>
                    x.Body?.Content?.Contains(uniqueIdentifier) == true &&
                    x.Subject?.Equals(subject) == true &&
                    x.Attachments?.Count == attachmentCount) == true;
            }, 100);

            return isReceived;
        }

        public string? ExtractQuoteLink(string emailContent)
        {
            if (string.IsNullOrWhiteSpace(emailContent))
            {
                return null;
            }

            var decodedContent = WebUtility.HtmlDecode(emailContent);
            // Pattern 1: match direct or AWS-tracking-wrapped URLs containing QuoteEntrance (slash may be percent-encoded as %2F in production tracking URLs)
            var match = Regex.Match(decodedContent, "href=[\"'](?<url>https?://[^\"']*QuoteEntrance(?:/|%2F)EnterConsumerQuote[^\"']*)[\"']", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups["url"].Value;
            }

            // Pattern 2: fallback — any href containing token= (no \b: in tracking URLs the ? is percent-encoded as %3F so no word boundary before "token")
            match = Regex.Match(decodedContent, "href=[\"'](?<url>https?://[^\"']*token=[^\"']*)[\"']", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups["url"].Value;
            }

            // Pattern 3: bare URL fallback
            match = Regex.Match(decodedContent, "(?<url>https?://[^\\s\"']*token=[^\\s\"']*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? match.Groups["url"].Value : null;
        }

        public string? ExtractVerificationCode(string emailContent)
        {
            if (string.IsNullOrWhiteSpace(emailContent))
            {
                return null;
            }

            var decodedContent = WebUtility.HtmlDecode(emailContent);
            var match = Regex.Match(decodedContent, @"verification code is:\s*(?<code>\d{6})", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["code"].Value : null;
        }
    }
}
