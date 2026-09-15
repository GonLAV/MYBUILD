using Bolt.Automation.FrontEnds.Projects.D2C.Flows;

namespace Bolt.Automation.Tests.TestExtension.Helpers
{
    /// <summary>
    /// The single place that answers "what values may each runtime-injected test parameter take?".
    /// One entry per <c>[InjectedParameter("...")]</c> env var that goes through this catalog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Cross-repo contract.</b> The orchestrator's test-discovery tool reflects this type by name
    /// (<c>Bolt.Automation.Tests.TestExtension.Helpers.InjectedParameterCatalog</c>) and invokes
    /// <see cref="ParameterChoices"/>, <see cref="ParameterAcceptedValues"/> and
    /// <see cref="ParameterCanonicalValues"/> to populate the Professional Services wizard's
    /// per-parameter pickers, validate a submitted value, and fold alias spellings together before
    /// fan-out. Do not rename the type or any of the three methods without updating
    /// <c>packages/discovery/DiscoveryPipeline.cs</c> in the automation-orchestrator repo.
    /// </para>
    /// <para>
    /// <b><c>INJECTED_PS_STATE</c> is deliberately absent.</b> The state axis predates this catalog
    /// and is already served end-to-end by the orchestrator's own probe of
    /// <c>AddressData.SupportedStateInputs()</c> — that path works, so it is left untouched rather
    /// than migrated for symmetry. Folding it in here later is a small change on both sides; until
    /// then, adding a state is still just an <c>AddressKey</c> member plus an <c>AddressData</c>
    /// entry, and this catalog covers every OTHER axis.
    /// </para>
    /// <para>
    /// <b>Adding a new fan-out axis is a change to this file only.</b> Add the env var here, put the
    /// matching <c>[InjectedParameter("YOUR_ENV_VAR")]</c> on the test method, and the orchestrator
    /// discovers the axis, renders a picker for it, and fans work items out across it — no
    /// orchestrator code change. That is the point of routing every axis through one method instead
    /// of the tool reflecting a different type per parameter.
    /// </para>
    /// <para>
    /// All three methods are keyed by env var name and return plain <c>Dictionary</c> instances
    /// deliberately: the discovery tool reads them through the non-generic <c>IDictionary</c>
    /// interface, so the shape stays readable across repos without a shared assembly reference.
    /// The two value lists are read as sequences of strings; <see cref="ParameterCanonicalValues"/>
    /// is a map of maps and has its OWN probe on the other side for that reason — enumerating a
    /// dictionary yields <c>KeyValuePair</c>, so feeding it to the sequence probe would filter
    /// every entry out and drop the axis with a warning blaming this file.
    /// </para>
    /// </remarks>
    public static class InjectedParameterCatalog
    {
        /// <summary>
        /// Values the wizard OFFERS for each parameter — the curated, human-facing list.
        /// Keep these short and canonical: they are what a user picks, what the run record stores,
        /// and what ends up in the composite TestId.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> ParameterChoices() =>
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Canonical LOB names only — the alias spellings ("HO3", "HO6", "Condominium",
                // "Dwelling Fire", "Bundle", "HomeAuto") are accepted but deliberately not offered,
                // so the picker shows one chip per real product rather than several that collapse
                // onto the same flow.
                ["INJECTED_PS_LOB"] = D2CLobCatalog.CanonicalLobs(),
            };

        /// <summary>
        /// Every value that is VALID for each parameter — a superset of
        /// <see cref="ParameterChoices"/>. Used to validate a submitted value, so a saved run or a
        /// direct API caller may send a spelling the picker never showed.
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> ParameterAcceptedValues() =>
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["INJECTED_PS_LOB"] = D2CLobCatalog.SupportedLobInputs(),
            };

        /// <summary>
        /// Alias → canonical value, for every entry in <see cref="ParameterAcceptedValues"/>
        /// (canonical values map to themselves). The consumer needs no fallback branch: any accepted
        /// value is a key here.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Fold requested values through this map before fanning work items out.</b> The accept
        /// set is many-to-one on purpose — <c>Bundle</c>, <c>HomeAuto</c> and <c>Home + Auto</c> are
        /// three spellings of one flow. A consumer that dedupes requested values only by string
        /// equality turns <c>['Home+Auto', 'HomeAuto']</c> into two work items in one job; both run
        /// the same LOB and both resolve to the same composite TestId
        /// (<c>24024701_TX_Crowley__Home_Auto</c>), so the second run record collides on the unique
        /// <c>idx_runId_testId</c> index and the two pods overwrite each other's outcome.
        /// Canonicalising first collapses them to the single work item the caller actually meant.
        /// </para>
        /// <para>
        /// An axis absent from this map has no aliases — its accepted values are already canonical
        /// and need only case-insensitive dedupe. That is the case for <c>INJECTED_PS_STATE</c>,
        /// which does not go through this catalog at all (see the type remarks); its <c>TX</c> vs
        /// <c>TX_Crowley</c> spellings have the same many-to-one shape and want the same treatment
        /// if that axis is ever folded in here.
        /// </para>
        /// </remarks>
        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ParameterCanonicalValues() =>
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["INJECTED_PS_LOB"] = D2CLobCatalog.LobCanonicalMap(),
            };
    }
}
