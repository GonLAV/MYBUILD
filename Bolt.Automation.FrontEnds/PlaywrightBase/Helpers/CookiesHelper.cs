using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Helpers
{
    public static class CookieHelper
    {
        public static async Task<string?> GetCookieValueAsync(IBrowserContext context, string domain, string cookieName, IAutomationLogger? logger = null)
        {
            logger?.Info($"Checking cookie '{cookieName}' on domain: {domain}");
            var cookies = await context.CookiesAsync([domain]);
            var cookieValue = cookies.FirstOrDefault(c => c.Name == cookieName)?.Value;
            logger?.Info($"Cookie '{cookieName}' value: {cookieValue ?? "(not found)"}");
            return cookieValue;
        }

        public static async Task<List<string>> GetCookieValuesAsync(
            IBrowserContext context,
            string domain,
            string cookieName,
            IAutomationLogger? logger = null)
        {
            logger?.Info($"Checking cookie '{cookieName}' on domain: {domain}");
            var cookies = await context.CookiesAsync([domain]);
            var cookieValues = cookies
                .Where(c => c.Name == cookieName)
                .Select(c => c.Value)
                .ToList();

            if (cookieValues.Count > 0)
                logger?.Info($"Cookie '{cookieName}' values: {string.Join(", ", cookieValues)}");
            else
                logger?.Info($"Cookie '{cookieName}' not found");

            return cookieValues;
        }

        public static async Task<List<string>> WaitForCookieValuesAsync(
            IBrowserContext context,
            string domain,
            string cookieName,
            string? expectedValue = null,
            int timeout = 5000,
            int pollInterval = 500,
            IAutomationLogger? logger = null)
        {
            logger?.Info($"Waiting for cookie '{cookieName}' on domain: {domain}" +
                         (expectedValue is not null ? $", expected value: {expectedValue}" : string.Empty));

            var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
            List<string> cookieValues;

            do
            {
                cookieValues = await GetCookieValuesAsync(context, domain, cookieName);

                var satisfied = expectedValue is null
                    ? cookieValues.Count > 0
                    : cookieValues.Contains(expectedValue);

                if (satisfied)
                    break;

                if (DateTime.UtcNow < deadline)
                {
                    var current = cookieValues.Count > 0 ? string.Join(", ", cookieValues) : "(not found)";
                    logger?.Info($"Cookie '{cookieName}' current value: {current}, retrying in {pollInterval}ms...");
                    await Task.Delay(pollInterval);
                }
            }
            while (DateTime.UtcNow < deadline);

            if (cookieValues.Count > 0)
                logger?.Info($"Cookie '{cookieName}' values: {string.Join(", ", cookieValues)}");
            else
                logger?.Info($"Cookie '{cookieName}' not found after {timeout}ms");

            return cookieValues;
        }
    }
}
