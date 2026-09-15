using System.Security.Cryptography;
using System.Text;

namespace Bolt.Automation.ApiClients.IntegrationHubApi
{
    public static class TwilioSignatureGenerator
    {
        public static string Generate(string url, IReadOnlyDictionary<string, string?> formFields, string authToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            ArgumentException.ThrowIfNullOrWhiteSpace(authToken);
            ArgumentNullException.ThrowIfNull(formFields);

            var data = new StringBuilder(url);

            foreach (var field in formFields.OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                data.Append(field.Key);
                data.Append(field.Value ?? string.Empty);
            }

            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data.ToString()));

            return Convert.ToBase64String(hash);
        }
    }
}
