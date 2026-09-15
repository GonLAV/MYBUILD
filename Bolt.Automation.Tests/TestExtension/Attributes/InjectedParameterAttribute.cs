namespace Bolt.Automation.Tests.TestExtension.Attributes
{
    /// <summary>
    /// Marks a test method whose <c>TestCaseSource</c> is driven by single-value runtime-injected
    /// environment variables (e.g. <c>INJECTED_PS_STATE</c>, <c>INJECTED_PS_LOB</c>), instead of a
    /// fixed, framework-owned data set. One marker per fan-out axis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two independent consumers key off this attribute — both by its type NAME string
    /// ("InjectedParameterAttribute"), not by an assembly reference:
    /// </para>
    /// <list type="bullet">
    /// <item>The orchestrator's discovery tool reflects the attribute (by name) off each discovered
    /// method and collects EVERY marker's constructor argument into
    /// <c>DiscoveredTest.parameterEnvVars</c>, enabling work-item fan-out across the cross-product
    /// of the values selected for each axis. The values an axis may take come from
    /// <see cref="Bolt.Automation.Tests.TestExtension.Helpers.InjectedParameterCatalog"/> — an env
    /// var named here but absent there gets no picker, so add both together.</item>
    /// <item><see cref="Bolt.Automation.Tests.TestExtension.Helpers.TestMetadataResolver"/> uses its
    /// presence to gate composite TestIds ("{TestCaseId}_{argsSignature}") — only tests carrying this
    /// attribute get a composite id, so per-value variants don't collide on a single Mongo run record.</item>
    /// </list>
    /// <para>
    /// AllowMultiple: a test may fan out on more than one axis (e.g. INJECTED_PS_STATE AND
    /// INJECTED_PS_LOB). Order is not significant — each marker names one independent env var,
    /// and the orchestrator takes the cross-product of the values selected for each. Anything
    /// reading this attribute off a method must use GetCustomAttributes (plural):
    /// GetCustomAttribute&lt;T&gt;() throws AmbiguousMatchException once a method carries two.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class InjectedParameterAttribute : Attribute
    {
        // The env-var name is consumed only by the orchestrator's discovery tool, which reads the
        // raw constructor argument via CustomAttributeData — nothing in this repo reads it, so it
        // is deliberately not stored.
        public InjectedParameterAttribute(string envVarName) { }
    }
}
