namespace Bolt.Automation.Common.PollyRetry
{
    public static class PollyRetryServiceExtensions
    {
        public static async Task<T> ExecuteWithExceptionAsync<T>(
            this IPollyRetryService pollyRetryService,
            Func<Task<T>> operation,
            int timeOutSeconds,
            string customErrorMessage)
        {
            try
            {
                var result = await pollyRetryService.ExecuteWithRetryAsync(operation, timeOutSeconds);

                // Polly returns last result (false) without throwing after retries exhaust
                if (result is bool boolResult && !boolResult)
                    throw new Exception(customErrorMessage);

                return result;
            }
            catch (Exception ex) when (ex.Message != customErrorMessage)
            {
                throw new Exception(customErrorMessage, ex);
            }
        }
    }
}
