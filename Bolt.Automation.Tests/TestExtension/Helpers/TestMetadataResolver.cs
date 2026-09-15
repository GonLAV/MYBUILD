using System.Reflection;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.Tests.TestExtension.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Bolt.Automation.Tests.TestExtension.Helpers
{
    /// <summary>
    /// Resolves test identity, metadata, and outcome from NUnit TestContext.
    /// Caches reflection results since they don't change during a test's lifecycle.
    /// </summary>
    internal sealed class TestMetadataResolver
    {
        private readonly Type _testClassType;
        private Tenant? _cachedTenant;
        private bool _tenantResolved;
        private string? _cachedTestId;
        private int? _rawTestCaseId;

        private static int _testCounter;

        public TestMetadataResolver(Type testClassType)
        {
            _testClassType = testClassType;
        }

        public string DisplayName
            => TestContext.CurrentContext?.Test?.Name ?? MethodName;

        public string MethodName
            => TestContext.CurrentContext?.Test?.MethodName ?? "Unknown";

        public string TestId
        {
            get
            {
                if (_cachedTestId == null)
                {
                    var props = TestContext.CurrentContext?.Test?.Properties;
                    if (props != null && props.ContainsKey("TestCaseId"))
                    {
                        var val = props["TestCaseId"].FirstOrDefault();
                        if (val != null)
                        {
                            var propValue = val.ToString()!;
                            if (int.TryParse(propValue, out var parsedId))
                                _rawTestCaseId = parsedId;

                            _cachedTestId = BuildCompositeTestId(propValue) ?? propValue;
                            return _cachedTestId;
                        }
                    }
                    _cachedTestId = Interlocked.Increment(ref _testCounter).ToString();
                }
                return _cachedTestId;
            }
        }

        public Tenant? Tenant
        {
            get
            {
                if (!_tenantResolved)
                {
                    _cachedTenant = ResolveTenant();
                    _tenantResolved = _cachedTenant.HasValue;
                }
                return _cachedTenant;
            }
        }

        public TestRunMetadata BuildMetadata(string environment, string? lob, string? frontEnd)
        {
            var test = TestContext.CurrentContext?.Test;
            return new TestRunMetadata
            {
                TestCaseId = GetTestCaseId(),
                FullyQualifiedName = test?.FullName,
                ClassName = test?.ClassName,
                Categories = GetCategories(),
                Tenant = Tenant?.ToString(),
                Environment = environment,
                Lob = lob,
                FrontEnd = frontEnd,
                Description = GetDescription(),
                Attempt = GetAttemptNumber()
            };
        }

        // [RetryOnFailure] bumps CurrentRepeatCount per re-run, so attempt 1 reports 0.
        // Deliberately read here rather than inferring a retry from "a Mongo document already
        // exists": StartTestAsync sits behind a 10s write guard that fails silently, and a lost
        // first insert would make attempt 2 look like a first run.
        private static int GetAttemptNumber()
            => (TestExecutionContext.CurrentContext?.CurrentRepeatCount ?? 0) + 1;

        public (string Outcome, TestFailureData? FailureData) DetectOutcome()
        {
            var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
            var outcome = testStatus switch
            {
                TestStatus.Failed => "Failed",
                TestStatus.Inconclusive => "Skipped",
                TestStatus.Skipped => "Skipped",
                _ => "Passed"
            };

            TestFailureData? failureData = null;
            if (testStatus == TestStatus.Failed)
            {
                var rawMessage = TestContext.CurrentContext.Result.Message ?? "Test Failed";
                var strippedMessage = StripExceptionNamespace(rawMessage);
                var exceptionType = FailureFingerprintGenerator.ExtractExceptionType(strippedMessage);

                var finalMessage = exceptionType == FailureFingerprintGenerator.AssertionFailureType
                    ? $"{FailureFingerprintGenerator.AssertionFailureType} : {strippedMessage}"
                    : strippedMessage;

                failureData = new TestFailureData
                {
                    Messages = [finalMessage],
                    StackTraces = TestContext.CurrentContext.Result.StackTrace is { } st ? [st] : [],
                    ExceptionTypes = [exceptionType]
                };
            }
            else if (testStatus is TestStatus.Inconclusive or TestStatus.Skipped)
            {
                failureData = new TestFailureData
                {
                    Messages = [TestContext.CurrentContext.Result.Message ?? "Test skipped"]
                };
            }

            return (outcome, failureData);
        }

        private static string StripExceptionNamespace(string message) =>
            System.Text.RegularExpressions.Regex.Replace(message, @"[\w.]+\.(\w*Exception)\s*:", "$1 :");

        // Cached from the raw "TestCaseId" property value when the TestId getter reads it — must
        // NOT re-derive from TestId, which can hold a composite "{TestCaseId}_{argsSignature}"
        // string for [InjectedParameter]-marked tests (see BuildCompositeTestId).
        private int? GetTestCaseId() => _rawTestCaseId;

        // Builds "{TestCaseId}_{argsSignature}" for tests whose method carries
        // [InjectedParameter] AND whose NUnit-generated Test.Name has parenthesized arguments
        // (TestCaseSource cases, e.g. "MyTest(TX_Crowley)") — this is what keeps per-value work
        // items (one per injected env-var value, e.g. one per INJECTED_PS_STATE) from colliding
        // on a single Mongo run record. Every other test's TestId stays exactly the raw
        // TestCaseId/counter value (propValue), unchanged.
        private string? BuildCompositeTestId(string propValue)
        {
            var methodName = TestContext.CurrentContext?.Test?.MethodName;
            // GetCustomAttributes (plural) is required: [InjectedParameter] is AllowMultiple, and
            // the singular GetCustomAttribute<T>() throws AmbiguousMatchException on a method that
            // fans out on two axes (e.g. INJECTED_PS_STATE + INJECTED_PS_LOB).
            var hasInjectedParameter = methodName != null
                && _testClassType
                    .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetCustomAttributes<InjectedParameterAttribute>().Any() == true;

            if (!hasInjectedParameter)
                return null;

            var testName = TestContext.CurrentContext?.Test?.Name ?? string.Empty;
            var args = System.Text.RegularExpressions.Regex.Match(testName, @"\((.*)\)");
            if (!args.Success)
                return null;

            var sanitized = System.Text.RegularExpressions.Regex.Replace(args.Groups[1].Value, "[^A-Za-z0-9_-]", "_");
            return $"{propValue}_{sanitized}";
        }

        private Tenant? ResolveTenant()
        {
            // Try NUnit test properties first (available when TestContext is fully populated)
            var props = TestContext.CurrentContext?.Test?.Properties;
            if (props != null && props.ContainsKey("Tenant"))
            {
                var tenantStr = props["Tenant"].FirstOrDefault()?.ToString();
                if (tenantStr != null && Enum.TryParse<Tenant>(tenantStr, out var tenantEnum))
                    return tenantEnum;
            }

            // Fallback for constructor-time: NUnit properties may not be populated yet for all test types.
            // Reflection is only needed here — BuildMetadata and all other callers run in [SetUp].
            var methodName = TestContext.CurrentContext?.Test?.MethodName;
            var methodAttr = methodName != null
                ? _testClassType
                    .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetCustomAttribute<TenantAttribute>()
                : null;
            return methodAttr?.TenantValue ?? _testClassType.GetCustomAttribute<TenantAttribute>()?.TenantValue;
        }

        private List<string> GetCategories()
        {
            // Try NUnit test properties first (populated correctly for most test types)
            var props = TestContext.CurrentContext?.Test?.Properties;
            if (props != null && props.ContainsKey("Category"))
            {
                var categories = props["Category"]
                    .Select(c => c?.ToString())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Cast<string>()
                    .ToList();
                if (categories.Count > 0)
                    return categories;
            }

            // Fallback: reflect on the method and class — same pattern as GetDescription/ResolveTenant.
            // NUnit may not propagate method-level [Category] into individual parameterized test case
            // properties when running via certain adapters (e.g., VS Test Explorer locally).
            var methodName = TestContext.CurrentContext?.Test?.MethodName;
            var method = methodName != null
                ? _testClassType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : null;

            var reflectedCategories = (method?.GetCustomAttributes<CategoryAttribute>() ?? [])
                .Concat(_testClassType.GetCustomAttributes<CategoryAttribute>())
                .Select(a => a.Name)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            return reflectedCategories;
        }

        private string? GetDescription()
        {
            // Check test-case-level properties first (e.g., TestCaseData.SetDescription())
            var props = TestContext.CurrentContext?.Test?.Properties;
            if (props != null && props.ContainsKey("Description"))
            {
                var description = props["Description"].FirstOrDefault()?.ToString();
                if (!string.IsNullOrEmpty(description))
                    return description;
            }

            // NUnit does not propagate method-level [Description] into individual parameterized test case
            // properties; fall back to reflection on the method (same pattern as ResolveTenant).
            var methodName = TestContext.CurrentContext?.Test?.MethodName;
            if (methodName != null)
            {
                var descAttr = _testClassType
                    .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null)
                {
                    var description = descAttr.Properties["Description"].OfType<object>().FirstOrDefault()?.ToString();
                    if (!string.IsNullOrEmpty(description))
                        return description;
                }
            }

            return null;
        }
    }
}
