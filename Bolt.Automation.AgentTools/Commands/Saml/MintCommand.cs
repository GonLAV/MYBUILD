using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Bolt.Automation.AgentTools.Browser.HostProcess; // ConfigLocator
using Bolt.Automation.ApiClients.SSO.Saml;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.RelayStates;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.Core.Infrastructure;
using Bolt.Automation.TestDataProvider.Context;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.AgentTools.Commands.Saml;

[Verb("mint", HelpText = "Mint a signed SAML assertion for a tenant/env and print the raw base64 SAMLResponse.")]
internal class MintOptions
{
    [Option("tenant", Required = true, HelpText = "Tenant enum value, e.g. PROGRESSIVEPL.")]
    public string Tenant { get; set; } = string.Empty;

    [Option("env", Required = true, HelpText = "Environment: Qa | Dev | Uat | Staging | Production.")]
    public string Env { get; set; } = string.Empty;

    [Option("user", Default = "Admin", HelpText = "Which user in the tenant's UserTestDataCollection carries the Sso data (Admin, Agent, Consumer, ...).")]
    public string User { get; set; } = "Admin";

    [Option("template", Default = "Saml2ResponseTemplate", HelpText = "SAML template: Saml2ResponseTemplate | Saml2ResponseTemplateWithGroupExternalId | SamlPartnerPortal.")]
    public string Template { get; set; } = "Saml2ResponseTemplate";

    [Option("valid-minutes", Default = 10, HelpText = "Minutes the assertion stays valid after IssueInstant, 1 to 1440. It cannot be renewed, so this must cover the consumer's whole run plus one worst-case request.")]
    public int ValidMinutes { get; set; } = 10;

    [Option("env-file", HelpText = "Write the assertion as KEY=value into this .env file instead of printing it. The file then holds a live credential in plaintext: keep it out of source control.")]
    public string? EnvFile { get; set; }

    [Option("env-key", Default = "SAML_RESPONSE", HelpText = "Variable name to write when --env-file is used.")]
    public string EnvKey { get; set; } = "SAML_RESPONSE";
}

/// <summary>
/// Mints a signed SAML assertion for a consumer that cannot sign one itself —
/// the k6 load harness, whose JavaScript runtime has no XML canonicalisation or
/// enveloped XML-DSig.
///
/// This is a thin wrapper over the path every nexus SSO request already takes
/// (SsoApiHandler -> SsoHelper -> SamlSigner), stopping at the assertion instead
/// of posting it. No crypto and no tenant data is duplicated, so the two cannot
/// drift.
/// </summary>
internal static class MintCommand
{
    // The signature does not cover RelayState and the caller supplies its own, so
    // only the SAMLResponse half of SsoHelper's form body is kept.
    private const string PlaceholderRelayState = "nexus-agent-mint";

    /// <summary>
    /// Ceiling on --valid-minutes (24h). The window cannot be renewed, so callers
    /// want it generous; without a cap, though, a typo mints an assertion valid
    /// for millennia and writes it to a file.
    /// </summary>
    private const int MaxValidMinutes = 1440;

    private sealed record MintResult(string Assertion, string Destination, string? NotBefore, string? NotOnOrAfter);

    /// <summary>Aborts the mint with a status, message and exit code for the caller.</summary>
    private sealed class MintFailure(string status, string message, int exitCode) : Exception(message)
    {
        public string Status { get; } = status;
        public int ExitCode { get; } = exitCode;
    }

