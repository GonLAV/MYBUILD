namespace Bolt.Automation.TestDiscovery.Models;

/// <summary>
/// Represents a test that cannot be automatically linked to Azure Test Plans.
/// Used for reporting tests that need manual mapping.
/// </summary>
public class UnmappedTest
{
    /// <summary>
    /// Fully qualified test name.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Test method name for easier identification.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Reason why the test cannot be automatically linked.
    /// Example: "Missing [TestCaseId] attribute"
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Tenant designation if available (helps prioritize manual mapping).
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// Categories to help identify what needs to be mapped.
    /// </summary>
    public List<string> Categories { get; set; } = new();
}
