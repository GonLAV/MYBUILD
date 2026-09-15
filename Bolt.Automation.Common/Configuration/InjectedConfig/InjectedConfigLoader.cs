using System.Reflection;

namespace Bolt.Automation.Common.Configuration.InjectedConfig
{
    /// <summary>
    /// Walks the property graph of <see cref="InjectedTestConfig"/>, reads each
    /// <see cref="EnvVarAttribute"/> Name verbatim from the process environment, and assigns
    /// the value. No prefix stripping, no section separator parsing — the attribute string
    /// IS the env var name.
    /// </summary>
    public static class InjectedConfigLoader
    {
        /// <summary>
        /// Builds an <see cref="InjectedTestConfig"/> from the current process environment.
        /// Does NOT validate — the test base calls <see cref="InjectedConfigValidator.Validate"/>
        /// with its own required-sections set so unused sections don't gate the test.
        /// </summary>
        public static InjectedTestConfig Load()
        {
            var config = new InjectedTestConfig();
            BindFromEnvironment(config);
            return config;
        }

        /// <summary>
        /// Convenience: load and validate in one call, requiring no sections (top-level only).
        /// Most callers use <see cref="Load"/> + <see cref="InjectedConfigValidator.Validate(InjectedTestConfig, IReadOnlySet{string})"/>
        /// instead, so they can opt in to the sections they actually consume.
        /// </summary>
        public static InjectedTestConfig LoadAndValidateTopLevelOnly()
        {
            var config = Load();
            InjectedConfigValidator.Validate(config);
            return config;
        }

        /// <summary>
        /// Recursive reflection walk: for every public instance property:
        ///   * If it has [EnvVar], read that env var (if set) and assign it.
        ///   * Otherwise, if it's a non-string reference type holding a section instance, recurse.
        /// </summary>
        private static void BindFromEnvironment(object instance)
        {
            foreach (var prop in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var envAttr = prop.GetCustomAttribute<EnvVarAttribute>();
                if (envAttr is not null)
                {
                    if (!prop.CanWrite) continue;
                    var value = System.Environment.GetEnvironmentVariable(envAttr.Name);
                    // Leave the property at its default (empty string) when the env var is unset —
                    // the validator turns that into a clear "must be set" error rather than a silent null.
                    if (value is not null)
                        prop.SetValue(instance, value);
                    continue;
                }

                // Recurse only into nested config sections (reference types, non-string).
                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) continue;
                if (!prop.CanRead) continue;

                var sub = prop.GetValue(instance);
                if (sub is not null) BindFromEnvironment(sub);
            }
        }
    }
}
