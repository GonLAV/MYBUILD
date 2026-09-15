using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;

namespace Bolt.Automation.FrontEnds.FormData.Base
{
    public static class ScopeContextExtensions
    {
        private const string FieldRegistryCacheKey = "__FieldRegistry_Cache__";

        /// <summary>
        /// Gets the field registry from cache or loads and caches it for the lifetime of the ScopeContext.
        /// This eliminates redundant registry lookups and improves performance.
        /// </summary>
        public static Dictionary<string, UIElement> GetOrCacheFieldRegistry(this IScopeContext? scopeContext)
        {
            if (scopeContext == null)
                throw new ArgumentNullException(nameof(scopeContext),
                    "ScopeContext is required to access field registry");

            // Try to get from cache first
            var cachedRegistry = scopeContext.GetTestData<Dictionary<string, UIElement>>(FieldRegistryCacheKey);
            if (cachedRegistry != null)
            {
                return cachedRegistry;
            }

            // Not cached, detect project and load registry
            var frontEndType = scopeContext.Data.FrontEnd;
            if (!frontEndType.HasValue)
            {
                throw new TestSetupException(
                    "Unable to detect project context. Please set project explicitly via ScopeContext: " +
                    "ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.YourProject)");
            }

            // Load the registry (only happens once per ScopeContext)
            var registry = LoadFieldRegistry(frontEndType.Value);

            // Cache it for future use within this ScopeContext
            scopeContext.SetTestData(FieldRegistryCacheKey, registry);

            return registry;
        }

        /// <summary>
        /// Direct registry loading (used internally)
        /// </summary>
        private static Dictionary<string, UIElement> LoadFieldRegistry(FrontEndType projectType)
        {
            if (FieldRegistryProvider.Registries.TryGetValue(projectType, out var registry))
            {
                return registry;
            }
            return FieldRegistryProvider.GetRegistry(projectType);
        }

        /// <summary>
        /// Clears the cached field registry (useful for testing or explicit context switches)
        /// </summary>
        public static void ClearFieldRegistryCache(this IScopeContext? scopeContext)
        {
            if (scopeContext != null && scopeContext.ContainsTestData(FieldRegistryCacheKey))
            {
                scopeContext.SetTestData<Dictionary<string, UIElement>>(FieldRegistryCacheKey, null);
            }
        }
    }
}
