using Bolt.Automation.Common.Logging.Core;
using Polly;
using Polly.Timeout;

namespace Bolt.Automation.Common.PollyRetry
{
    public class PolicyProvider(IAutomationLogger logger)
    {
        private readonly IAutomationLogger _logger = logger;

        public IAsyncPolicy<T> GetAsyncPolicyGeneric<T>(int timeOut)
        {
            // Calculate retry count so exponential backoff fits within the timeout.
            // Sum of waits: 2^1 + 2^2 + ... + 2^n = 2^(n+1) - 2 <= timeOut
            // Solving for n: n <= log2(timeOut + 2) - 1
            // Guard: ensure at least 1 retry and a meaningful timeout
            if (timeOut < 2)
                throw new ArgumentOutOfRangeException(nameof(timeOut), "Timeout must be at least 2 seconds.");

            int retryCount = Math.Max(1, (int)Math.Floor(Math.Log2(timeOut + 2) - 1));

            // Per-attempt timeout: divide the total budget across all attempts,
            // accounting for the sleep durations between retries.
            // Total sleep = 2^(n+1) - 2; remaining time is split across (retryCount + 1) attempts.
            double totalSleepSeconds = Math.Pow(2, retryCount + 1) - 2;
            double remainingBudget = timeOut - totalSleepSeconds;
            int attemptCount = retryCount + 1;
            int perAttemptTimeout = Math.Max(1, (int)Math.Floor(remainingBudget / attemptCount));

            var retryPolicy = Policy<T>
                .Handle<Exception>() // Handles all exceptions, including TimeoutRejectedException from inner policy
                .OrResult(result => ShouldRetry(result))
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timeSpan, attempt, context) =>
                    {
                        _logger.Info($"Attempt {attempt}/{retryCount}: Waiting {timeSpan.TotalSeconds}s before next retry. " +
                                     $"Error: {outcome.Exception?.Message ?? "Result did not meet condition"}");
                    }
                );

            // Inner per-attempt timeout — prevents a single attempt from hanging indefinitely
            var timeoutPolicy = Policy.TimeoutAsync<T>(TimeSpan.FromSeconds(perAttemptTimeout));

            // Outer total-budget timeout — hard cap on the entire retry chain
            var totalTimeoutPolicy = Policy.TimeoutAsync<T>(TimeSpan.FromSeconds(timeOut));

            // Policy execution order (outermost -> innermost):
            // totalTimeout -> retry -> perAttemptTimeout -> operation
            return totalTimeoutPolicy.WrapAsync(retryPolicy.WrapAsync(timeoutPolicy));

//            IAsyncPolicy<T>(returned)
//│
//|--- 1.totalTimeoutPolicy   <- outer hard cap on entire operation
//│       └── 2.retryPolicy  <- orchestrates retries with exponential backoff
//│               └── 3.timeoutPolicy  <- per - attempt timeout
//│                       └──  your operation(delegate)
        }

        private static bool ShouldRetry<T>(T result)
        {
            if (typeof(T) == typeof(bool))
                return result is bool boolResult && !boolResult;

            if (typeof(T) == typeof(int))
                return result is int intResult && intResult == 0;

            if (result == null)
                return true;

            return false;
        }
    }
}
