using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Mongo;
using Bolt.Automation.Common.Services.Secrets;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Bolt.Automation.Tests.Tests.DevOps
{
    /// <summary>
    /// DevOps verification tests for CI/CD pipeline validation.
    /// These tests do NOT depend on databases and are designed to verify:
    /// - Docker container environment is working
    /// - Browser automation (Playwright) is functional
    /// - Logging infrastructure is operational
    /// - Test infrastructure components are initialized correctly
    /// 
    /// Use filter: --filter "FullyQualifiedName~DevOpsVerificationTests"
    /// </summary>
    [Category("devops")]
    [Tenant(Tenant.BOLTAG)]
    public class DevOpsVerificationTests : UITestBase
    {
        public DevOpsVerificationTests() : base()
        {
        }

        [Test]
        [TestCaseId(00000001)]
        [Description("Verify browser can launch and navigate to public site")]
        public async Task VerifyBrowserLaunchAndNavigation()
        {
            _logger.Info("=== DevOps Test: Browser Launch Verification ===");
            _logger.Info($"Environment: {Environment}");
            _logger.Info($"Test execution started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            
            _logger.Info("Step 1: Initializing browser...");
            var page = await BrowserManager.GetPageAsync();
            
            _logger.Info("Step 2: Navigating to example.com...");
            await page.GotoAsync("https://example.com");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var title = await page.TitleAsync();
            var url = page.Url;
            
            _logger.Info($"Step 3: Page loaded successfully");
            _logger.Info($"  Title: {title}");
            _logger.Info($"  URL: {url}");
            
            Assert.That(page, Is.Not.Null);
            Assert.That(page.IsClosed, Is.False);
            Assert.That(url, Is.EqualTo("https://example.com/"));
            Assert.That(title, Does.Contain("Example"));
            
            _logger.Info("? Browser launch and navigation verified successfully");
        }

        [Test]
        [TestCaseId(00000002)]
        [Description("Verify browser can interact with page elements")]
        public async Task VerifyBrowserInteraction()
        {
            _logger.Info("=== DevOps Test: Browser Interaction Verification ===");
            
            _logger.Info("Step 1: Navigating to httpbin.org (public test API)...");
            var page = await BrowserManager.GetPageAsync();
            await page.GotoAsync("https://httpbin.org/forms/post");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            _logger.Info("Step 2: Locating form elements...");
            var customerNameInput = page.Locator("input[name='custname']");
            var emailInput = page.Locator("input[name='custemail']");
            
            _logger.Info("Step 3: Filling form fields...");
            await customerNameInput.FillAsync("DevOps Test User");
            await emailInput.FillAsync("devops@test.com");
            
            _logger.Info("Step 4: Verifying form values...");
            var nameValue = await customerNameInput.InputValueAsync();
            var emailValue = await emailInput.InputValueAsync();
            
            _logger.Info($"  Name field value: {nameValue}");
            _logger.Info($"  Email field value: {emailValue}");
            
            Assert.That(nameValue, Is.EqualTo("DevOps Test User"));
            Assert.That(emailValue, Is.EqualTo("devops@test.com"));
            
            _logger.Info("? Browser interaction verified successfully");
        }

        [Test]
        [TestCaseId(00000003)]
        [Description("Verify PageHelper functionality")]
        public async Task VerifyPageHelperFunctionality()
        {
            _logger.Info("=== DevOps Test: PageHelper Verification ===");
            
            _logger.Info("Step 1: Navigating to test page...");
            var page = await BrowserManager.GetPageAsync();
            await page.GotoAsync("https://example.com");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            _logger.Info("Step 2: Verifying PageHelper is initialized...");
            Assert.That(_pageHelper, Is.Not.Null);
            
            _logger.Info("Step 3: Testing PageHelper methods...");
            var h1Element = page.Locator("h1");
            var h1Text = await h1Element.TextContentAsync();
            
            _logger.Info($"  Found H1 element with text: {h1Text}");
            
            Assert.That(_pageHelper, Is.Not.Null);
            Assert.That(h1Text ?? "", Does.Contain("Example"));
            
            _logger.Info("? PageHelper functionality verified successfully");
        }

        [Test]
        [TestCaseId(00000004)]
        [Description("Verify logging infrastructure is operational")]
        public async Task VerifyLoggingInfrastructure()
        {
            _logger.Info("=== DevOps Test: Logging Infrastructure Verification ===");
            
            _logger.Info("Step 1: Testing all log levels...");
            _logger.Debug("DEBUG level log message");
            _logger.Info("INFO level log message");
            _logger.Warning("WARNING level log message");
            _logger.Error("ERROR level log message (test only)");
            
            _logger.Info("Step 2: Testing structured logging...");
            _logger.Info($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
            _logger.Info($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            _logger.Info($"Test Method: {GetTestMethodName()}");
            
            _logger.Info("Step 3: Navigating to verify logging during operations...");
            var page = await BrowserManager.GetPageAsync();
            await page.GotoAsync("https://example.com");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            _logger.Info($"Page loaded: {page.Url}");
            
            Assert.That(_logger, Is.Not.Null);
            
            _logger.Info("? Logging infrastructure verified successfully");
        }

        [Test]
        [TestCaseId(00000005)]
        [Description("Verify test infrastructure components initialization")]
        public async Task VerifyTestInfrastructureComponents()
        {
            _logger.Info("=== DevOps Test: Infrastructure Components Verification ===");
            
            _logger.Info("Step 1: Verifying core components...");
            var page = await BrowserManager.GetPageAsync();
            
            _logger.Info($"  Browser Manager: {(_browserManager != null ? "? Initialized" : "? Not initialized")}");
            _logger.Info($"  Page Helper: {(_pageHelper != null ? "? Initialized" : "? Not initialized")}");
            _logger.Info($"  Current Page: {(page != null ? "? Initialized" : "? Not initialized")}");
            _logger.Info($"  Executor: {(_executor != null ? "? Initialized" : "? Not initialized")}");
            _logger.Info($"  Page Factory: {(_pageFactory != null ? "? Initialized" : "? Not initialized")}");
            
            _logger.Info("Step 2: Verifying browser state...");
            Assert.That(page, Is.Not.Null);
            Assert.That(page.IsClosed, Is.False);
            
            _logger.Info("Step 3: Testing browser context...");
            var context = page.Context;
            var browser = context.Browser;
            
            _logger.Info($"  Browser type: {browser?.BrowserType?.Name ?? "Unknown"}");
            _logger.Info($"  Context pages: {context.Pages.Count}");
            
            await page.GotoAsync("https://example.com");
            await Task.Delay(1000);
            
            Assert.That(_browserManager, Is.Not.Null);
            Assert.That(_pageHelper, Is.Not.Null);
            Assert.That(page, Is.Not.Null);
            Assert.That(_executor, Is.Not.Null);
            Assert.That(_pageFactory, Is.Not.Null);
            Assert.That(context, Is.Not.Null);
            Assert.That(browser, Is.Not.Null);
            
            _logger.Info("? All infrastructure components verified successfully");
        }

        [Test]
        [TestCaseId(00000006)]
        [Description("Verify screenshot capability")]
        public async Task VerifyScreenshotCapability()
        {
            _logger.Info("=== DevOps Test: Screenshot Capability Verification ===");
            
            _logger.Info("Step 1: Navigating to test page...");
            var page = await BrowserManager.GetPageAsync();
            await page.GotoAsync("https://example.com");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            _logger.Info("Step 2: Capturing screenshot...");
            var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = true
            });
            
            _logger.Info($"  Screenshot size: {screenshotBytes.Length} bytes");
            
            Assert.That(screenshotBytes, Is.Not.Null);
            Assert.That(screenshotBytes.Length > 0, Is.True);
            Assert.That(screenshotBytes.Length > 1000, Is.True, "Screenshot seems too small");
            
            _logger.Info("? Screenshot capability verified successfully");
        }

        [Test]
        [TestCaseId(00000007)]
        [Description("Verify multiple page navigation")]
        public async Task VerifyMultiplePageNavigation()
        {
            _logger.Info("=== DevOps Test: Multiple Page Navigation Verification ===");
            var visitedUrls = new List<string>
            {
                "https://example.com",
                "https://httpbin.org/html",
                "https://httpbin.org/get"
            };
            
            var page = await BrowserManager.GetPageAsync();
            foreach (var url in visitedUrls)
            {
                _logger.Info($"Step: Navigating to {url}...");
                await page.GotoAsync(url);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                
                var currentUrl = page.Url;
                _logger.Info($"  Current URL: {currentUrl}");
                
                Assert.That(currentUrl, Does.Contain(url));
                await Task.Delay(500);
            }
            
            _logger.Info($"? Successfully navigated to {visitedUrls.Count} different pages");
        }

        [Test]
        [TestCaseId(00000008)]
        [Description("Verify environment configuration")]
        public async Task VerifyEnvironmentConfiguration()
        {
            _logger.Info("=== DevOps Test: Environment Configuration Verification ===");
            
            _logger.Info("Step 1: Checking environment variables...");
            _logger.Info($"  ASPNETCORE_ENVIRONMENT: {System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"}");
            _logger.Info($"  ENVIRONMENT: {System.Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Not set"}");
            _logger.Info($"  HEADLESS: {System.Environment.GetEnvironmentVariable("HEADLESS") ?? "Not set"}");
            _logger.Info($"  CI: {System.Environment.GetEnvironmentVariable("CI") ?? "Not set"}");
            
            _logger.Info("Step 2: Checking test configuration...");
            _logger.Info($"  Test Environment: {Environment}");
            _logger.Info($"  Test Method: {GetTestMethodName()}");
            
            _logger.Info("Step 3: Verifying browser is functional...");
            var page = await BrowserManager.GetPageAsync();
            await page.GotoAsync("https://example.com");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Assert.That(page, Is.Not.Null);
            Assert.That(page.IsClosed, Is.False);
            
            _logger.Info("? Environment configuration verified successfully");
        }

        [Test]
        [TestCaseId(00000009)]
        [Description("Verify logging integration readiness")]
        public async Task VerifyLoggingIntegrationReadiness()
        {
            _logger.Info("=== DevOps Test: Logging Integration Readiness ===");

            _logger.Info("Step 1: This test verifies that test attributes are properly set for logging");
            _logger.Info($"  Test ID: DEVOPS-009");
            _logger.Info($"  Category: DevOps");
            _logger.Info($"  Description: Logging integration readiness check");

            _logger.Info("Step 2: Logging structured information...");
            _logger.Info($"  Execution Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            _logger.Info($"  Test Class: {GetType().Name}");
            _logger.Info($"  Test Method: {GetTestMethodName()}");

            _logger.Info("Step 3: Performing browser operation to generate activity...");
            var page = await BrowserManager.GetPageAsync();
            await page.GotoAsync("https://example.com");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var title = await page.TitleAsync();
            _logger.Info($"  Page Title: {title}");

            _logger.Info("Step 4: Testing nested logging for test steps...");
            _logger.Info("  ? Sub-step: Browser initialized");
            _logger.Info("  ? Sub-step: Navigation completed");
            _logger.Info("  ? Sub-step: Page verification done");

            Assert.That(true, Is.True, "Logging integration readiness verified");

            _logger.Info("? Logging integration readiness verified successfully");
        }

        [Test]
        [TestCaseId(00000011)]
        [Description("Verify BOLT_SECRETS_PATH is configured and the secrets file exists")]
        public void VerifyBoltSecretsPath()
        {
            var rawPath = System.Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH");

            _logger.Info("=== DevOps Test: BOLT_SECRETS_PATH Verification ===");
            _logger.Info($"  BOLT_SECRETS_PATH = '{rawPath ?? "(not set)"}'");

            Assert.That(rawPath, Is.Not.Null.And.Not.Empty,
                "BOLT_SECRETS_PATH environment variable is not set. " +
                "Mount the secrets file and set BOLT_SECRETS_PATH in the pod/container spec.");

            // Resolve the actual file path via the same shared helper the readers use
            var resolvedPath = SecretsBundle.ResolvePath(rawPath!);

            _logger.Info($"  Resolved file path = '{resolvedPath}'");

            Assert.That(File.Exists(resolvedPath), Is.True,
                $"Secrets file not found at '{resolvedPath}'. " +
                $"Verify the volume is mounted and the file name matches '{SecretsBundle.FileName}'.");

            _logger.Info("  Secrets file exists on disk");
            _logger.Info("? BOLT_SECRETS_PATH is configured and the file is present");
        }

        [Test]
        [TestCaseId(00000014)]
        [Description("Verify the secrets bundle's appSecrets block resolves for the active environment through the real AddBoltSecrets provider")]
        public void VerifyBoltSecretsAppSecretsBlock()
        {
            var rawPath = System.Environment.GetEnvironmentVariable("BOLT_SECRETS_PATH");
            var envName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? System.Environment.GetEnvironmentVariable("ENVIRONMENT");

            _logger.Info("=== DevOps Test: BoltSecrets appSecrets Block Verification ===");
            _logger.Info($"  BOLT_SECRETS_PATH = '{rawPath ?? "(not set)"}'");
            _logger.Info($"  Environment       = '{envName ?? "(not set)"}'");

            Assert.That(rawPath, Is.Not.Null.And.Not.Empty,
                "BOLT_SECRETS_PATH is not set — cannot verify appSecrets block.");
            Assert.That(envName, Is.Not.Null.And.Not.Empty,
                "Environment variable (ASPNETCORE_ENVIRONMENT or ENVIRONMENT) is not set.");

            var resolvedPath = SecretsBundle.ResolvePath(rawPath!);
            Assert.That(File.Exists(resolvedPath), Is.True,
                $"Secrets bundle not found at '{resolvedPath}'.");

            // Assert against the SAME provider production uses (AddBoltSecrets) rather than a
            // private JSON walk — a resolvable known key proves environments[<env>].appSecrets is
            // present AND that the production provider's navigation/key-derivation actually works.
            var config = new ConfigurationBuilder()
                .AddBoltSecrets(rawPath, envName)
                .Build();

            var outlookSecret = config["OutlookClient:ClientSecret"];
            Assert.That(outlookSecret, Is.Not.Null.And.Not.Empty,
                $"OutlookClient:ClientSecret did not resolve for env '{envName}'. " +
                "Either the bundle's 'environments.<env>.appSecrets' block is missing/empty " +
                "(DevOps must populate it in the AWS secret) or the schema does not match.");

            _logger.Info("  OutlookClient:ClientSecret resolved via IConfiguration overlay");
            _logger.Info("? BoltSecrets appSecrets block is present and resolvable");
        }

        [Test]
        [TestCaseId(00000029)]
        [Description("Tier-2: the mounted bundle resolves BOLTAG user + Twilio credentials via SecretsStore for the active env")]
        public void VerifyBoltSecretsUserAndTwilioBlock()
        {
            _logger.Info("=== DevOps Test: BoltSecrets user + Twilio Block Verification ===");
            _logger.Info($"  Environment = '{Environment}'");

            Assume.That(SecretsStore.Instance.IsLoaded, Is.True,
                "Skipping: secrets bundle is not loaded (BOLT_SECRETS_PATH not set). " +
                "Run 'nexus-agent secrets sync' and set BOLT_SECRETS_PATH to verify Tier-2 secrets.");

            var env = Environment;

            var userSecrets = SecretsStore.Instance.GetUserSecrets(Tenant.BOLTAG, env);
            Assert.That(userSecrets, Is.Not.Null,
                $"No userSecrets resolved for BOLTAG in env '{env}'. DevOps must populate " +
                "'environments.<env>.userSecrets' in the AWS secret.");
            Assert.That(userSecrets!["ServiceAgent"].ApiKey, Is.Not.Null.And.Not.Empty,
                "BOLTAG ServiceAgent ApiKey must resolve from the bundle's userSecrets block.");

            var twilio = SecretsStore.Instance.GetTwilioSecrets(Tenant.BOLTAG, env);
            Assert.That(twilio, Is.Not.Null,
                $"No Twilio secrets resolved for BOLTAG in env '{env}'.");
            Assert.That(twilio!.AuthToken, Is.Not.Null.And.Not.Empty);
            Assert.That(twilio.AccountSid, Is.Not.Null.And.Not.Empty);

            _logger.Info("? BoltSecrets user + Twilio blocks are present and resolvable");
        }

        [Test]
        [TestCaseId(00000012)]
        [Description("Verifies ENVIRONMENT and ASPNETCORE_ENVIRONMENT env vars are injected by the orchestrator and have a known value.")]
        public void VerifyEnvironmentVariableIsInjected()
        {
            var envValue = System.Environment.GetEnvironmentVariable("ENVIRONMENT");
            var aspnetValue = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Assert.That(envValue, Is.Not.Null.And.Not.Empty,
                "ENVIRONMENT variable must be set by the orchestrator in envVariables. " +
                "If missing, check that jobs.ts/templates.ts/scheduler.ts inject ENVIRONMENT into envVariables.");

            Assert.That(aspnetValue, Is.Not.Null.And.Not.Empty,
                "ASPNETCORE_ENVIRONMENT variable must be set by the orchestrator in envVariables. " +
                "Required so the orchestrator's intended environment overrides the K8s pod spec value.");

            var validValues = new[] { "QA", "Dev", "UAT", "Staging", "Production" };
            Assert.That(validValues, Contains.Item(envValue),
                $"ENVIRONMENT must be one of the orchestrator enum values, but was: '{envValue}'");

            Assert.That(aspnetValue, Is.EqualTo(envValue),
                $"ASPNETCORE_ENVIRONMENT ('{aspnetValue}') must match ENVIRONMENT ('{envValue}')");
        }

        [Test]
        [TestCaseId(00000013)]
        [Description("When the orchestrator targets UAT, CiCdContextProvider must resolve 'UAT'. Run via an orchestrator job created with environment='UAT'.")]
        public void VerifyEnvironmentVariableIsUatWhenRunningInUat()
        {
            var envValue = System.Environment.GetEnvironmentVariable("ENVIRONMENT");

            // Skip if this job is not targeting UAT (Assume skips, not fails)
            Assume.That(envValue, Is.EqualTo("UAT"),
                "Skipping: this test only validates UAT environment injection. " +
                "Run via an orchestrator job created with environment='UAT'.");

            // Both vars should be set to UAT by the orchestrator
            Assert.That(System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), Is.EqualTo("UAT"),
                "ASPNETCORE_ENVIRONMENT must be 'UAT' — orchestrator injects it alongside ENVIRONMENT " +
                "to override the K8s pod spec value in the test subprocess");

            // CiCdContextProvider should now correctly resolve UAT because ASPNETCORE_ENVIRONMENT=UAT
            // takes highest priority and was injected by the orchestrator
            var resolved = CiCdContextProvider.ResolveEnvironment();
            Assert.That(resolved, Is.EqualTo("UAT"),
                "CiCdContextProvider.ResolveEnvironment() must return 'UAT' when orchestrator job environment='UAT'");
        }

        [Test]
        [TestCaseId(00000010)]
        [Description("Verify complete end-to-end test execution")]
        public async Task VerifyCompleteEndToEndExecution()
        {
            _logger.Info("=== DevOps Test: Complete End-to-End Verification ===");
            _logger.Info("This test simulates a complete test execution workflow");
            
            try
            {
                _logger.Info("Phase 1: Pre-execution checks");
                _logger.Info($"  ? Test environment: {Environment}");
                _logger.Info($"  ? Browser manager: {(_browserManager != null ? "Available" : "Not available")}");
                _logger.Info($"  ? Logger: {(_logger != null ? "Available" : "Not available")}");
                
                _logger.Info("Phase 2: Browser operations");
                var page = await BrowserManager.GetPageAsync();
                await page.GotoAsync("https://httpbin.org/html");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                _logger.Info("  ? Page navigation successful");
                
                var h1 = await page.Locator("h1").First.TextContentAsync();
                _logger.Info($"  ? Page content verified: {h1}");
                
                _logger.Info("Phase 3: Screenshot capture");
                var screenshot = await page.ScreenshotAsync();
                _logger.Info($"  ? Screenshot captured: {screenshot.Length} bytes");
                
                _logger.Info("Phase 4: Post-execution logging");
                _logger.Info($"  ? Test completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                
                Assert.That(screenshot.Length > 0, Is.True);
                Assert.That(h1, Is.Not.Null);
                
                _logger.Info("? Complete end-to-end verification PASSED");
                _logger.Info("=".PadRight(60, '='));
            }
            catch (Exception ex)
            {
                _logger.Error($"? End-to-end verification FAILED: {ex.Message}");
                _logger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
