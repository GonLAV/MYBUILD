namespace Bolt.Automation.FrontEnds.FormData.Helpers
{
    public static class FieldRegistryUtils
    {
        public static Dictionary<string, object?> GetDefaultValues(Dictionary<string, UIElement> fields)
        {
            var defaults = new Dictionary<string, object?>();
            foreach (var (key, field) in fields)
            {
                if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    defaults[key] = field.DefaultValue;
                }
            }
            return defaults;
        }
    }
}
