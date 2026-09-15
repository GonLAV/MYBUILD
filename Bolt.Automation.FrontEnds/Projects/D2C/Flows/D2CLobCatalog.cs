using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Flows
{
    /// <summary>
    /// One line of business as a Professional Services run can request it: which D2C
    /// <see cref="FlowType"/> drives it, which pages that flow must skip to reach Rates, and —
    /// for bundles only — the LOB group titles the Rates page is expected to render.
    /// </summary>
    /// <param name="FlowType">The registered D2C flow walked from
    /// <see cref="D2C_YourAddressPage"/> to <c>D2C_RatesPage</c>.</param>
    /// <param name="PagesToSkip">Pages present in the flow definition that must not be walked for
    /// this LOB. Derived from the closest existing sibling test for each flow — see the per-entry
    /// comments in <see cref="D2CLobCatalog"/>.</param>
    /// <param name="ExpectedRatesLobs">LOB group titles expected on the Rates page. Populated only
    /// for multi-LOB entries, where the group split is the thing under test; single-LOB entries
    /// leave this empty and assert on rated-carrier count alone.</param>
    /// <param name="HasConditionalRoofStep">True when the flow contains the roof-replacement step,
    /// which the interview asks only for older houses. The test walks such a flow in two segments
    /// and resumes from whichever page the app actually lands on — see the note on the Home entry.</param>
    /// <param name="FormData">Field values this LOB must send on top of the caller's own form data,
    /// because the product derives the line of business from what the interview is told rather than
    /// from the flow alone. Condominium needs <c>PLTypeOfDwelling</c>; Dwelling Fire needs
    /// <c>IsPrimaryResidence = No</c> and is otherwise the Home flow. Merged UNDER the caller's
    /// values, so a test can still override.</param>
    /// <remarks>
    /// <para>
    /// <b>FormData is not a place to pin fields that drive conditional pages.</b> Pinning e.g.
    /// PLYearBuilt to suppress the roof-replacement question does not work on tenants that prefill
    /// property data from the address lookup — verified on UNIFY/QA, where house-details renders
    /// none of those inputs, so there is no value to override. Use
    /// <paramref name="HasConditionalRoofStep"/> for that. FormData is for values the USER would
    /// genuinely answer, which is what selects the product.
    /// </para>
    /// </remarks>
    public sealed record D2CLobFlow(
        FlowType FlowType,
        IReadOnlyList<Type> PagesToSkip,
        IReadOnlyList<string> ExpectedRatesLobs,
        bool HasConditionalRoofStep = false,
        IReadOnlyDictionary<string, string>? FormData = null);

    /// <summary>
    /// Line-of-business → D2C flow lookup for runtime-injected Professional Services runs.
    /// Adding a supported LOB is a data change here — a new entry in <c>_flowsByLob</c> (plus any
    /// input aliases) — not a code change in the test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives in FrontEnds, next to <see cref="Flows"/>, because an entry carries a
    /// <see cref="FlowType"/> and page <see cref="Type"/>s. It deliberately does NOT live beside
    /// <c>AddressData.SupportedStateInputs()</c> in TestDataProvider: FrontEnds references
    /// TestDataProvider, so a lookup returning <see cref="FlowType"/> cannot sit on the far side of
    /// that reference. <see cref="SupportedLobInputs"/> mirrors the SHAPE of
    /// <c>SupportedStateInputs()</c>, not its location.
    /// </para>
    /// <para>
    /// <b>Cross-repo contract.</b> The orchestrator's test-discovery tool does NOT reflect this
    /// type directly — it goes through
    /// <see cref="Bolt.Automation.Tests.TestExtension.Helpers.InjectedParameterCatalog"/>.
    /// <see cref="CanonicalLobs"/> feeds the wizard's line-of-business picker and
    /// <see cref="SupportedLobInputs"/> the accept set used to validate a submitted value, so both
    /// must keep their names and shapes. Renaming either one is safe only if that catalog is
    /// updated in the same change.
    /// </para>
    /// <para>
    /// <b>Canonical names use the D2C product's own wording</b> — "Auto", "Home", "Home+Auto",
    /// "Renters", "Condo", "Condo+Auto", "DF", "DF+Auto" — matching the LobType enum so the
    /// orchestrator's picker reads the way the codebase and the LOBs page already name things.
    /// Policy codes ("HO3", "HO4", "HO6", "DP3"), spelled-out forms ("Condominium",
    /// "Dwelling Fire") and the older "HomeAuto" spelling stay accepted as aliases but are not
    /// offered — one product must never appear as several choices.
    /// </para>
    /// </remarks>
    public static class D2CLobCatalog
    {
        /// <summary>
        /// LOB used when <c>INJECTED_PS_LOB</c> is unset. Auto is what the PS D2C test ran before
        /// LOB became selectable, so saved runs and variable collections that predate the LOB
        /// picker keep behaving exactly as they did.
        /// </summary>
        public const string DefaultLob = "Auto";


        // Canonical name -> flow. Canonical names are what the wizard shows, what the run record
        // records, and what appears in the composite TestId — keep them short and stable.
        private static readonly Dictionary<string, D2CLobFlow> _flowsByLob = new(StringComparer.OrdinalIgnoreCase)
        {
            // Auto: the pre-existing PS behaviour, unchanged. Cross-sell is skipped so the run
            // stays single-LOB (same skip set as BOLTAG_D2C_Auto_* in D2CAutoTests).
            ["Auto"] = new(
                FlowType.D2CAutoFlow,
                [typeof(D2C_CrossSellInformationPage)],
                []),

            // Home: PropertiesUsage only renders when the property is NOT the primary residence,
            // and the registry default for IsPrimaryResidence is "Yes" — so it is skipped, as in
            // D2CHomeTests/D2CCrossSellTests. Cross-sell is skipped to keep the run single-LOB.
            //
            // Roof-replacement is deliberately NOT in the skip set, and cannot be. Whether the
            // interview asks the roof question is derived from how old the house is, and house age
            // is prefilled from the address — so the same flow renders a different page sequence per
            // state (UNIFY/QA: OH and MA render it; TX_Crowley, a 2017 build, and PA do not). A skip
            // set keyed on LOB would have to depend on the state axis, so no value here is right for
            // every state. HasConditionalRoofStep instead tells the test to walk the flow in two
            // segments and resume from whichever page the app actually lands on.
            ["Home"] = new(
                FlowType.D2CHomeFlow,
                [typeof(D2C_PropertiesUsagePage), typeof(D2C_CrossSellInformationPage)],
                [],
                HasConditionalRoofStep: true),

            // Renters: the flow definition contains neither PropertiesUsage nor CrossSell, so
            // there is nothing to skip (matches BOLTAG_D2C_Testing_Payment_Site's Renters leg).
            //
            // PLTypeOfDwelling must be answered EXPLICITLY, and the registry DEFAULT cannot serve:
            // it is "PersonalHome", which the Renters Properties page does not offer at all. That
            // page ("What is your type of home?") offers Apartment | SingleFamilyHouse | Townhouse
            // | Rowhouse — verified on UNIFY/UAT/CA. With no option selected the page's Continue
            // button stays disabled and the flow strands on Properties.
            //
            // SingleFamilyHouse over Apartment: the address the flow quotes resolves to a house, so
            // this keeps the answer consistent with the property data the interview already holds,
            // and it matches the TypeOfHome default ("Single Family House") the house-details page
            // uses for the same underlying field.
            ["Renters"] = new(
                FlowType.D2CRentersFlow,
                [],
                [],
                FormData: new Dictionary<string, string> { [PLTypeOfDwelling] = "SingleFamilyHouse" }),

            // Home + Auto bundle. D2CHomeAutoFlow (not D2CAutoHomeFlow) is the entry point that
            // starts from the property side, which is how the wizard presents "Home + Auto";
            // both enum members exist and differ only in page order. Skip set and the expected
            // Rates group titles both come from BOLTAG_D2C_HomeAuto_Test.
            //
            // Canonical name matches the D2C product's own wording ("Home + Auto" on the LOBs page)
            // so the orchestrator's picker reads the same as what a QA sees on screen. The '+' is
            // sanitized to '_' in the composite TestId, giving "..._Home_Auto".
            ["Home+Auto"] = new(
                FlowType.D2CHomeAutoFlow,
                [typeof(D2C_PropertiesUsagePage), typeof(D2C_CrossSellInformationPage)],
                ["Auto", "Homeowners"],
                HasConditionalRoofStep: true),

            // Condominium. A dedicated flow exists, and unlike Home it contains NO
            // roof-replacement page at all — so there is no conditional step to resume around.
            // PLTypeOfDwelling is what actually selects the product: the field defaults to
            // "PersonalHome", so without this the Condo flow would quote a house
            // (see BOLTAG_D2C_E2E_Condominium, which sends exactly this one value).
            ["Condo"] = new(
                FlowType.D2CCondoFlow,
                [typeof(D2C_PropertiesUsagePage), typeof(D2C_CrossSellInformationPage)],
                [],
                FormData: new Dictionary<string, string> { [PLTypeOfDwelling] = "Condominium" }),

            ["Condo+Auto"] = new(
                FlowType.D2CCondoAutoFlow,
                [typeof(D2C_PropertiesUsagePage), typeof(D2C_CrossSellInformationPage)],
                ["Auto", "Condominium"],
                FormData: new Dictionary<string, string> { [PLTypeOfDwelling] = "Condominium" }),

            // Dwelling Fire has NO flow of its own — it IS the Home flow, and the product derives
            // the LOB from the property not being the insured's primary residence. Hence
            // IsPrimaryResidence = No (verified by BOLTAG_D2C_DF_CrossSell_Auto and
            // BOLTAG_D2C_HO3_NewProperty_SecondarySeasonal, which assert the resulting LOB is
            // "DwellingFire").
            //
            // Consequently PropertiesUsage must NOT be skipped here, unlike every other property
            // LOB: that page renders precisely BECAUSE the answer is No, and it is where
            // DwellingUsage gets answered. Skipping it would strand the flow.
            //
            // DwellingUsage must be answered EXPLICITLY, and its absence is not a cosmetic gap:
            // the registry defaults DwellingUsage to "Primary" and OccupancyType to "Vacant", so
            // relying on defaults asks the rater to price a property that is simultaneously not
            // the primary residence, used as a primary residence, and vacant. Verified on
            // UNIFY/QA/OH: the Rates page rendered the "Dwelling Fire" group with "No quotes were
            // returned." and an empty coverages list while Auto rated seven carriers. Note the
            // sibling BOLTAG_D2C_DF_CrossSell_Auto does NOT catch this — it only asserts the group
            // TITLES render, never that DF has rates. "Secondary / Seasonal" matches what
            // BOLTAG_D2C_Condominium_Validation and BOLTAG_D2C_HO3_NewProperty_SecondarySeasonal
            // pair with IsPrimaryResidence = No, and it is a value a real user would answer — so
            // it belongs in FormData, unlike a field that drives a conditional page.
            ["DF"] = new(
                FlowType.D2CHomeFlow,
                [typeof(D2C_CrossSellInformationPage)],
                [],
                HasConditionalRoofStep: true,
                FormData: new Dictionary<string, string>
                {
                    [IsPrimaryResidence] = "No",
                    [DwellingUsage] = "Secondary / Seasonal",
                }),

            ["DF+Auto"] = new(
                FlowType.D2CHomeAutoFlow,
                [typeof(D2C_CrossSellInformationPage)],
                // Rates group titles taken verbatim from BOLTAG_D2C_DF_CrossSell_Auto's
                // ValidateLobsOnRatesPage(["Dwelling Fire", "Auto"]) — note the space, which the
                // canonical input name deliberately drops.
                ["Auto", "Dwelling Fire"],
                HasConditionalRoofStep: true,
                FormData: new Dictionary<string, string>
                {
                    [IsPrimaryResidence] = "No",
                    [DwellingUsage] = "Secondary / Seasonal",
                }),
        };

        // Input spellings the orchestrator / a local runsettings may send, mapped onto a canonical
        // name. Canonical names themselves are matched directly and need no alias entry.
        //
        // "HomeAuto" is listed here because it USED to be the canonical name — keeping it accepted
        // means a saved run or a runsettings file written before the rename still resolves.
        private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Home + Auto"] = "Home+Auto",
            ["Home & Auto"] = "Home+Auto",
            ["HomeAuto"] = "Home+Auto",
            ["HomeAndAuto"] = "Home+Auto",
            ["Bundle"] = "Home+Auto",
            ["Homeowners"] = "Home",
            ["HO3"] = "Home",
            ["HO4"] = "Renters",

            ["Condominium"] = "Condo",
            ["HO6"] = "Condo",
            ["Condo + Auto"] = "Condo+Auto",
            ["CondoAuto"] = "Condo+Auto",
            ["Condominium+Auto"] = "Condo+Auto",

            ["DwellingFire"] = "DF",
            ["Dwelling Fire"] = "DF",
            ["DP3"] = "DF",
            ["DF + Auto"] = "DF+Auto",
            ["DFAuto"] = "DF+Auto",
            ["DwellingFire+Auto"] = "DF+Auto",
            ["Dwelling Fire + Auto"] = "DF+Auto",
        };

        /// <summary>
        /// Every accepted <c>INJECTED_PS_LOB</c> input — canonical names first, then aliases.
        /// Surfaced to the orchestrator as the ACCEPT set for INJECTED_PS_LOB via
        /// InjectedParameterCatalog.ParameterAcceptedValues(); see CanonicalLobs for what the
        /// wizard actually offers.
        /// </summary>
        public static IReadOnlyList<string> SupportedLobInputs() =>
            [.. _flowsByLob.Keys
                .Concat(_aliases.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)];

        /// <summary>
        /// Every accepted input mapped to the canonical LOB it resolves to — including each
        /// canonical name mapped to itself, so a consumer needs no fallback branch.
        /// </summary>
        /// <remarks>
        /// The accept set is intentionally many-to-one: <c>Bundle</c>, <c>HomeAuto</c> and
        /// <c>Home + Auto</c> all run the same flow. A consumer that fans work items out over
        /// requested values must therefore fold them through this map BEFORE fan-out, or two
        /// spellings of one LOB become two work items that both resolve to a single composite
        /// TestId and collide on the run record. Surfaced across repos via
        /// <c>InjectedParameterCatalog.ParameterCanonicalValues()</c>.
        /// </remarks>
        public static IReadOnlyDictionary<string, string> LobCanonicalMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var canonical in _flowsByLob.Keys)
                map[canonical] = canonical;

            foreach (var (alias, canonical) in _aliases)
                map[alias] = canonical;

            return map;
        }

        /// <summary>Canonical LOB names only — one entry per flow, no aliases.</summary>
        public static IReadOnlyList<string> CanonicalLobs() =>
            [.. _flowsByLob.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)];

        /// <summary>
        /// Resolves an input spelling to its canonical LOB name, or null when the input names no
        /// supported LOB. Callers turn null into a visible Ignore/failure rather than silently
        /// falling back to Auto — running a different product than the one the user picked is
        /// worse than not running.
        /// </summary>
        public static string? Canonicalize(string lobInput)
        {
            if (string.IsNullOrWhiteSpace(lobInput)) return null;

            var trimmed = lobInput.Trim();

            if (_flowsByLob.TryGetValue(trimmed, out _))
                return _flowsByLob.Keys.First(k => k.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

            return _aliases.TryGetValue(trimmed, out var canonical) ? canonical : null;
        }

        /// <summary>
        /// Flow definition for a canonical LOB name (as returned by <see cref="Canonicalize"/>).
        /// </summary>
        public static D2CLobFlow Get(string canonicalLob) =>
            _flowsByLob.TryGetValue(canonicalLob, out var flow)
                ? flow
                : throw new ArgumentException(
                    $"No D2C flow mapped for line of business '{canonicalLob}'. " +
                    $"Supported inputs: {string.Join(", ", SupportedLobInputs())}.",
                    nameof(canonicalLob));
    }
}
