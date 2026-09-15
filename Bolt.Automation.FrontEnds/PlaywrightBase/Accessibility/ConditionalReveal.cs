using Bolt.Automation.FrontEnds.FormData;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;

/// <summary>A parent field value that reveals one or more child questions.</summary>
public sealed record ConditionalReveal(string ParentField, string ParentValue, IReadOnlyList<string> ChildFields)
{
    /// <summary>Stable state name for reports and baseline keys.</summary>
    public string StateName => $"{ParentField}={ParentValue}";
}

/// <summary>Derives the conditional reveals on a page from the field registry itself.</summary>
public static class ConditionalRevealMap
{
    /// <summary>Every value-gated reveal whose child fields are registered against <paramref name="pageType"/>.</summary>
    public static IReadOnlyList<ConditionalReveal> ForPage(
        IReadOnlyDictionary<string, UIElement> registry,
        Type pageType)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(pageType);

        return [.. registry
            .Where(entry => entry.Value.Pages?.Contains(pageType) == true)
            .Where(entry => !string.IsNullOrEmpty(entry.Value.DependsOn))
            // An empty DependsOnValue is an ordering hint, not a gate — FormDataHelper.ShouldFillField
            // fills those unconditionally, so they are already covered by the default state.
            .Where(entry => !string.IsNullOrEmpty(entry.Value.DependsOnValue))
            .GroupBy(entry => (Parent: entry.Value.DependsOn!, Value: entry.Value.DependsOnValue!))
            .Select(group => new ConditionalReveal(
                group.Key.Parent,
                group.Key.Value,
                [.. group.Select(entry => entry.Key).OrderBy(name => name)]))
            .OrderBy(reveal => reveal.ParentField)
            .ThenBy(reveal => reveal.ParentValue)];
    }
}
