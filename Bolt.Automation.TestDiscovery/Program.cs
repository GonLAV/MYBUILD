using System.Reflection;
using Bolt.Automation.TestDiscovery.Discovery;
using Bolt.Automation.TestDiscovery.Output;
using CommandLine;

namespace Bolt.Automation.TestDiscovery;

/// <summary>
/// Command-line test discovery tool for Azure Test Plans integration.
/// Scans test assemblies and generates manifests for automatic test linking.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(
                async options => await RunDiscovery(options),
                errors => Task.FromResult(1)
            );
    }

    private static async Task<int> RunDiscovery(CommandLineOptions options)
    {
        try
        {
            // Determine assembly path
            string assemblyPath = DetermineAssemblyPath(options.AssemblyPath);

            if (!File.Exists(assemblyPath))
            {
                Console.Error.WriteLine($"Error: Test assembly not found at path: {assemblyPath}");
                return 1;
            }

            // Load test assembly
            Assembly assembly = Assembly.LoadFrom(assemblyPath);

            // Discover tests
            var engine = new TestDiscoveryEngine();
            var manifest = engine.DiscoverTests(assembly);

            // Format output as JSON
            var formatter = new JsonOutputFormatter();
            var output = formatter.Format(manifest);

            // Write output
            if (string.IsNullOrEmpty(options.OutputPath))
            {
                // Write to stdout
                Console.WriteLine(output);
            }
            else
            {
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(options.OutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Write to file
                await File.WriteAllTextAsync(options.OutputPath, output);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");

            if (options.Verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }

            return 1;
        }
    }

    /// <summary>
    /// Determines the assembly path from command-line options or defaults.
    /// </summary>
    private static string DetermineAssemblyPath(string? specifiedPath)
    {
        if (!string.IsNullOrEmpty(specifiedPath))
        {
            return Path.GetFullPath(specifiedPath);
        }

        // Default: Look for Bolt.Automation.Tests.dll in the same directory
        var baseDir = AppContext.BaseDirectory;
        var defaultPath = Path.Combine(baseDir, "Bolt.Automation.Tests.dll");

        return defaultPath;
    }
}

/// <summary>
/// Command-line options for the test discovery tool.
/// </summary>
public class CommandLineOptions
{
    [Option('a', "assembly", Required = false, HelpText = "Path to the test assembly DLL. Defaults to Bolt.Automation.Tests.dll in current directory.")]
    public string? AssemblyPath { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output file path. If not specified, writes to stdout.")]
    public string? OutputPath { get; set; }

    [Option('v', "verbose", Required = false, Default = false, HelpText = "Enable verbose output (stack traces on errors).")]
    public bool Verbose { get; set; }
}
