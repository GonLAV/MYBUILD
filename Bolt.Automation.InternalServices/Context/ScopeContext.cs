using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using Bolt.Automation.Common.Context;
using Microsoft.Extensions.Primitives;

namespace Bolt.Automation.InternalServices.Context
{
    public abstract class ScopeContext : IScopeContext
    {
        protected const string _boltHeaderPrefix = "X-Bolt-";
        protected const string _tenantHeader = "X-Bolt-Tenant";
        protected const string _correlationIdHeader = "X-Bolt-CorrelationId";

        private readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters = new();

        public abstract void StartAsyncChildScope();

        public abstract bool IsEmpty { get; }
        protected abstract IDictionary<string, StringValues> Headers { get; }
        protected abstract ConcurrentDictionary<string, object?> TestData { get; }

        public string? Tenant => GetFirstHeaderValue(_tenantHeader);

        public string? CorrelationId => GetFirstHeaderValue(_correlationIdHeader);

        private string? GetFirstHeaderValue(string headerKey)
        {
            if (Headers.TryGetValue(headerKey, out var headerValue))
            {
                return headerValue.Count > 0
                    ? headerValue[0]
                    : null;
            }

            return null;
        }

        public bool ContainsTestData(string key)
        {
            return TestData.ContainsKey(key);
        }

        public T? GetTestData<T>(string key)
        {
            TryGetTestData<T>(key, out var value);
            return value;
        }

        public bool TryGetTestData<T>(string key, out T? data)
        {
            if (TestData.TryGetValue(key, out var value) && value is T currentData)
            {
                data = currentData;
                return true;
            }

            data = default;
            return false;
        }

        public string? GetTestValue(string key)
        {
            return GetFirstHeaderValue(_boltHeaderPrefix + key);
        }

        public T? GetTestValue<T>(string key)
        {
            var value = GetTestValue(key);

            if (value is null)
                return default;

            var converter = _typeConverters.GetOrAdd(typeof(T), TypeDescriptor.GetConverter);
            var testValue = converter.ConvertFromInvariantString(value);


            return testValue is null ? default : (T)testValue;
        }

        public void SetTestData<T>(string key, T? data)
        {
            TestData[key] = data;
        }


        public bool TryGetTestValue(string key, out string? value)
        {
            value = null;
            if (ContainsBoltKey(key))
            {
                value = GetTestValue(key);
                return true;
            }

            return false;
        }

        public bool TryGetTestValue<T>(string key, out T? value)
        {
            value = default;

            if (ContainsBoltKey(key))
            {
                value = GetTestValue<T>(key);
                return true;
            }

            return false;
        }

        public bool ContainsBoltKey(string key)
        {
            return Headers.ContainsKey(_boltHeaderPrefix + key);
        }

        protected static IDictionary<string, StringValues> ConvertToBoltHeader(IDictionary<string, string?>? boltValues)
        {
            if (boltValues is null)
                return new Dictionary<string, StringValues>();

            return boltValues
                .ToDictionary(e => _boltHeaderPrefix + e.Key, e => new StringValues(e.Value));
        }

        protected static IDictionary<string, StringValues> ConvertToBoltHeader(IDictionary<string, StringValues>? boltValues)
        {
            if (boltValues is null)
                return new Dictionary<string, StringValues>();

            return boltValues
                .ToDictionary(e => _boltHeaderPrefix + e.Key, e => e.Value);
        }

        // Strongly-typed context model instance
        public TestContextData Data { get; set; } = new TestContextData();

        // Type-safe property setter
        public void Set<T>(Expression<Func<TestContextData, T>> property, T value)
        {
            if (property.Body is MemberExpression memberExpr)
            {
                var propInfo = typeof(TestContextData).GetProperty(memberExpr.Member.Name);
                if (propInfo == null || !propInfo.CanWrite)
                    throw new InvalidOperationException($"Property '{memberExpr.Member.Name}' is not writable on TestContextData.");

                // Clear FieldRegistry cache when FrontEnd changes
                if (memberExpr.Member.Name == nameof(TestContextData.FrontEnd))
                {
                    SetTestData<Dictionary<string, object>>("__FieldRegistry_Cache__", null);
                }

                propInfo.SetValue(Data, value);
            }
            else
            {
                throw new ArgumentException("Expression must be a property access.", nameof(property));
            }
        }

        // Type-safe property getter
        public T Get<T>(Expression<Func<TestContextData, T>> property)
        {
            if (property.Body is MemberExpression memberExpr)
            {
                var propInfo = typeof(TestContextData).GetProperty(memberExpr.Member.Name);
                if (propInfo == null || !propInfo.CanRead)
                    throw new InvalidOperationException($"Property '{memberExpr.Member.Name}' is not readable on TestContextData.");
                return (T)propInfo.GetValue(Data);
            }
            throw new ArgumentException("Expression must be a property access.", nameof(property));
        }

    }
}
