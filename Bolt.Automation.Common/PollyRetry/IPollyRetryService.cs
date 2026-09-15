
namespace Bolt.Automation.Common.PollyRetry
{
    public interface IPollyRetryService
    {
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int timeOutSeconds = 70);
    }
}
