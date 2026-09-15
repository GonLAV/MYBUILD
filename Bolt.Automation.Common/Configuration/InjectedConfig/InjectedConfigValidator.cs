using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Bolt.Automation.Common.Exceptions;

namespace Bolt.Automation.Common.Configuration.InjectedConfig
{
    /// <summary>
    /// Validates an <see cref="InjectedTestConfig"/> instance. Aggregates every failure into a
    /// single <see cref="InjectedConfigValidationException"/> so one re-run reveals the full set
    /// of missing/invalid env vars.
    ///
    /// Top-level <c>[EnvVar]</c> properties on <see cref="InjectedTestConfig"/> are always
    /// validated. Nested section properties (Adbx, PartnerPortal, D2C) are opt-in per test:
    /// pass the section property names that the test actually consumes via
    /// <paramref name="requiredSections"/>. Sections not in the set are skipped — a test that
    /// never touches D2C does not need <c>INJECTED_D2C_*</c> env vars.
    ///
    /// Error messages reference the explicit <see cref="EnvVarAttribute"/> name on each
    /// property — never the C# property name. QAs see the exact string they need to set.
    /// </summary>
    public static class InjectedConfigValidator
    {
        /// <summary>
        /// Validates an <see cref="InjectedTestConfig"/>, requiring only the named sections.
        /// Section names are property names on <see cref="InjectedTestConfig"/>
        /// (use <c>nameof(InjectedTestConfig.Adbx)</c> rather than string literals).
        /// </summary>
        public static void Validate(InjectedTestConfig config, IReadOnlySet<string> requiredSections)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(requiredSections);

            var errors = new List<string>();
            ValidateRoot(config, requiredSections, errors);

            if (errors.Count > 0)
                throw new InjectedConfigValidationException(errors);
        }

        /// <summary>
        /// Validates only top-level <c>[EnvVar]</c> properties (Tenant, Environment) — no sections.
        /// Equivalent to <c>Validate(config, EmptySet)</c>; provided for callers that don't need
        /// any section data (rare, but keeps the API symmetric).
        /// </summary>
        public static void Validate(InjectedTestConfig config) => Validate(config, EmptySet);

        private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>();

        private static void ValidateRoot(InjectedTestConfig config, IReadOnlySet<string> requiredSections, List<string> errors)
        {
            foreach (var prop in typeof(InjectedTestConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var envAttr = prop.GetCustomAttribute<EnvVarAttribute>();
                if (envAttr is not null)
                {
                    // Top-level [EnvVar] property (Tenant, Environment) — always validated.
                    ValidateProperty(config, prop, envAttr, errors);
                    continue;
                }

                // Section property. Validate its inner [EnvVar] properties only if this section
                // was opted-in by the test. Skip primitives/strings/value types.
                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) continue;
                if (!prop.CanRead) continue;
                if (!requiredSections.Contains(prop.Name)) continue;

                var sub = prop.GetValue(config);
                if (sub is not null) ValidateSection(sub, errors);
            }
        }

        private static void ValidateSection(object instance, List<string> errors)
        {
            foreach (var prop in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var envAttr = prop.GetCustomAttribute<EnvVarAttribute>();
                if (envAttr is not null) ValidateProperty(instance, prop, envAttr, errors);
            }
        }

        private static void ValidateProperty(object instance, PropertyInfo prop, EnvVarAttribute envAttr, List<string> errors)
        {
            var value = prop.GetValue(instance) as string ?? "";
            var hasRequired = prop.GetCustomAttribute<RequiredAttribute>() is not null;
            var hasUrl = prop.GetCustomAttribute<UrlAttribute>() is not null;

            if (hasRequired && string.IsNullOrEmpty(value))
            {
                errors.Add($"{envAttr.Name} must be set.");
                return; // No point checking format if the value is missing.
            }

            if (hasUrl && !string.IsNullOrEmpty(value))
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    errors.Add($"{envAttr.Name} must be a valid absolute http(s) URL (got: \"{value}\").");
                }
            }
        }
    }
}
