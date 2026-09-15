using System.Text.Json;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.Secrets;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.Common.Services.Secrets;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Bolt.Automation.Tests.Tests.DevOps
{
    /// <summary>
    /// Unit-level verification for <see cref="BoltSecretsConfigurationProvider"/>.
    /// Sibling to <see cref="DevOpsVerificationTests"/> (same conventions) but does NOT
    /// require a real bundle, a browser, or DB access — it drives the provider against
    /// in-memory fixtures written to a temp dir, so it is safe to run anywhere in CI.
    ///
    /// Use filter: --filter "FullyQualifiedName~BoltSecretsProviderTests"
    /// </summary>
    [Category("devops")]
    [Tenant(Tenant.BOLTAG)]
    public class BoltSecretsProviderTests
    {
        private static string _fixtureDir = null!;

        private const string FixtureJson = """
            {
              "environments": {
                "qa": {
                  "tenants": {
                    "BOLTAG": {
                      "PlatformSqlConnectionString": "Server=qa-db;Database=Main_BOLTAG",
                      "AuditSqlConnectionString": "Server=qa-db;Database=Audit_BOLTAG"
                    }
                  },
                  "appSecrets": {
                    "OutlookClient":      { "ClientSecret": "qa-outlook-secret" },
                    "MongoReporting":     { "ConnectionString": "mongodb://qa-host/nexusAutomation" },
                    "Aws":                { "AccessKey": "TEST-ACCESS-000001", "SecretKey": "qa-aws-secret-key" },
                    "BoltInfrastructure": { "Authentication": { "secret": "qa-hmac-secret-hex" } },
                    "LDClient":           { "SdkKey": "sdk-qa-111" },
                    "Database":           { "USAA": { "BasicAuth": "dXNhYXFhYmFzZQ==" }, "NATGAN": { "BasicAuth": "bmF0Z2FucWE=" } }
                  },
                  "userSecrets": {
                    "BOLTAG": {
                      "ServiceAgent": { "Password": "qa-svc-pass", "ApiKey": "qa-svc-apikey", "AgentIdentity": "qa-svc-identity" },
                      "ConsumerOrganicPL": { "ApiKey": "qa-consumer-apikey" }
                    }
                  },
                  "twilio": {
                    "BOLTAG": { "AuthToken": "qa-twilio-token", "AccountSid": "qa-twilio-sid" }
                  }
                },
                "uat": {
                  "tenants": {
                    "BOLTAG": {
                      "PlatformSqlConnectionString": "Server=uat-db;Database=Main_BOLTAG"
                    }
                  },
                  "appSecrets": {
                    "OutlookClient":      { "ClientSecret": "uat-outlook-secret" },
                    "LDClient":           { "SdkKey": "sdk-uat-222" },
                    "BoltInfrastructure": { "Authentication": { "secret": "uat-hmac-secret-hex" } }
                  },
                  "userSecrets": {
                    "BOLTAG": {
                      "ServiceAgent": { "ApiKey": "uat-svc-apikey" }
                    }
                  },
                  "twilio": {
                    "BOLTAG": { "AuthToken": "uat-twilio-token", "AccountSid": "uat-twilio-sid" }
                  }
                }
              }
            }
            """;

        [OneTimeSetUp]
        public static void SetUp()
        {
            _fixtureDir = Path.Combine(Path.GetTempPath(), $"bolt-secrets-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_fixtureDir);
            File.WriteAllText(
                Path.Combine(_fixtureDir, SecretsBundle.FileName),
                FixtureJson);
        }

        [OneTimeTearDown]
        public static void TearDown()
        {
            if (Directory.Exists(_fixtureDir))
                Directory.Delete(_fixtureDir, recursive: true);
        }

        [Test]
        [TestCaseId(00000015)]
        [Description("AC2: every relocated Tier-1 key resolves through IConfiguration for the active env")]
        public void QaAppSecrets_AllTier1KeysResolve()
        {
            var config = Build(_fixtureDir, "QA");

            Assert.That(config["OutlookClient:ClientSecret"],                    Is.EqualTo("qa-outlook-secret"));
            Assert.That(config["MongoReporting:ConnectionString"],               Is.EqualTo("mongodb://qa-host/nexusAutomation"));
            Assert.That(config["Aws:AccessKey"],                                 Is.EqualTo("TEST-ACCESS-000001"));
            Assert.That(config["Aws:SecretKey"],                                 Is.EqualTo("qa-aws-secret-key"));
            Assert.That(config["BoltInfrastructure:Authentication:secret"],      Is.EqualTo("qa-hmac-secret-hex"));
            Assert.That(config["LDClient:SdkKey"],                               Is.EqualTo("sdk-qa-111"));
            Assert.That(config["Database:USAA:BasicAuth"],                       Is.EqualTo("dXNhYXFhYmFzZQ=="));
            Assert.That(config["Database:NATGAN:BasicAuth"],                     Is.EqualTo("bmF0Z2FucWE="));
        }

        [Test]
        [TestCaseId(00000016)]
        [Description("AC4: the provider flattens only the active env — a UAT-only key never resolves under QA")]
        public void EnvIsolation_UatKeyDoesNotLeakIntoQa()
        {
            var config = Build(_fixtureDir, "QA");

            Assert.That(config["LDClient:SdkKey"], Is.Not.EqualTo("sdk-uat-222"),
                "UAT LD key must not appear in QA config");
            Assert.That(config["BoltInfrastructure:Authentication:secret"], Is.Not.EqualTo("uat-hmac-secret-hex"),
                "UAT auth secret must not appear in QA config");
        }

        [Test]
        [TestCaseId(00000017)]
        [Description("AC5: the tenants (DB) subtree is not surfaced as IConfiguration keys")]
        public void Provider_DoesNotFlattenTenants_ConnectionStringNotExposed()
        {
            var config = Build(_fixtureDir, "QA");

            // SecretsStore's subtree must not leak as IConfiguration keys
            Assert.That(config["tenants:BOLTAG:PlatformSqlConnectionString"], Is.Null,
                "DB connection strings from the tenants subtree must not appear in IConfiguration");
            Assert.That(config["PlatformSqlConnectionString"], Is.Null);
        }

        [Test]
        [TestCaseId(00000018)]
        [Description("AC5: adding appSecrets does not break SecretsStore deserialization of connection strings")]
        public void SecretsStore_StillLoadsConnectionStrings_AfterAppSecretsAdded()
        {
            // Proves the appSecrets block does not break the existing SecretsStore deserialization
            var json = File.ReadAllText(Path.Combine(_fixtureDir, SecretsBundle.FileName));
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var store = JsonSerializer.Deserialize<AutomationSecretsStore>(json, options);

            Assert.That(store, Is.Not.Null);
            Assert.That(store!.Environments, Contains.Key("qa"));
            var qaEnv = store.Environments["qa"];
            Assert.That(qaEnv.Tenants, Contains.Key("BOLTAG"));
            Assert.That(qaEnv.Tenants!["BOLTAG"].PlatformSqlConnectionString,
                Is.EqualTo("Server=qa-db;Database=Main_BOLTAG"));
        }

        [Test]
        [TestCaseId(00000019)]
        [Description("AC6: a null BOLT_SECRETS_PATH yields empty config without throwing (fails safe)")]
        public void NullPath_YieldsEmptyConfigWithoutThrowing()
        {
            var config = Build(null, "QA");
            Assert.That(config["OutlookClient:ClientSecret"], Is.Null);
        }

        [Test]
        [TestCaseId(00000020)]
        [Description("AC6: a missing optional bundle yields empty config without throwing (fails safe)")]
        public void MissingFile_Optional_YieldsEmptyConfigWithoutThrowing()
        {
            var config = Build(Path.Combine(_fixtureDir, "nonexistent-subdir"), "QA");
            Assert.That(config["OutlookClient:ClientSecret"], Is.Null);
        }

        [Test]
        [TestCaseId(00000021)]
        [Description("AC6: malformed bundle JSON throws InvalidOperationException (fails loud)")]
        public void MalformedJson_ThrowsInvalidOperationException()
        {
            var badDir = Path.Combine(Path.GetTempPath(), $"bolt-secrets-bad-{Guid.NewGuid():N}");
            Directory.CreateDirectory(badDir);
            File.WriteAllText(Path.Combine(badDir, SecretsBundle.FileName), "{ NOT VALID JSON !!!");

            try
            {
                Assert.Throws<InvalidOperationException>(() => Build(badDir, "QA"));
            }
            finally
            {
                Directory.Delete(badDir, recursive: true);
            }
        }

        [Test]
        [TestCaseId(00000022)]
        [Description("AC4/AC6: an env with no appSecrets block yields empty config without throwing")]
        public void MissingAppSecretsBlock_YieldsEmptyConfigWithoutThrowing()
        {
            // No appSecrets in "staging" env — should yield empty config gracefully
            var config = Build(_fixtureDir, "staging");
            Assert.That(config["OutlookClient:ClientSecret"], Is.Null);
        }

        [Test]
        [TestCaseId(00000023)]
        [Description("F2: Flatten decodes JSON string escapes via GetString() rather than returning the raw token")]
        public void Flatten_DecodesJsonEscapedStringValues()
        {
            // A secret value with a JSON-escaped double-quote must arrive decoded.
            // GetRawText().Trim('"') leaves the escape sequences raw; GetString() decodes them.
            var escapedDir = Path.Combine(Path.GetTempPath(), $"bolt-secrets-esc-{Guid.NewGuid():N}");
            Directory.CreateDirectory(escapedDir);

            // Write JSON where the secret contains a literal " — JSON-encoded as \"
            // File content:  "Secret": "pass\"word"  → decoded value: pass"word
            var json = "{\"environments\":{\"qa\":{\"appSecrets\":{\"SomeService\":{\"Secret\":\"pass\\\"word\"}}}}}";
            File.WriteAllText(Path.Combine(escapedDir, SecretsBundle.FileName), json);

            try
            {
                var config = Build(escapedDir, "QA");
                Assert.That(config["SomeService:Secret"], Is.EqualTo("pass\"word"),
                    "Flatten must decode JSON string escapes via GetString(), not return raw JSON token");
            }
            finally
            {
                Directory.Delete(escapedDir, recursive: true);
            }
        }

        [Test]
        [TestCaseId(00000024)]
        [Description("F1: a raw env alias ('Development') resolves the same bundle key SecretsStore uses ('dev')")]
        public void EnvAlias_Development_ResolvesToDevBundleKey()
        {
            // The bundle keys environments by the lowercase enum name. A caller passing the raw
            // ASPNETCORE_ENVIRONMENT alias 'Development' (as the WorkerAgent does) must still land
            // on the 'dev' block — otherwise app secrets silently resolve to null while
            // SecretsStore (which uses the Environment enum → 'dev') finds them.
            var dir = Path.Combine(Path.GetTempPath(), $"bolt-secrets-dev-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            var json = "{\"environments\":{\"dev\":{\"appSecrets\":{\"OutlookClient\":{\"ClientSecret\":\"dev-outlook-secret\"}}}}}";
            File.WriteAllText(Path.Combine(dir, SecretsBundle.FileName), json);

            try
            {
                Assert.That(Build(dir, "Development")["OutlookClient:ClientSecret"], Is.EqualTo("dev-outlook-secret"),
                    "Raw env alias 'Development' must resolve the 'dev' bundle block");
                Assert.That(Build(dir, "Dev")["OutlookClient:ClientSecret"], Is.EqualTo("dev-outlook-secret"),
                    "Canonical 'Dev' must resolve the 'dev' bundle block");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        // ---- Tier-2: user + Twilio secrets ----------------------------------------------------
        // These bind through the same AutomationSecretsStore model SecretsStore uses, so they prove
        // the schema + env isolation without needing the BOLT_SECRETS_PATH-bound singleton.

        private static AutomationSecretsStore DeserializeFixture()
        {
            var json = File.ReadAllText(Path.Combine(_fixtureDir, SecretsBundle.FileName));
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<AutomationSecretsStore>(json, options)!;
        }

        [Test]
        [TestCaseId(00000025)]
        [Description("Tier-2: userSecrets + twilio nodes bind for the active env, keyed by tenant/role")]
        public void UserAndTwilioSecrets_BindForActiveEnv()
        {
            var qa = DeserializeFixture().Environments!["qa"];

            var serviceAgent = qa.UserSecrets!["BOLTAG"]["ServiceAgent"];
            Assert.That(serviceAgent.Password, Is.EqualTo("qa-svc-pass"));
            Assert.That(serviceAgent.ApiKey, Is.EqualTo("qa-svc-apikey"));
            Assert.That(serviceAgent.AgentIdentity, Is.EqualTo("qa-svc-identity"));
            Assert.That(qa.UserSecrets["BOLTAG"]["ConsumerOrganicPL"].ApiKey, Is.EqualTo("qa-consumer-apikey"));

            Assert.That(qa.Twilio!["BOLTAG"].AuthToken, Is.EqualTo("qa-twilio-token"));
            Assert.That(qa.Twilio["BOLTAG"].AccountSid, Is.EqualTo("qa-twilio-sid"));
        }

        [Test]
        [TestCaseId(00000026)]
        [Description("Tier-2 AC4: a UAT user/Twilio credential never resolves under QA (env isolation)")]
        public void UserAndTwilioSecrets_EnvIsolation()
        {
            var store = DeserializeFixture();
            var qa = store.Environments!["qa"];
            var uat = store.Environments["uat"];

            Assert.That(qa.UserSecrets!["BOLTAG"]["ServiceAgent"].ApiKey, Is.EqualTo("qa-svc-apikey"));
            Assert.That(uat.UserSecrets!["BOLTAG"]["ServiceAgent"].ApiKey, Is.EqualTo("uat-svc-apikey"));
            Assert.That(qa.UserSecrets["BOLTAG"]["ServiceAgent"].ApiKey,
                Is.Not.EqualTo(uat.UserSecrets["BOLTAG"]["ServiceAgent"].ApiKey),
                "QA and UAT ServiceAgent API keys must not be the same value");

            Assert.That(qa.Twilio!["BOLTAG"].AuthToken, Is.Not.EqualTo(uat.Twilio!["BOLTAG"].AuthToken),
                "QA and UAT Twilio tokens must not be the same value");
        }

        [Test]
        [TestCaseId(00000027)]
        [Description("Tier-2: HydrateFrom overlays credentials onto a clone, preserves structure, and never mutates the shared static")]
        public void HydrateFrom_OverlaysCredentials_OnClone_WithoutMutatingOriginal()
        {
            var original = new UserTestDataCollection
            {
                ServiceAgent = new UserTestData
                {
                    Username = "Service@Agent.com",
                    Role = UserRole.ServiceAgent,
                    SendAgentIdentity = true
                },
                Admin = new UserTestData { Username = "admin@test.com", Role = UserRole.Admin }
            };

            var secrets = DeserializeFixture().Environments!["qa"].UserSecrets!["BOLTAG"];
            var hydrated = original.HydrateFrom(secrets);

            // Overlay applied to the clone
            Assert.That(hydrated.ServiceAgent!.ApiKey, Is.EqualTo("qa-svc-apikey"));
            Assert.That(hydrated.ServiceAgent.Password, Is.EqualTo("qa-svc-pass"));
            Assert.That(hydrated.ServiceAgent.AgentIdentity, Is.EqualTo("qa-svc-identity"));
            // Structural fields preserved
            Assert.That(hydrated.ServiceAgent.Username, Is.EqualTo("Service@Agent.com"));
            Assert.That(hydrated.ServiceAgent.SendAgentIdentity, Is.True);
            // A role with no bundle entry keeps its (empty) in-code credentials
            Assert.That(hydrated.Admin!.ApiKey, Is.Empty);
            Assert.That(hydrated.Admin.Username, Is.EqualTo("admin@test.com"));

            // The shared static instance must be untouched
            Assert.That(original.ServiceAgent!.ApiKey, Is.Empty,
                "HydrateFrom must overlay onto a clone, never the shared static instance");
            Assert.That(hydrated, Is.Not.SameAs(original));
            Assert.That(hydrated.ServiceAgent, Is.Not.SameAs(original.ServiceAgent));
        }

        [Test]
        [TestCaseId(00000028)]
        [Description("Tier-2: HydrateFrom(null) clones without overlay (bundle-not-loaded fallback)")]
        public void HydrateFrom_NullSecrets_ClonesWithoutOverlay()
        {
            var original = new UserTestDataCollection
            {
                Agent = new UserTestData { Username = "agent@test.com", ApiKey = "in-code" }
            };

            var hydrated = original.HydrateFrom(null);

            Assert.That(hydrated.Agent!.ApiKey, Is.EqualTo("in-code"));
            Assert.That(hydrated.Agent, Is.Not.SameAs(original.Agent));
        }

        private static IConfigurationRoot Build(string? path, string env) =>
            new ConfigurationBuilder()
                .AddBoltSecrets(path, env)
                .Build();
    }
}
