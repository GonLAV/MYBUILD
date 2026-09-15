namespace Bolt.Automation.Common.Utils
{
    public static class RetryHelper
    {
        public static async Task RunWithRetryAsync(Func<Task> action, int maxRetries = 3, int baseDelayMs = 1000, Action<Exception, int>? onRetry = null)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    onRetry?.Invoke(ex, i + 1);
                    if (i == maxRetries - 1)
                        throw;
                    await Task.Delay(baseDelayMs * (i + 1)); // Exponential backoff
                }
            }
        }
    }
}

