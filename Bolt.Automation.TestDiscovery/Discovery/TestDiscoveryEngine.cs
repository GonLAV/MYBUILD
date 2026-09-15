using System.Reflection;
using Bolt.Automation.TestDiscovery.Models;
using Bolt.Automation.Tests.TestExtension.Attributes;
using NUnit.Framework;

namespace Bolt.Automation.TestDiscovery.Discovery;

/// <summary>
/// Core engine for discovering test methods and their metadata using reflection.
/// Scans test assemblies and extracts all relevant information for Azure Test Plans linking.
/// </summary>
public class TestDiscoveryEngine
{
    /// <summary>
    /// Discovers all test methods in the specified assembly and returns a complete manifest.
    /// </summary>
    /// <param name="testAssembly">The test assembly to scan</param>
    /// <returns>TestManifest containing all discovered tests and summary statistics</returns>
    public TestManifest DiscoverTests(Assembly testAssembly)
    {
        var tests = new List<TestInfo>();

        // Get all types in the assembly
        var types = testAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in types)
        {
            // Find test methods (marked with [Test] or [TestCase] or [TestCaseSource])
            var testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsTestMethod);

            foreach (var method in testMethods)
            {
                try
                {
                    var testInfo = ExtractTestInfo(type, method);
                    tests.Add(testInfo);
                }
                catch
                {
                    // Silently skip tests that fail to extract
                }
            }
        }

        var manifest = new TestManifest
        {
            AssemblyName = testAssembly.GetName().Name ?? "Unknown",
            AssemblyVersion = testAssembly.GetName().Version?.ToString() ?? "Unknown",
            AssemblyPath = testAssembly.Location,
            Tests = tests.OrderBy(t => t.FullyQualifiedName).ToList(),
            Summary = GenerateSummary(tests)
        };

        return manifest;
    }

    /// <summary>
    /// Determines if a method is a test method (has [Test], [TestCase], or [TestCaseSource] attribute).
    /// </summary>
    private bool IsTestMethod(MethodInfo method)
    {
        var attributes = method.GetCustomAttributes();
        return attributes.Any(a =>
        {
            var typeName = a.GetType().Name;
            return typeName == "TestAttribute" || typeName == "TestCaseAttribute" || typeName == "TestCaseSourceAttribute";
        });
    }

    /// <summary>
    /// Extracts all metadata from a test method and its containing class.
    /// </summary>
    private TestInfo ExtractTestInfo(Type type, MethodInfo method)
    {
        var testInfo = new TestInfo
        {
            TestCaseId = GetTestCaseId(method, type),
            FullyQualifiedName = $"{type.FullName}.{method.Name}",
            ClassName = type.Name,
            MethodName = method.Name,
            DisplayName = method.Name, // Could be enhanced to check for [DisplayName] attribute
            Namespace = type.Namespace ?? string.Empty,
            Tenant = GetTenant(method, type),
            Categories = GetCategories(method, type),
            IsParameterized = IsParameterizedTest(method),
            BaseClass = GetBaseClassName(type)
        };

        return testInfo;
    }

    /// <summary>
    /// Gets the TestCaseId from [TestCaseId] attribute.
    /// Checks method first, then walks up the class hierarchy.
    /// </summary>
    private int? GetTestCaseId(MethodInfo method, Type type)
    {
        // Check method attribute first
        var methodAttr = method.GetCustomAttribute<TestCaseIdAttribute>();
        if (methodAttr != null)
        {
            return methodAttr.TestCaseIdValue;
        }

        // Walk up the class hierarchy
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            var classAttr = currentType.GetCustomAttribute<TestCaseIdAttribute>();
            if (classAttr != null)
            {
                return classAttr.TestCaseIdValue;
            }
            currentType = currentType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Gets the Tenant designation from [Tenant] attribute.
    /// Checks method first, then walks up the class hierarchy.
    /// </summary>
    private string? GetTenant(MethodInfo method, Type type)
    {
        // Check method attribute first
        var methodAttr = method.GetCustomAttribute<TenantAttribute>();
        if (methodAttr != null)
        {
            return methodAttr.TenantValue.ToString();
        }

        // Walk up the class hierarchy
        var currentType = type;
        while (currentType != null && currentType != typeof(object))
        {
            var classAttr = currentType.GetCustomAttribute<TenantAttribute>();
            if (classAttr != null)
            {
                return classAttr.TenantValue.ToString();
            }
            currentType = currentType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Gets all category values from [Category] attributes.
    /// Combines attributes from both method and class.
    /// </summary>
    private List<string> GetCategories(MethodInfo method, Type type)
    {
        var categories = new HashSet<string>();

        // Get CategoryAttribute instances from method
        var methodCategories = method.GetCustomAttributes<CategoryAttribute>(false);
        foreach (var cat in methodCategories)
        {
            categories.Add(cat.Name);
        }

        // Get CategoryAttribute instances from class
        var classCategories = type.GetCustomAttributes<CategoryAttribute>(false);
        foreach (var cat in classCategories)
        {
            categories.Add(cat.Name);
        }

        return categories.OrderBy(c => c).ToList();
    }

    /// <summary>
    /// Determines if the test has parameterized data sources (TestCaseSource or TestCase).
    /// </summary>
    private bool IsParameterizedTest(MethodInfo method)
    {
        return method.GetCustomAttributes()
            .Any(a => a.GetType().Name == "TestCaseSourceAttribute" || a.GetType().Name == "TestCaseAttribute");
    }

    /// <summary>
    /// Gets the base test class name (TestBase or UITestBase).
    /// Walks up the inheritance hierarchy to find known base classes.
    /// </summary>
    private string GetBaseClassName(Type type)
    {
        var currentType = type.BaseType;
        while (currentType != null && currentType != typeof(object))
        {
            var name = currentType.Name;
            // Check for known base test classes
            if (name == "TestBase" || name == "UITestBase")
            {
                return name;
            }
            currentType = currentType.BaseType;
        }
        return "Unknown";
    }

    /// <summary>
    /// Generates summary statistics from the discovered tests.
    /// </summary>
    private ManifestSummary GenerateSummary(List<TestInfo> tests)
    {
        var summary = new ManifestSummary
        {
            TotalTests = tests.Count,
            TestsWithTestCaseId = tests.Count(t => t.TestCaseId.HasValue),
            TestsWithoutTestCaseId = tests.Count(t => !t.TestCaseId.HasValue)
        };

        // Group by tenant
        var testsByTenant = tests
            .Where(t => !string.IsNullOrEmpty(t.Tenant))
            .GroupBy(t => t.Tenant!)
            .OrderBy(g => g.Key);

        foreach (var group in testsByTenant)
        {
            summary.TestsByTenant[group.Key] = group.Count();
        }

        // Group by category (tests can have multiple categories)
        var categoryCounts = new Dictionary<string, int>();
        foreach (var test in tests)
        {
            foreach (var category in test.Categories)
            {
                if (!categoryCounts.ContainsKey(category))
                {
                    categoryCounts[category] = 0;
                }
                categoryCounts[category]++;
            }
        }
        summary.TestsByCategory = categoryCounts.OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Group by base class
        var testsByBaseClass = tests
            .GroupBy(t => t.BaseClass)
            .OrderBy(g => g.Key);

        foreach (var group in testsByBaseClass)
        {
            summary.TestsByBaseClass[group.Key] = group.Count();
        }

        // Group by test type (Test vs Parameterized)
        var testsByType = tests
            .GroupBy(t => t.IsParameterized ? "Parameterized" : "Test")
            .OrderBy(g => g.Key);

        foreach (var group in testsByType)
        {
            summary.TestsByType[group.Key] = group.Count();
        }

        // Collect unmapped tests (those without TestCaseId)
        summary.UnmappedTests = tests
            .Where(t => !t.TestCaseId.HasValue)
            .Select(t => new UnmappedTest
            {
                FullyQualifiedName = t.FullyQualifiedName,
                MethodName = t.MethodName,
                Reason = "Missing [TestCaseId] attribute",
                Tenant = t.Tenant,
                Categories = t.Categories
            })
            .OrderBy(u => u.FullyQualifiedName)
            .ToList();

        return summary;
    }
}
