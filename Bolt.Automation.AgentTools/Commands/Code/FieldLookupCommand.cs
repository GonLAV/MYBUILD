using Bolt.Automation.AgentTools.Code;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Code;

[Verb("field-lookup", HelpText = "Resolve a UI field name to its UIElement entry + Pages tags. Returns ALL matches grouped by FrontEnd.")]
internal sealed class FieldLookupOptions
{
    [Value(0, MetaName = "name", Required = true, HelpText = "Field name, e.g. 'VIN' or 'PrimaryDriverFirstName'.")]
    public string Name { get; set; } = string.Empty;
}

internal static class FieldLookupCommand
{
    public static Task<int> ExecuteAsync(FieldLookupOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
            return CommandBase.EmitErrorAsync("input_error", "Pass a field name.", exitCode: 3);

        var loader = new ReflectionLoader();
        if (CodeReflection.PreflightFailed(loader, out var preflight))
            return preflight;

        try
        {
            var asm = loader.Load();
            var result = new FieldRegistryReader(asm).Lookup(options.Name);

            if (result.MatchCount == 0)
            {
                return CommandBase.EmitJsonAsync(new
                {
                    status = "not_found",
                    field = result.Field,
                    searched_front_ends = result.SearchedFrontEnds,
                    hint = "No registry entry by that exact name. Try `code find-similar --pattern <name>` "
                         + "or `kb lookup --topic framework:field-registry`.",
                }, exitCode: 2);
            }

            return CommandBase.EmitJsonAsync(new
            {
                field = result.Field,
                match_count = result.MatchCount,
                matches = result.Matches,
                searched_front_ends = result.SearchedFrontEnds,
            }, exitCode: 0);
        }
        catch (Exception ex)
        {
            return CommandBase.EmitErrorAsync("reflection_error",
                $"Failed to read field registries via reflection: {ex.Message}", exitCode: 2,
                detail: new { dll = loader.DllPath });
        }
    }
}
