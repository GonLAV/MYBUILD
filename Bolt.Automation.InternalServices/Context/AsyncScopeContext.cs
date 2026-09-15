using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;

namespace Bolt.Automation.InternalServices.Context
{
    internal class AsyncScopeContext : ScopeContext
    {
        private static string? _tenant;
        private static readonly AsyncLocal<IDictionary<string, StringValues>> _asyncLocalHeaders = new();
        private static readonly AsyncLocal<ConcurrentDictionary<string, object?>> _asyncLocalScopeData = new();

        private static readonly object _lockObject = new();

        public static void SetInitialTestValues()
        {
            if (string.IsNullOrWhiteSpace(_tenant))
                throw new InvalidOperationException("Tenant is not set");

            _asyncLocalScopeData.Value ??= new ConcurrentDictionary<string, object?>();
            _asyncLocalHeaders.Value = ConvertToBoltHeader(new Dictionary<string, string?>
            {
                { "Tenant", _tenant },
                { "CorrelationId", Guid.NewGuid().ToString() }
            });
        }

        public override void StartAsyncChildScope()
        {
            var parentCorrelationId = CorrelationId;

            _asyncLocalScopeData.Value = _asyncLocalScopeData.Value is null
                ? new ConcurrentDictionary<string, object?>()
                : new ConcurrentDictionary<string, object?>(_asyncLocalScopeData.Value);

            _asyncLocalHeaders.Value = new Dictionary<string, StringValues>(Headers);

        }

        public static void SetTestValues(IDictionary<string, string?>? testValues)
        {
            _asyncLocalScopeData.Value ??= new ConcurrentDictionary<string, object?>();
            _asyncLocalHeaders.Value = ConvertToBoltHeader(testValues);
        }

        public override bool IsEmpty => _asyncLocalHeaders.Value is null || !_asyncLocalHeaders.Value.Any();

        protected override IDictionary<string, StringValues> Headers =>
            _asyncLocalHeaders.Value ?? new Dictionary<string, StringValues>
            {
                { _tenantHeader, _tenant }
            };

        protected override ConcurrentDictionary<string, object?> TestData
        {
            get
            {
                lock (_lockObject)
                {
                    _asyncLocalScopeData.Value ??= new ConcurrentDictionary<string, object?>();
                    return _asyncLocalScopeData.Value;
                }
            }
        }
    }
}
