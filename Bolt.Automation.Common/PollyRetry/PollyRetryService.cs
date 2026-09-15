namespace Bolt.Automation.Common.PollyRetry
{
    public class PollyRetryService(PolicyProvider policyProvider) : IPollyRetryService
    {
        private readonly PolicyProvider _policyProvider = policyProvider;

        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int timeout = 70)
        {
            var policy = _policyProvider.GetAsyncPolicyGeneric<T>(timeout);
            return await policy.ExecuteAsync(operation);
        }
    }
}
