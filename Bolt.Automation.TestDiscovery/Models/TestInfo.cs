namespace Bolt.Automation.TestDiscovery.Models;

/// <summary>
/// Represents a single test method with all its metadata for Azure Test Plans linking.
/// </summary>
public class TestInfo
{
    /// <summary>
    /// Azure DevOps Test Case ID from [TestCaseId] attribute.
    /// Null if test doesn't have the attribute.
    /// </summary>
    public int? TestCaseId { get; set; }

    /// <summary>
    /// Fully qualified test name: Namespace.ClassName.MethodName
    /// This is the unique identifier used by test runners.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class containing the test method.
    /// May include '+' for nested classes (e.g., "D2CTests+Condominium").
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the test method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the test (currently same as MethodName).
    /// Can be customized if [DisplayName] attribute is used.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace containing the test class.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Tenant designation from [Tenant] attribute.
    /// Examples: "BOLTAG", "KRAFTLAKEX", "USAA", etc.
    /// Null if test doesn't have tenant attribute.
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// List of category values from [Category] attributes.
    /// Examples: "API", "D2C", "Sanity", "Regression", etc.
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// True if test is parameterized (TestCase/TestCaseSource), false if a simple [Test].
    /// </summary>
    public bool IsParameterized { get; set; }

    /// <summary>
    /// Name of the base test class (e.g., "TestBase", "UITestBase").
    /// Indicates what infrastructure the test uses.
    /// </summary>
    public string BaseClass { get; set; } = "Unknown";

    /// <summary>
    /// Relative source file path (optional, requires PDB).
    /// Currently not implemented.
    /// </summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// Line number in source file (optional, requires PDB).
    /// Currently not implemented.
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Estimated test duration based on historical data (optional).
    /// Currently not implemented.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; set; }
}
