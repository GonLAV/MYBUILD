using System.Text.Json;
using System.Text.Json.Serialization;
using Bolt.Automation.TestDiscovery.Models;

namespace Bolt.Automation.TestDiscovery.Output;

/// <summary>
/// Formats test manifest as simplified JSON with only essential fields for Azure Test Plans integration.
/// </summary>
public class JsonOutputFormatter
{
    private readonly JsonSerializerOptions _options;

    public JsonOutputFormatter()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public string Format(TestManifest manifest)
    {
        // Create simplified output with only essential fields
        var simplifiedOutput = new
        {
            generatedAt = manifest.GeneratedAt,
            totalTests = manifest.Tests.Count,
            testsWithTestCaseId = manifest.Tests.Count(t => t.TestCaseId.HasValue),
            tests = manifest.Tests
                .Where(t => t.TestCaseId.HasValue) // Only include tests with TestCaseId
                .Select(t => new
                {
                    testCaseId = t.TestCaseId,
                    automationTestName = t.FullyQualifiedName,
                    tenant = t.Tenant
                })
                .ToList()
        };

        return JsonSerializer.Serialize(simplifiedOutput, _options);
    }
}
