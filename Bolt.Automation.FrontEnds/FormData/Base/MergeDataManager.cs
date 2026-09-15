using Bolt.Automation.Common.Context;

namespace Bolt.Automation.FrontEnds.FormData.Base
{
    public static class MergeDataManager
    {
        public static Dictionary<string, string> MergeFormData(
            Dictionary<string, object>? flowDefaults = null,
            Dictionary<string, string>? userOverrides = null,
            Dictionary<string, string>? fieldRegistryDefaults = null)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Apply in priority order: registry defaults → flow defaults → user overrides
            AddIfNotNull(merged, fieldRegistryDefaults);
            AddIfNotNull(merged, flowDefaults);
            AddIfNotNull(merged, userOverrides);

            return merged;
        }

        public static Dictionary<string, string> GetSmartFormData(
            object pageInstance,
            IScopeContext scopeContext,
            Dictionary<string, string>? userInput,
            Dictionary<string, UIElement> fields)
        {
            var pageType = pageInstance.GetType();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (fieldName, fieldConfig) in fields.Where(kvp => kvp.Value.Pages?.Contains(pageType) == true))
            {
                result[fieldName] = userInput?.TryGetValue(fieldName, out var userValue) == true
                    ? userValue
                    : fieldConfig.DefaultValue ?? string.Empty;
            }

            return result;
        }

        private static void AddIfNotNull(Dictionary<string, string> target, Dictionary<string, string>? source)
        {
            if (source == null) return;
            foreach (var (key, value) in source)
                target[key] = value ?? string.Empty;
        }

        private static void AddIfNotNull(Dictionary<string, string> target, Dictionary<string, object>? source)
        {
            if (source == null) return;
            foreach (var (key, value) in source)
                target[key] = value?.ToString() ?? string.Empty;
        }
    }
}
