using System.Collections.Concurrent;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.FormData.Helpers;

namespace Bolt.Automation.FrontEnds.InterviewFlowHelpers
{
    public static class FlowDefaultsProvider
    {
        private static readonly ConcurrentDictionary<FrontEndType, Dictionary<string, object?>> _cache = new();

        public static Dictionary<string, object?> GetFlowDefaults(FrontEndType frontEndType, params LobType[] lobs)
        {
            // Get cached defaults
            var defaults = _cache.GetOrAdd(frontEndType, ft =>
            {
                var fields = FieldRegistryProvider.GetRegistry(ft);
                return FieldRegistryUtils.GetDefaultValues(fields);
            });

            // Clone and add LOBs
            var result = new Dictionary<string, object?>(defaults);
            if (lobs.Length > 0)
                result[FieldNames.Lob] = string.Join(",", lobs.Select(l => l.ToToken()));

            return result;
        }
    }
}
