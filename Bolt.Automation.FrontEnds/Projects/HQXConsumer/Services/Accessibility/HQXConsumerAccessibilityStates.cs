using Bolt.Automation.FrontEnds.PlaywrightBase.Accessibility;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Accessibility;

/// <summary>The distinct screen states an accessibility sweep has to cover, derived from the field registry.</summary>
public static class HQXConsumerAccessibilityStates
{
    /// <summary>The screen as it renders with prefilled data and nothing expanded.</summary>
    public const string DefaultState = "default";

    /// <summary>Overview's collapsible sections. Expanding one shows questions but changes no data.</summary>
    public static IReadOnlyList<string> OverviewSections =>
    [
        HQXConsumer_OverviewPage.SectionNames.Property,
        HQXConsumer_OverviewPage.SectionNames.Exterior,
        HQXConsumer_OverviewPage.SectionNames.Interior,
        HQXConsumer_OverviewPage.SectionNames.PersonalInfo
    ];

    /// <summary>Value-gated child questions on a page, keyed by the parent that reveals them.</summary>
    public static IReadOnlyList<ConditionalReveal> RevealsFor(Type pageType) =>
        ConditionalRevealMap.ForPage(FieldRegistryHQXConsumer.Fields, pageType);

    /// <summary>State names for a page: the default, plus Overview's sections where applicable.</summary>
    // Reveal states are excluded — driving a parent mutates form data, so they are swept by a
    // dedicated test rather than inside a flow walk that still has to reach Rates.
    public static IReadOnlyList<string> NonMutatingStatesFor(Type pageType) =>
        pageType == typeof(HQXConsumer_OverviewPage)
            ? [DefaultState, .. OverviewSections.Select(section => $"section:{section}")]
            : [DefaultState];
}
