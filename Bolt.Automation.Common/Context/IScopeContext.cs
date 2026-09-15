using System.Linq.Expressions;

namespace Bolt.Automation.Common.Context
{
    public interface IScopeContext
    {
        bool IsEmpty { get; }
        string? Tenant { get; }
        string? CorrelationId { get; }
        void StartAsyncChildScope();
        string? GetTestValue(string key);
        T? GetTestValue<T>(string key);
        bool TryGetTestValue(string key, out string? value);
        bool TryGetTestValue<T>(string key, out T? value);
        void SetTestData<T>(string key, T? data);
        bool ContainsTestData(string key);
        bool TryGetTestData<T>(string key, out T? value);
        T? GetTestData<T>(string key);

        // Strongly-typed context model
        TestContextData Data { get; set; }

        // Type-safe property accessors
        void Set<T>(Expression<Func<TestContextData, T>> property, T value);
        T Get<T>(Expression<Func<TestContextData, T>> property);
    }
}
