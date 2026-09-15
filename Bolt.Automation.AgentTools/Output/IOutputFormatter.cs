namespace Bolt.Automation.AgentTools.Output;

/// <summary>
/// Renders a structured payload as text for stdout. JSON by default; table
/// when the caller passes <c>--format table</c>. Used by commands that return
/// non-trivial data (Phase 3+). Phase-1 stubs go through <see cref="Commands.CommandBase"/>.
/// </summary>
public interface IOutputFormatter
{
    string Format(object payload);
}
