using Microsoft.Graph.Models;

namespace Bolt.Automation.ExternalServices.Outlook
{
    public interface IOutlookClient
    {
        Task<MessageCollectionResponse> GetMessages(string userId = "boltautomation@boltinc.com");
        Task<Message> GetSpecificEmail(string emailFrom, string subject, string uniqueIdentifier);
        Task<Message> GetSpecificEmail(string emailFrom, string subject, DateTimeOffset receivedAfter);
        // Recipient-filtered overload: distributed test pods share one automation inbox, so concurrent
        // MFA logins can produce same-sender/same-subject emails in the same window; recipient disambiguates them.
        Task<Message> GetSpecificEmail(string emailFrom,string recipient, string subject, DateTimeOffset receivedAfter);
        //Task<Message> GetSpecificEmail(string subject, string emailTo);
        Task<bool> IsEmailReceived(string emailFrom, string subject, string uniqueIdentifier);
        Task<bool> IsEmailReceived(string emailFrom, string subject, int attachmentCount);
        Task<bool> IsEmailReceived(string subject, string uniqueIdentifier);
        string? ExtractQuoteLink(string emailContent);
        string? ExtractVerificationCode(string emailContent);
        //string GetDeepLink(ItemBody body);
        //bool IsSpecificContentExistInEmailContent(ItemBody body, string content);
    }
}
