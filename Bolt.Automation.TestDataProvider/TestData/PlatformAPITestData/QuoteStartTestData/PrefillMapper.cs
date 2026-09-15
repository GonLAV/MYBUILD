using System.Reflection;

namespace Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData.QuoteStartTestData
{
    /// <summary>
    /// Centralized, cached reflection-based mapper from strongly-typed prefill models to dictionary keys
    /// </summary>
    internal static class PrefillMapper
    {
        private static readonly PropertyInfo[] _policyProps = typeof(PolicyDataPrefill).GetProperties();
        private static readonly PropertyInfo[] _customProps = typeof(CustomFieldsPrefill).GetProperties();

        /// <summary>
        /// Builds a dictionary of prefill key/value pairs from the given strongly-typed models
        /// </summary>
        internal static Dictionary<string,string> ToDictionary(PolicyDataPrefill policy, CustomFieldsPrefill custom)
        {
            var dict = new Dictionary<string, string>(capacity: 32);

            foreach (var p in _policyProps)
            {
                var v = p.GetValue(policy);
                if (v != null)
                {
                    dict[$"PolicyData.{p.Name}"] = v.ToString()!;
                }
            }

            foreach (var p in _customProps)
            {
                var v = p.GetValue(custom);
                if (v == null) continue;
                var keyBase = p.Name.StartsWith("ABTest")
                    ? $"PolicyData/CustomFields/{p.Name.Replace('_','-')}"
                    : $"PolicyData.CustomFields.{p.Name}";
                dict[keyBase] = v.ToString()!;
            }

            return dict;
        }
    }
}
