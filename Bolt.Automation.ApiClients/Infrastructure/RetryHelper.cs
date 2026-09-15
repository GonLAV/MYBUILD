using System.Net;
using System.Text.Json;
using Bolt.Automation.Common.Exceptions;
using Refit;

namespace Bolt.Automation.ApiClients.Infrastructure
{
    public static class RetryHelper
    {
        private static readonly HashSet<HttpStatusCode> TransientStatusCodes =
        [
            HttpStatusCode.BadGateway,           // 502
            HttpStatusCode.ServiceUnavailable,   // 503
            HttpStatusCode.GatewayTimeout,       // 504
            HttpStatusCode.RequestTimeout,       // 408
            (HttpStatusCode)429                  // Too Many Requests
        ];

        /// <summary>
        /// Executes an API call and retries on transient HTTP errors (502, 503, 504, 408, 429).
        /// On final success, returns the deserialized content via EnsureSuccessContent.
        /// </summary>
        public static async Task<T> RetryOnTransientAsync<T>(
            Func<Task<ApiResponse<T>>> action,
            string? customMessage = null,
            int maxAttempts = 3,
            TimeSpan? delay = null)
        {
            delay ??= TimeSpan.FromSeconds(3);

            ApiResponse<T> response = null!;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                response = await action();

                if (response.IsSuccessStatusCode)
                    return response.EnsureSuccessContent(customMessage);

                if (!response.StatusCode.HasValue || !TransientStatusCodes.Contains(response.StatusCode.Value))
                    return response.EnsureSuccessContent(customMessage);

                if (attempt == maxAttempts)
                {
                    var errorContent = (response.Error as ApiException)?.Content ?? response.Error?.InnerException?.Message ?? "no details";
                    var label = customMessage ?? "API call";
                    throw new ApiResponseException(
                        $"{label} failed after {maxAttempts} attempts with {(int)response.StatusCode} ({response.StatusCode}). Last error: {errorContent}");
                }

                await Task.Delay(delay.Value);
            }

            return response.EnsureSuccessContent(customMessage);
        }

        public static async Task<T> RetryAsync<T>(
            Func<Task<T>> action,
            Predicate<T>? predicate = null,
            int maxAttempts = 3,
            TimeSpan? delay = null)
        {
            delay ??= TimeSpan.FromSeconds(2);
            predicate ??= GetDefaultPredicate<T>();

            T result = await action();

            for (int attempt = 1; attempt < maxAttempts; attempt++)
            {
                if (predicate(result))
                    return result;

                await Task.Delay(delay.Value);
                result = await action();
            }

            return result;
        }


        /// <summary>
        /// Polls <paramref name="action"/> until <paramref name="contentPredicate"/> holds on a successful
        /// response, or the timeout elapses. On timeout, throws an <see cref="ApiResponseException"/> that
        /// carries the poll count and the last response seen — never a bare cancellation.
        /// </summary>
        /// <param name="label">
        /// What is being polled (e.g. "Submission for application 'abc'"), used in the timeout message.
        /// </param>
        public static async Task<ApiResponse<T>> RetryUntilAsync<T>(
       Func<Task<ApiResponse<T>>> action,
       Func<T, bool> contentPredicate,
       TimeSpan? timeout = null,
       TimeSpan? delay = null,
       string? label = null)
        {
            delay ??= TimeSpan.FromSeconds(5);
            timeout ??= TimeSpan.FromSeconds(30);
            label ??= "Polled call";

            using var cts = new CancellationTokenSource(timeout.Value);
            var token = cts.Token;

            ApiResponse<T>? lastResponse = null;
            int polls = 0;

            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var response = await action();
                    lastResponse = response;
                    polls++;

                    if (response.IsSuccessStatusCode && response.Content != null && contentPredicate(response.Content))
                        return response;

                    await Task.Delay(delay.Value, token);
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new ApiResponseException(
                    $"{label} did not satisfy its condition within {timeout.Value.TotalSeconds:0.#}s " +
                    $"({polls} poll(s), {delay.Value.TotalSeconds:0.#}s apart). " +
                    $"Last response: {DescribeLastResponse(lastResponse)}");
            }
        }

        /// <summary>
        /// Renders the last polled response for a timeout message: status, error body, and content.
        /// </summary>
        private static string DescribeLastResponse<T>(ApiResponse<T>? response)
        {
            if (response is null)
                return "none — the first call never completed";

            var status = response.StatusCode.HasValue
                ? $"{(int)response.StatusCode.Value} ({response.StatusCode.Value})"
                : "no status";

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = (response.Error as ApiException)?.Content
                    ?? response.Error?.Message
                    ?? "no details";
                return $"{status}, error: {Truncate(errorContent)}";
            }

            return response.Content is null
                ? $"{status}, content: null"
                : $"{status}, content: {Truncate(SafeSerialize(response.Content))}";
        }

        private static string SafeSerialize(object content)
        {
            try
            {
                return JsonSerializer.Serialize(content);
            }
            catch (NotSupportedException)
            {
                // A model the serializer can't walk — the type name is still better than nothing.
                return content.GetType().Name;
            }
        }

        private static string Truncate(string value, int maxLength = 1000) =>
            value.Length <= maxLength ? value : $"{value[..maxLength]}… [truncated, {value.Length} chars]";



        private static Predicate<T> GetDefaultPredicate<T>()
        {
            // Check if T is ApiResponse<X> and default to IsSuccessStatusCode
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                return result =>
                {
                    var isSuccessProp = typeof(T).GetProperty(nameof(ApiResponse<object>.IsSuccessStatusCode));
                    return isSuccessProp?.GetValue(result) as bool? == true;
                };
            }

            // Default fallback: treat all results as successful
            return _ => true;
        }
    }

}
