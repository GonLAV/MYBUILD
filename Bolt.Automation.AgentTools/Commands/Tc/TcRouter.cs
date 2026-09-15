using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Tc;

internal static class TcRouter
{
    public static Task<int> RunAsync(string[] args)
    {
        // The plan's user-facing CLI has multi-word verbs ("tc cache list", "tc cache clear",
        // "tc auth set-token"). CommandLineParser only supports single-token [Verb] names,
        // so we rewrite the user's input to hyphenated form before parsing. The hyphenated
        // form is also accepted as a direct alias.
        var rewritten = RewriteMultiWord(args);

        return Parser.Default
            .ParseArguments<FetchOptions, CacheListOptions, CacheClearOptions, AuthSetTokenOptions>(rewritten)
            .MapResult(
                (FetchOptions o)         => FetchCommand.ExecuteAsync(o),
                (CacheListOptions o)     => CacheListCommand.ExecuteAsync(o),
                (CacheClearOptions o)    => CacheClearCommand.ExecuteAsync(o),
                (AuthSetTokenOptions o)  => AuthSetTokenCommand.ExecuteAsync(o),
                _                        => Task.FromResult(3));
    }

    private static string[] RewriteMultiWord(string[] args)
    {
        if (args.Length >= 2 && args[0] == "cache" && args[1] == "list")
            return ["cache-list", .. args[2..]];
        if (args.Length >= 2 && args[0] == "cache" && args[1] == "clear")
            return ["cache-clear", .. args[2..]];
        if (args.Length >= 2 && args[0] == "auth" && args[1] == "set-token")
            return ["auth-set-token", .. args[2..]];
        return args;
    }
}
