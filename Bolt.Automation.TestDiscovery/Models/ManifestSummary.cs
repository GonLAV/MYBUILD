namespace Bolt.Automation.TestDiscovery.Models;

/// <summary>
/// Statistical summary of discovered tests, including breakdowns by various dimensions.
/// </summary>
public class ManifestSummary
{
    /// <summary>
    /// Total number of test methods discovered.
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Number of tests that have [TestCaseId] attribute.
    /// These can be automatically linked to Azure Test Plans.
    /// </summary>
    public int TestsWithTestCaseId { get; set; }

    /// <summary>
    /// Number of tests missing [TestCaseId] attribute.
    /// These cannot be automatically linked and require manual mapping.
    /// </summary>
    public int TestsWithoutTestCaseId { get; set; }

    /// <summary>
    /// Number of tests by tenant (BOLTAG, KRAFTLAKEX, USAA, etc.).
    /// Key = tenant name, Value = count of tests
    /// </summary>
    public Dictionary<string, int> TestsByTenant { get; set; } = new();

    /// <summary>
    /// Number of tests by category (API, D2C, Sanity, Regression, etc.).
    /// Key = category name, Value = count of tests
    /// </summary>
    public Dictionary<string, int> TestsByCategory { get; set; } = new();

    /// <summary>
    /// Number of tests by base class (TestBase, UITestBase).
    /// Indicates what type of infrastructure tests use.
    /// Key = base class name, Value = count of tests
    /// </summary>
    public Dictionary<string, int> TestsByBaseClass { get; set; } = new();

    /// <summary>
    /// Number of [Test] vs parameterized tests.
    /// Key = "Test" or "Parameterized", Value = count
    /// </summary>
    public Dictionary<string, int> TestsByType { get; set; } = new();

    /// <summary>
    /// List of tests that don't have [TestCaseId] attribute.
    /// These require manual linking to Azure Test Plans.
    /// </summary>
    public List<UnmappedTest> UnmappedTests { get; set; } = new();

    /// <summary>
    /// Total estimated execution time for all tests (optional).
    /// Currently not implemented.
    /// </summary>
    public TimeSpan? TotalEstimatedDuration { get; set; }
}
