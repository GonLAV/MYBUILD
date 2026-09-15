using System.Net;
using Bolt.Automation.Common.Exceptions;
using Refit;

namespace Bolt.Automation.ApiClients.Infrastructure
{
    public static class ApiResponseExtensions
    {
        // Returns the API content or throws a descriptive exception if failed or null.
        public static T EnsureSuccessContent<T>(this ApiResponse<T> response, string? customMessage = null, bool allowNullContent = false)
        {
            if (!response.IsSuccessStatusCode)
                throw BuildApiException(response, customMessage);

            if (response.Error != null)
                throw new InvalidOperationException(
                    $"API returned success but deserialization failed: {response.Error.Message}",
                    response.Error);

            if (response.Content is not null)
                return response.Content;

            if (allowNullContent)
                return default!;

            throw new InvalidOperationException("API returned 2xx success but content is null.");

        }
        // Awaits an API response task and throws if the result is not successful.
        public static async Task<T> EnsureSuccessContentAsync<T>(this Task<ApiResponse<T>> responseTask, string? customMessage = null, bool allowNullContent = false)
        {
            var response = await responseTask;
            return response.EnsureSuccessContent(customMessage, allowNullContent);
        }
        
        // For negative test cases — checks that the API failed with a specific expected code.
        public static bool FailedWithStatus<T>(this ApiResponse<T> response, HttpStatusCode expectedStatusCode)
        {
            return !response.IsSuccessStatusCode && response.StatusCode == expectedStatusCode;
        }

        // Safely reads the error body from a Refit ApiResponse. In Refit 11+, ApiResponse.Error is typed
        // as ApiExceptionBase which doesn't expose Content; only the ApiException subclass does.
        public static string? GetErrorContent(this IApiResponse response)
            => (response.Error as ApiException)?.Content ?? response.Error?.InnerException?.Message;

        // Internal helper to consistently build an exception.
        private static Exception BuildApiException<T>(ApiResponse<T> response, string? customMessage)
        {
            var label = customMessage ?? "API call failed";
            var status = response.StatusCode is { } statusCode
                ? $"{(int)statusCode} ({statusCode})"
                : "no response received";
            var errorContent = (response.Error as ApiException)?.Content
                ?? response.Error?.InnerException?.Message
                ?? response.Error?.Message;

            var message = $"{label}: {status}" +
                          (errorContent != null ? $" Error Content: {errorContent}" : "");

            return new ApiResponseException(message);
        }
    }
}
