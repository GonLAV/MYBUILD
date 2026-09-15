using Bolt.Automation.Common.Logging.Core;
using LinqToDB.Data;

namespace Bolt.Automation.InternalServices.Database.Extensions
{
    public static class QueryExtensions
    {
        public static async Task<T> ExecuteWithSqlLoggingOrFailAsync<T>(
           this Task<T> queryTask,
           DataConnection db,
           IAutomationLogger logger,
           string operationDescription,
           Func<T, bool>? isFailure = null)
        {
            try
            {
                var result = await queryTask;
                var sql = db.LastQuery;
                logger?.Debug($"{operationDescription}: {sql}");

                if (isFailure?.Invoke(result) ?? result == null)
                {
                    var errorMsg = $"Query '{operationDescription}' failed: result is null or invalid.";
                    logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }
                return result!;
            }
            catch (Exception ex)
            {
                logger.Error($"Error during '{operationDescription}': {ex.Message}");
                logger.LogException(ex, operationDescription);
                throw;
            }
        }
            public static async Task<T?> WaitForValueAsync<T>(
                Func<Task<T?>> query,
                Func<T?, bool> predicate,
                TimeSpan? timeout = null,
                TimeSpan? interval = null,
                CancellationToken ct = default)
            {
                timeout ??= TimeSpan.FromSeconds(10);
                interval ??= TimeSpan.FromSeconds(1);

                var deadline = DateTime.UtcNow.Add(timeout.Value);
                T? value = default;

                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    value = await query();
                    if (predicate(value))
                        return value;
                    await Task.Delay(interval.Value, ct);
                }

                return value;
            }
        }
    }

