using Bolt.Automation.Common.Context;

namespace Bolt.Automation.FrontEnds.FormData.Base
{
    public static class ProjectContextManager
    {
        public static Dictionary<string, UIElement> GetFieldRegistry(IScopeContext? scopeContext) =>
            scopeContext.GetOrCacheFieldRegistry();
    }
}