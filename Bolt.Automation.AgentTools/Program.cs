using Bolt.Automation.AgentTools.Commands;

namespace Bolt.Automation.AgentTools;

/// <summary>
/// nexus-agent CLI — entry point dispatching the first arg (noun) to the
/// matching subcommand router. Each router uses CommandLineParser to bind
/// the remaining args to a verb-attribute-decorated class.
///
/// Convention: <c>nexus-agent &lt;noun&gt; &lt;verb&gt; [args]</c>.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Internal entrypoint: the long-lived browser host, spawned by HostClient
        // as `nexus-agent --host-mode <port>`. Not a user-facing noun.
        if (args.Length >= 1 && args[0] == "--host-mode")
        {
            var port = args.Length >= 2 && int.TryParse(args[1], out var p) ? p : 5151;
            return await Browser.HostProcess.HostProgram.RunAsync(port);
        }

        if (args.Length == 0 || IsTopLevelHelp(args[0]))
        {
            PrintHelp();
            return args.Length == 0 ? 3 : 0;
        }

        var noun = args[0];
        var rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

        // Last-resort handler: any exception a command doesn't handle itself
        // becomes a clean exit-2 error on stderr instead of a stack-trace crash
        // (e.g. malformed index.yml, an overflowing duration, an unreadable leaf).
        try
        {
            return noun switch
            {
                "kb"         => await Commands.Kb.KbRouter.RunAsync(rest),
                "tc"         => await Commands.Tc.TcRouter.RunAsync(rest),
                "failure"    => await Commands.Failure.FailureRouter.RunAsync(rest),
                "browser"    => await Commands.Browser.BrowserRouter.RunAsync(rest),
                "code"       => await Commands.Code.CodeRouter.RunAsync(rest),
                "philosophy" => await Commands.Philosophy.PhilosophyRouter.RunAsync(rest),
                "secrets"    => await Commands.Secrets.SecretsRouter.RunAsync(rest),
                "doctor"     => await Commands.Doctor.DoctorCommand.RunAsync(rest),
                "saml"       => await Commands.Saml.SamlRouter.RunAsync(rest),
                _ => UnknownNoun(noun),
            };
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException ?? ex;
            return await Commands.CommandBase.EmitErrorAsync(
                "unhandled_error", inner.Message, exitCode: 2,
                detail: new { noun, type = inner.GetType().Name });
        }
    }

    private static bool IsTopLevelHelp(string arg)
        => arg is "--help" or "-h" or "/?" or "help";

    private static int UnknownNoun(string noun)
    {
        Console.Error.WriteLine($"unknown noun: '{noun}'");
        Console.Error.WriteLine("run with --help to see the noun list.");
        return 3;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            nexus-agent — Nexus AI Agent Extension CLI

            Usage:
              nexus-agent <noun> <verb> [args]

            Nouns and verbs:

              kb         — knowledge-base navigation
                  lookup       --topic <key>
                  search       <terms...> [--type framework|domain|recipe|philosophy]
                  describe     --file <path>

              tc         — Azure DevOps test-case API
                  fetch        <id> [--force-refresh]
                  cache        list | clear [--older-than <duration>]
                  auth         set-token --token <jwt>

              failure    — diagnose a failed test
                  summarize    --test <fqn>
                  page-source  --test <fqn> [--scope form|page|all]
                  screenshot   --test <fqn>

              browser    — live Playwright sessions
                  navigate     --flow <name> --tenant <t> --env <e> --until <PageName> [--headed] [--data <file>] [--set F=V ...]
                  open         --tenant <t> --env <e> [--user <User>] [--login] [--url <u>] [--front-end ADBX] [--headed]
                  open-quote   --tenant <t> --env <e> --quote-file <file> [--front-end D2C] [--user OnlineQuote] [--headed]
                  quote-start  --tenant <t> --env <e> --address <AddressKey> [--user Consumer] [--headed]
                  fill         --session <id> [--data <file>] [--set Field=Value ...]
                  continue     --session <id> [--page <PageName>] [--expect <PageName>]
                  record       [--session <id>] [--url <u>] [--output <file.cs>]
                  parse-recording --file <recording.cs>
                  raw          --session <id> --action <click|dblclick|fill|check|uncheck|select|press|type|hover> --selector <sel> [--value <v>]
                  close        --session <id> | --all
                  screenshot   --session <id> [--scope full|viewport|element] [--selector <sel>]
                  inspect      --session <id> [--scope form|page]
                  pause        --session <id> --reason <text>
                  resume       --session <id>
                  list

              code       — codebase queries
                  find-similar --pattern <symbol> [--limit 5]
                  field-lookup <name>
                  flow-trace   --flow <name>
                  diff-impact  --ref <git-ref>
                  wip-stop     --note <text>

              philosophy — design-philosophy lookup
                  lookup       --area <area>

              secrets    — local secrets cache management
                  sync         [--region <r>] [--secret-id <id>] [--ttl <hours>] [--force]
                  status

              doctor     — environment bootstrap/diagnosis (QA machines)
                  [--fix]      check repo/build/playwright/aws/secrets/env; --fix applies non-interactive repairs; records last-known-good build

              saml       — mint a signed SAML assertion for callers that cannot sign one
                  mint         --tenant <t> --env <e> [--user Admin] [--template <T>]
                               [--valid-minutes 10] [--env-file <path> --env-key <KEY>]

            Output is JSON to stdout (or `--format table` for compact text).
            `saml mint` is the exception: it prints the raw base64 assertion on stdout
            and its metadata on stderr, so a caller can capture the value directly.
            Exit codes: 0 success · 2 user-facing error · 3 input error.

            All commands are live: kb · tc · failure · code · browser · philosophy · secrets · saml.
            """);
    }
}