    public static async Task<int> ExecuteAsync(MintOptions options)
    {
        if (!Enum.TryParse<Tenant>(options.Tenant, ignoreCase: true, out var tenant))
        {
            return await CommandBase.EmitErrorAsync("unknown_tenant", $"Unknown tenant '{options.Tenant}'.",
                exitCode: 3, detail: new { expected = Enum.GetNames<Tenant>() });
        }

        // Validated here because TestInfrastructure reports an unknown environment
        // as a FileNotFoundException for a missing appsettings file, which says
        // nothing about the real mistake. The k6 harness has a LOAD environment
        // that nexus does not, so this path gets hit for real.
        if (!Enum.TryParse<Bolt.Automation.Common.Environment>(options.Env, ignoreCase: true, out _))
        {
            return await CommandBase.EmitErrorAsync("unknown_environment", $"Unknown environment '{options.Env}'.",
                exitCode: 3, detail: new { expected = Enum.GetNames<Bolt.Automation.Common.Environment>() });
        }

        if (!Enum.TryParse<SamlTemplateType>(options.Template, ignoreCase: true, out var template))
        {
            return await CommandBase.EmitErrorAsync("unknown_template", $"Unknown SAML template '{options.Template}'.",
                exitCode: 3, detail: new { expected = Enum.GetNames<SamlTemplateType>() });
        }

        if (options.ValidMinutes is < 1 or > MaxValidMinutes)
        {
            return await CommandBase.EmitErrorAsync("invalid_valid_minutes",
                $"--valid-minutes must be between 1 and {MaxValidMinutes} (got {options.ValidMinutes}).", exitCode: 3);
        }

        try
        {
            // stdout is a DATA channel here — callers do `saml=$(nexus-agent saml
            // mint ...)`. The config bootstrap AND the lazily-loaded tenant data
            // collections both print "[BoltSecrets]/[SecretsStore] ..." to
            // Console.Out; left alone they ride along into that capture and the
            // assertion decodes as garbage. Park stdout on stderr for the whole
            // body: guarding only the bootstrap leaks, because the collections
            // load later, on first touch.
            var realStdout = Console.Out;
            Console.SetOut(Console.Error);
            MintResult result;
            try
            {
                result = Mint(options, tenant, template);
            }
            finally
            {
                Console.SetOut(realStdout);
            }

            return await EmitAsync(options, tenant, template, result);
        }
        catch (MintFailure failure)
        {
            return await CommandBase.EmitErrorAsync(failure.Status, failure.Message, failure.ExitCode);
        }
    }

    private static MintResult Mint(MintOptions options, Tenant tenant, SamlTemplateType template)
    {
        ConfigLocator.EnsureConfigPath();

        // The same DI graph a test builds, so URLs and users come from the one
        // source of truth (UrlDataStore / UserDataStore).
        var (config, environment) = TestInfrastructure.GetConfiguration(
            new Dictionary<string, string> { ["Tenant"] = options.Tenant, ["Environment"] = options.Env });

        var serviceProvider = TestInfrastructure.CreateServiceProvider(config, environment);
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var scopeContext = provider.GetRequiredService<IScopeContext>();
        scopeContext.StartAsyncChildScope();
        scopeContext.Set(ctx => ctx.Environment, environment);
        scopeContext.Set(ctx => ctx.Tenant, tenant);

        var accessor = provider.GetRequiredService<TestContextAccessor>();
        var urlCollection = accessor.CurrentUrlCollection;
        scopeContext.Set(ctx => ctx.UrlDataCollection, urlCollection);

        var user = ResolveUser(accessor.CurrentUserCollection, options.User)
            ?? throw new MintFailure("unknown_user",
                $"'{options.User}' is not a user on {tenant}/{environment}'s UserTestDataCollection.", 3);

        // Refuse to sign with empty Issuer/Audience/destination. The STS answers
        // such an assertion with an HTML login page, not a validation error, so
        // the cause would be invisible to whoever runs the consumer.
        var ssoUrl = urlCollection?.SsoApi;
        if (string.IsNullOrWhiteSpace(ssoUrl?.EndpointSso))
        {
            throw new MintFailure("missing_sso_url",
                $"UrlDataStore has no SsoApi.EndpointSso for {tenant}/{environment}.", 2);
        }

        if (string.IsNullOrWhiteSpace(user.Sso?.Issuer) || string.IsNullOrWhiteSpace(user.Sso?.Audience))
        {
            throw new MintFailure("missing_sso_user_data",
                $"User '{options.User}' on {tenant}/{environment} has no Sso.Issuer/Sso.Audience in UserDataStore.", 2);
        }

        var formBody = new SsoHelper().GetSsoSignedSaml(
            user, ssoUrl, new RelayStateTestData { Value = PlaceholderRelayState },
            template.ToString(), cryptographyService: null, validMinutes: options.ValidMinutes);

        var assertion = ExtractRawAssertion(formBody);
        var (notBefore, notOnOrAfter) = ReadWindow(assertion);

        return new MintResult(assertion, ssoUrl.EndpointSso!, notBefore, notOnOrAfter);
    }

