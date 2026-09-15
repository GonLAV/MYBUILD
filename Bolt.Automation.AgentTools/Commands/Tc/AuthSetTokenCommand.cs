using Bolt.Automation.AgentTools.ApiClients;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Tc;

[Verb("auth-set-token", HelpText = "Persist a nexus-logger bearer token with user-only ACL (also 'tc auth set-token').")]
internal sealed class AuthSetTokenOptions
{
    [Option("token", Required = true, HelpText = "Azure AD JWT issued for the nexus-logger API.")]
    public string Token { get; set; } = string.Empty;
}

internal static class AuthSetTokenCommand
{
    public static Task<int> ExecuteAsync(AuthSetTokenOptions options)
    {
        var token = options.Token?.Trim() ?? string.Empty;
        if (token.Length == 0)
            return CommandBase.EmitErrorAsync("input_error", "Pass --token <jwt>.", exitCode: 3);

        // Light shape check — a JWT has three dot-separated segments. We don't
        // validate the signature (that's the server's job), just catch obvious
        // paste errors before storing.
        if (token.Count(c => c == '.') != 2)
        {
            return CommandBase.EmitErrorAsync(
                "input_error",
                "That doesn't look like a JWT (expected three dot-separated segments). Token not stored.",
                exitCode: 3);
        }

        var (path, acl) = TokenStore.Save(token);

        // Never echo the token back.
        return CommandBase.EmitJsonAsync(new
        {
            status = "stored",
            path,
            acl,
            note = "Token expires ~65 minutes after issuance; re-run if you hit 401.",
        }, exitCode: 0);
    }
}
