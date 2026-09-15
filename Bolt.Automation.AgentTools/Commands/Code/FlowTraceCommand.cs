using Bolt.Automation.AgentTools.Code;
using CommandLine;

namespace Bolt.Automation.AgentTools.Commands.Code;

[Verb("flow-trace", HelpText = "Return the ordered page sequence registered for a FlowType.")]
internal sealed class FlowTraceOptions
{
    [Option("flow", Required = true, HelpText = "FlowType enum value, e.g. D2CCondoFlow or InterviewCLAutoFlow.")]
    public string Flow { get; set; } = string.Empty;
}

internal static class FlowTraceCommand
{
    public static Task<int> ExecuteAsync(FlowTraceOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Flow))
            return CommandBase.EmitErrorAsync("input_error", "Pass --flow <name>.", exitCode: 3);

        var loader = new ReflectionLoader();
        if (CodeReflection.PreflightFailed(loader, out var preflight))
            return preflight;

        try
        {
            var asm = loader.Load();
            var result = new FlowTracer(asm).Trace(options.Flow);

            if (!result.Found)
            {
                return CommandBase.EmitJsonAsync(new
                {
                    status = "not_found",
                    flow = options.Flow,
                    available_flows = result.AvailableFlows,
                }, exitCode: 2);
            }

            return CommandBase.EmitJsonAsync(new
            {
                flow = result.FlowType,
                flow_namespace = result.FlowTypeNamespace,
                declaring_type = result.DeclaringType,
                page_count = result.Pages.Count,
                pages = result.Pages,
            }, exitCode: 0);
        }
        catch (Exception ex)
        {
            return CommandBase.EmitErrorAsync("reflection_error",
                $"Failed to trace flow via reflection: {ex.Message}", exitCode: 2,
                detail: new { dll = loader.DllPath });
        }
    }
}