    private static async Task<int> EmitAsync(
        MintOptions options, Tenant tenant, SamlTemplateType template, MintResult result)
    {
        if (!string.IsNullOrWhiteSpace(options.EnvFile))
        {
            WriteToEnvFile(options.EnvFile!, options.EnvKey, result.Assertion);

            // Summary only: the assertion went to the file, so nothing secret
            // reaches a build log that captures stdout.
            return await CommandBase.EmitJsonAsync(new
            {
                Status = "ok",
                Tenant = tenant.ToString(),
                Environment = options.Env,
                User = options.User,
                Template = template.ToString(),
                Destination = result.Destination,
                ValidMinutes = options.ValidMinutes,
                NotBefore = result.NotBefore,
                NotOnOrAfter = result.NotOnOrAfter,
                Length = result.Assertion.Length,
                EnvFile = options.EnvFile,
                EnvKey = options.EnvKey,
            });
        }

        // Metadata on stderr so it cannot contaminate a `$(...)` capture. Length
        // only — the assertion itself is never logged.
        await Console.Error.WriteLineAsync(
            $"[saml] {tenant}/{options.Env} user={options.User} template={template} " +
            $"valid={options.ValidMinutes}min not_on_or_after={result.NotOnOrAfter} length={result.Assertion.Length}");

        // Explicit "\n" rather than WriteLine: on Windows WriteLine emits CRLF and
        // the stray \r rides into `> file` redirects, breaking `base64 -d`.
        Console.Out.Write(result.Assertion + "\n");
        return 0;
    }

    /// <summary>
    /// Resolves a named user off the collection by property name, so the command
    /// follows whatever users a tenant defines without a hardcoded switch.
    /// </summary>
    private static UserTestData? ResolveUser(UserTestDataCollection? collection, string name)
    {
        if (collection == null) return null;

        return typeof(UserTestDataCollection)
            .GetProperties()
            .FirstOrDefault(p => p.PropertyType == typeof(UserTestData)
                              && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?.GetValue(collection) as UserTestData;
    }

    /// <summary>
    /// Recovers the raw base64 assertion from SsoHelper's form body
    /// ("RelayState=...&amp;SAMLResponse=&lt;url-encoded&gt;"). k6 builds its own
    /// RelayState and url-encodes the assertion itself, so passing the encoded
    /// form through would double-encode it and the STS would reject it.
    /// </summary>
    private static string ExtractRawAssertion(string formBody)
    {
        const string marker = "SAMLResponse=";

        // Take the NAMED field out of the form body rather than everything after
        // the marker. SsoHelper happens to put SAMLResponse last today, so a tail
        // slice works by luck; reordering it to RelayState LAST would silently
        // append "&RelayState=..." to the assertion. Splitting on '&' is safe:
        // base64 has no '&', and UrlEncode escapes any that appear in a value.
        var field = formBody.Split('&')
            .FirstOrDefault(part => part.StartsWith(marker, StringComparison.Ordinal));

        // No field means the signer returned something we do not understand.
        // Guessing here would hand the caller a corrupt assertion.
        if (field is null)
        {
            throw new MintFailure("mint_failed",
                $"The signer returned no '{marker}' field in its response.", 2);
        }

        var assertion = HttpUtility.UrlDecode(field[marker.Length..]);
        if (string.IsNullOrWhiteSpace(assertion))
        {
            throw new MintFailure("mint_failed", "The signer returned an empty SAMLResponse.", 2);
        }

        return assertion;
    }

    /// <summary>
    /// Reads the window back off the signed document rather than recomputing it,
    /// so the reported validity is what the STS will actually judge. Best-effort:
    /// never fail a good mint over a reporting detail.
    /// </summary>
    private static (string? NotBefore, string? NotOnOrAfter) ReadWindow(string base64Assertion)
    {
        try
        {
            var xml = Encoding.ASCII.GetString(Convert.FromBase64String(base64Assertion));
            var notBefore = Regex.Match(xml, "NotBefore=\"([^\"]+)\"");
            var notOnOrAfter = Regex.Match(xml, "NotOnOrAfter=\"([^\"]+)\"");
            return (notBefore.Success ? notBefore.Groups[1].Value : null,
                    notOnOrAfter.Success ? notOnOrAfter.Groups[1].Value : null);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Replaces the key in place if present, appends otherwise, so repeated mints
    /// cannot leave a stale assertion above the fresh one for a reader that takes
    /// the first match. Newlines are LF: these files are read by bash too, where a
    /// trailing \r ends up inside the value and corrupts it.
    /// </summary>
    private static void WriteToEnvFile(string path, string key, string value)
    {
        var lines = File.Exists(path)
            ? File.ReadAllLines(path).ToList()
            : [];

        var line = $"{key}={value}";
        var index = lines.FindIndex(l => l.TrimStart().StartsWith($"{key}=", StringComparison.Ordinal));

        if (index >= 0) lines[index] = line;
        else lines.Add(line);

        File.WriteAllText(path, string.Join("\n", lines) + "\n");

        // The file now holds a live credential in plaintext. Narrow it to the owner
        // where the OS allows it cheaply; on Windows it inherits the directory ACL,
        // and reworking that is out of scope for a mint step.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
