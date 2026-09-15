namespace Bolt.Automation.TestDiscovery.Models;

/// <summary>
/// Root object representing the complete test manifest for Azure DevOps test linking.
/// Contains all discovered tests and summary statistics.
/// </summary>
public class TestManifest
{
    /// <summary>
    /// Version of the manifest schema. Used for compatibility tracking.
    /// </summary>
    public string ManifestVersion { get; set; } = "1.0";

    /// <summary>
    /// UTC timestamp when the manifest was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Name of the test assembly that was scanned.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Version of the test assembly.
    /// </summary>
    public string AssemblyVersion { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the assembly that was scanned.
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Collection of all discovered test methods.
    /// </summary>
    public List<TestInfo> Tests { get; set; } = new();

    /// <summary>
    /// Statistical summary of the discovered tests.
    /// </summary>
    public ManifestSummary Summary { get; set; } = new();
}
