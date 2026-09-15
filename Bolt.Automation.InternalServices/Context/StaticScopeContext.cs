using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;

namespace Bolt.Automation.InternalServices.Context
{
    internal class StaticScopeContext(IDictionary<string, StringValues>? headers) : ScopeContext
    {
        public static StaticScopeContext GetEmpty() => new();

        private readonly IDictionary<string, StringValues> _staticHeaders = headers ?? new Dictionary<string, StringValues>();
        private readonly ConcurrentDictionary<string, object?> _staticScopeData = new();

        public StaticScopeContext()
            : this(null)
        {

        }

        public override bool IsEmpty => !_staticHeaders.Any();

        protected override IDictionary<string, StringValues> Headers => _staticHeaders;

        protected override ConcurrentDictionary<string, object?> TestData => _staticScopeData;

        public override void StartAsyncChildScope()
        {
            throw new NotSupportedException("StaticScopeContext does not support ChildScope");
        }
    }
}
