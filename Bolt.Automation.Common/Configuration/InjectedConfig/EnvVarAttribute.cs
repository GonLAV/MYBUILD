namespace Bolt.Automation.Common.Configuration.InjectedConfig
{
    /// <summary>
    /// Declares the explicit process-environment variable name that backs a property on
    /// <see cref="InjectedTestConfig"/> (or any nested section). The name written here is
    /// what QAs enter in the orchestrator's Test Variables page — there is NO naming
    /// convention or prefix-stripping logic. The string on this attribute IS the contract.
    /// </summary>
    /// <example>
    /// <code>
    /// [EnvVar("INJECTED_ADBX_LOGIN_URL"), Required, Url]
    /// public string LoginUrl { get; set; } = "";
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EnvVarAttribute : Attribute
    {
        public string Name { get; }

        public EnvVarAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Env var name must not be empty.", nameof(name));
            Name = name;
        }
    }
}
