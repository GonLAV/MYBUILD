using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bolt.Automation.Common.Utils
{
    public static class JsonUtils
    {
        public static string FormatIfJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            try
            {
                var parsed = JToken.Parse(input);
                return parsed.ToString(Formatting.Indented);
            }
            catch
            {
                return input;
            }
        }
    }
}

