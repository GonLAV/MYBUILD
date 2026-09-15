using System.Xml.Linq;
using Bolt.Automation.Common.Services.Secrets;
using Microsoft.Extensions.Configuration;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.Core
{
    public static class ConfigurationLoader
    {
        private static readonly object _lock = new object();
        private static string _cachedConfigDirectory;

        /// <summary>
        /// Loads configuration following the priority hierarchy (lowest to highest):
        /// 1. appsettings.json (base configuration)
        /// 2. appsettings.{Environment}.json (environment-specific)
        /// 3. Environment variables
        /// 4. Command-line arguments
        /// 5. Test parameters (from .runsettings files)
        /// 6. Local override files (highest priority)
        /// </summary>
        public static (IConfigurationRoot, Environment) LoadConfigurations(IDictionary<string, string> testParameters = null)
        {
            lock (_lock)
            {
                var runSettingsParameters = LoadRunSettingsParameters();
                var allTestParameters = MergeParameters(runSettingsParameters, testParameters);
                
                var environmentString = GetEnvironmentName(allTestParameters);
                var environment = ParseEnvironment(environmentString);
                var configDirectory = GetConfigDirectory();

                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(configDirectory);

                // Add configuration sources in priority order
                AddJsonFiles(configBuilder, configDirectory, environmentString);
                configBuilder.AddBoltSecrets(System.Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH"), environmentString);
                configBuilder.AddEnvironmentVariables();
                AddCommandLine(configBuilder);
                
                if (allTestParameters.Any())
                {
                    configBuilder.AddInMemoryCollection(allTestParameters!);
                }
                
                AddLocalOverrides(configBuilder, configDirectory);

                var configuration = configBuilder.Build();
                return (configuration, environment);
            }
        }

        /// <summary>
        /// Loads runsettings parameters from available sources
        /// </summary>
        private static Dictionary<string, string> LoadRunSettingsParameters()
        {
            // Check environment variables first (VS/dotnet test specified)
            var envFile = System.Environment.GetEnvironmentVariable("RunSettingsFilePath") ??
                         System.Environment.GetEnvironmentVariable("VSTest_SettingsFile");
            if (!string.IsNullOrEmpty(envFile) && File.Exists(envFile))
            {
                return LoadRunSettingsFile(envFile);
            }

            // Look for well-known files in common locations
            var searchDirs = new[] { Directory.GetCurrentDirectory(), GetConfigDirectory() };
            var fileNames = new[] { "local.runsettings", "test.runsettings", ".runsettings" };

            foreach (var dir in searchDirs.Where(Directory.Exists))
            {
                foreach (var fileName in fileNames)
                {
                    var filePath = Path.Combine(dir, fileName);
                    if (File.Exists(filePath))
                    {
                        return LoadRunSettingsFile(filePath);
                    }
                }
            }

            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Loads parameters from a runsettings XML file
        /// </summary>
        private static Dictionary<string, string> LoadRunSettingsFile(string filePath)
        {
            var parameters = new Dictionary<string, string>();

            try
            {
                var doc = XDocument.Load(filePath);
                var testRunParameters = doc.Descendants("TestRunParameters").FirstOrDefault();

                if (testRunParameters != null)
                {
                    foreach (var param in testRunParameters.Descendants("Parameter"))
                    {
                        var name = param.Attribute("name")?.Value;
                        var value = param.Attribute("value")?.Value;

                        if (!string.IsNullOrEmpty(name) && value != null)
                        {
                            parameters[name] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigurationLoader] Error reading runsettings file {filePath}: {ex.Message}");
            }

            return parameters;
        }

        /// <summary>
        /// Merges runsettings parameters with provided test parameters
        /// </summary>
        private static Dictionary<string, string> MergeParameters(
            Dictionary<string, string> runSettingsParameters, 
            IDictionary<string, string> testParameters)
        {
            var merged = new Dictionary<string, string>(runSettingsParameters);
            
            if (testParameters != null)
            {
                foreach (var kvp in testParameters)
                {
                    merged[kvp.Key] = kvp.Value; // Test parameters override runsettings
                }
            }

            return merged;
        }

        /// <summary>
        /// Adds JSON configuration files (appsettings.json and environment-specific)
        /// </summary>
        private static void AddJsonFiles(IConfigurationBuilder builder, string configDirectory, string environment)
        {
            // Base configuration
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Environment-specific configuration
            builder.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);
            
            // Try standard environment name fallback
            var standardEnv = MapToStandardEnvironmentName(environment);
            if (standardEnv != environment)
            {
                builder.AddJsonFile($"appsettings.{standardEnv}.json", optional: true, reloadOnChange: false);
            }
        }

        /// <summary>
        /// Adds command line arguments with standard switch mappings
        /// </summary>
        private static void AddCommandLine(IConfigurationBuilder builder)
        {
            try
            {
                var args = System.Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    var switchMappings = new Dictionary<string, string>
                    {
                        { "--environment", "Environment" },
                        { "--env", "Environment" },
                        { "--tenant", "Tenant" },
                        { "-e", "Environment" },
                        { "-t", "Tenant" }
                    };
                    builder.AddCommandLine(args, switchMappings);
                }
            }
            catch
            {
                // Ignore command line parsing errors
            }
        }

        /// <summary>
        /// Adds local override files (for development)
        /// </summary>
        private static void AddLocalOverrides(IConfigurationBuilder builder, string configDirectory)
        {
            var localFiles = new[] { "local.runsettings.json", "runsettings.local.json" };
            
            foreach (var fileName in localFiles)
            {
                if (File.Exists(Path.Combine(configDirectory, fileName)))
                {
                    builder.AddJsonFile(fileName, optional: true, reloadOnChange: true);
                    break; // Only load the first one found
                }
            }
        }

        /// <summary>
        /// Collects all test parameters for backward compatibility.
        /// This method is used by TestInfrastructure.
        /// </summary>
        public static Dictionary<string, string> CollectTestParameters()
        {
            var parameters = new Dictionary<string, string>();

            // Add environment variables
            foreach (var envVar in System.Environment.GetEnvironmentVariables().Keys)
            {
                var key = envVar.ToString();
                var value = System.Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(value))
                {
                    parameters[key] = value;
                }
            }

            // Add runsettings parameters (overrides environment variables)
            var runSettingsParams = LoadRunSettingsParameters();
            foreach (var kvp in runSettingsParams)
            {
                parameters[kvp.Key] = kvp.Value;
            }

            return parameters;
        }

        private static string GetConfigDirectory()
        {
            if (!string.IsNullOrEmpty(_cachedConfigDirectory))
            {
                return _cachedConfigDirectory;
            }

            // Check explicit path first
            var explicitPath = System.Environment.GetEnvironmentVariable("BOLT_CONFIG_PATH");
            if (!string.IsNullOrEmpty(explicitPath) && Directory.Exists(explicitPath))
            {
                _cachedConfigDirectory = explicitPath;
                return explicitPath;
            }

            // Search common locations for appsettings.json
            var searchDirs = new[]
            {
                Directory.GetCurrentDirectory(),
                GetTestAssemblyDirectory(),
                FindTestProjectDirectory()
            }.Where(d => !string.IsNullOrEmpty(d)).Distinct();

            foreach (var dir in searchDirs)
            {
                if (File.Exists(Path.Combine(dir, "appsettings.json")))
                {
                    _cachedConfigDirectory = dir;
                    return dir;
                }
            }

            // Fallback to current directory
            var currentDir = Directory.GetCurrentDirectory();
            _cachedConfigDirectory = currentDir;
            return currentDir;
        }

        private static string GetTestAssemblyDirectory()
        {
            try
            {
                var testAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name?.Contains("Tests") == true);
                
                if (testAssembly?.Location != null)
                {
                    return Path.GetDirectoryName(testAssembly.Location);
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        private static string FindTestProjectDirectory()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            
            // Go up directories looking for a test project
            while (current != null)
            {
                if (current.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            
            return null;
        }

        private static string GetEnvironmentName(IDictionary<string, string> testParameters)
        {
            // Check test parameters first
            var envKeys = new[] { "Environment", "ENVIRONMENT", "env", "ENV" };
            if (testParameters != null)
            {
                foreach (var key in envKeys)
                {
                    if (testParameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            // Check environment variables
            var envVars = new[] { "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "ENVIRONMENT", "ENV" };
            foreach (var envVar in envVars)
            {
                var value = System.Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            // Throw exception instead of defaulting to Development
            throw new InvalidOperationException(
                "Environment could not be determined. Please specify environment using one of the following methods:\n" +
                "1. Test parameters: Environment, ENVIRONMENT, env, or ENV\n" +
                "2. Environment variables: ASPNETCORE_ENVIRONMENT, DOTNET_ENVIRONMENT, ENVIRONMENT, or ENV\n" +
                "3. Command line arguments: --environment or --env\n" +
                "4. .runsettings file with Environment parameter");
        }

        /// <summary>
        /// Parses environment string and converts it to Environment enum
        /// </summary>
        private static Environment ParseEnvironment(string environmentString)
        {
            if (string.IsNullOrWhiteSpace(environmentString))
            {
                return Environment.Dev;
            }

            // Try direct enum parsing first
            if (Enum.TryParse<Environment>(environmentString, true, out var directResult))
            {
                return directResult;
            }

            // Handle common aliases and variations
            return environmentString.ToLowerInvariant() switch
            {
                "dev" or "development" => Environment.Dev,
                "qa" or "test" or "testing" => Environment.Qa,
                "uat" or "user acceptance" or "useracceptance" => Environment.Uat,
                "staging" or "stage" or "stg" => Environment.Staging,
                "prod" or "production" or "live" => Environment.Production,
                _ => Environment.Dev // Default fallback
            };
        }

        private static string MapToStandardEnvironmentName(string environment)
        {
            return environment.ToUpperInvariant() switch
            {
                "DEV" => "Development",
                "PROD" => "Production", 
                "STG" or "STAGE" => "Staging",
                "TEST" => "Testing",
                _ => environment // Keep original if no mapping
            };
        }
    }
}