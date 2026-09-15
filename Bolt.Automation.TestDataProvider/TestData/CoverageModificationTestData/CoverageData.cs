using Bolt.Automation.Common.Enums;

namespace Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData;

public sealed record CoverageVerbiage(string DisplayName, string Tooltip);

public sealed record CoverageDefinition(
    string Group,
    CoverageEnums Coverage,
    CoverageVerbiage DefaultVerbiage,
    IReadOnlyDictionary<string, CoverageVerbiage> StateOverrides)
{

    public CoverageVerbiage GetVerbiage(string? state = null)
    {
        if (string.IsNullOrWhiteSpace(state)) return DefaultVerbiage;
        var key = state.Trim().ToUpperInvariant();
        return StateOverrides.GetValueOrDefault(key, DefaultVerbiage);
    }
}

public sealed record DeductibleDefinition(
    CoverageEnums Coverage,
    CoverageVerbiage DefaultVerbiage,
    IReadOnlyDictionary<string, CoverageVerbiage> StateOverrides)
{
  
    public CoverageVerbiage GetVerbiage(string? state = null)
    {
        if (string.IsNullOrWhiteSpace(state)) return DefaultVerbiage;
        var key = state.Trim().ToUpperInvariant();
        return StateOverrides.GetValueOrDefault(key, DefaultVerbiage);
    }
}



public enum CoverageAvailability { Available, Unavailable }

public sealed class LobOverride
{
    public string? Group { get; init; }
    public string? DisplayTitle { get; init; }
    public string? Tooltip { get; init; }
    public CoverageAvailability Availability { get; init; } = CoverageAvailability.Available;
}

public sealed class CoverageDefinitionSpec
{
    public CoverageEnums Coverage { get; init; }
    public string DefaultDisplayTitle { get; init; } = string.Empty;
    public string DefaultTooltip { get; init; } = string.Empty;
    public string DefaultGroup { get; init; } = string.Empty;
    public Dictionary<LOBEnums, LobOverride> LobOverrides { get; init; } = new();
}

public static class CoverageDefinitions
{
    // Helper to resolve enum from legacy string names passed in example
    private static CoverageEnums Map(string modelName) => Enum.Parse<CoverageEnums>(modelName, ignoreCase: true);

    private static CoverageDefinitionSpec Spec(
        string coverageModelName,
        string defaultTitle,
        string defaultTooltip,
        string defaultGroup,
        Action<Dictionary<LOBEnums, LobOverride>>? configure = null)
    {
        var overrides = new Dictionary<LOBEnums, LobOverride>();
        configure?.Invoke(overrides);
        return new CoverageDefinitionSpec
        {
            Coverage = Map(coverageModelName),
            DefaultDisplayTitle = defaultTitle,
            DefaultTooltip = defaultTooltip,
            DefaultGroup = defaultGroup,
            LobOverrides = overrides
        };
    }

    public static readonly IReadOnlyList<CoverageDefinitionSpec> All = new List<CoverageDefinitionSpec>
    {
        Spec("Dwelling", "Repair/rebuild dwelling", "If your home is damaged in a covered loss, Dwelling Coverage pays to repair, rebuild, or replace your home's physical structure", "A", overrides =>
        {
            overrides[LOBEnums.HO6] = new LobOverride
            {
                DisplayTitle = "Repair/Rebuild interior",
                Tooltip = "This coverage pays to repair or rebuild the interior portion of your condo if it is damaged in a covered loss. The coverage starts with the interior walls and moves inward. Any damage caused to the exterior portion of your condo should be covered by your condo association."
            };
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
            overrides[LOBEnums.MFH] = new LobOverride { DisplayTitle = "Repair/rebuild your home" };
        }),

        Spec("ReplaceMentCostDwelling", "Settlement option - Dwelling",
             "Replacement cost: Pays to repair or rebuild your home back to the original condition before the loss. Characteristics of the home are used to accurately determine the value of the home's rebuild cost. Actual Cash Value: This coverage takes the market price of the home and adjusts coverage to account for depreciation. Age & condition are factored in when calculating the value for the home.",
             "A", overrides =>
        {
            overrides[LOBEnums.HO6] = new LobOverride { Availability = CoverageAvailability.Unavailable };
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
            overrides[LOBEnums.MFH] = new LobOverride { DisplayTitle = "Settlement Option- Dwelling" };
        }),

        // Settlement Option - Roof (Wind/Hail)
        Spec("ReplaceMentCostRoof", "Settlement option - Roof (Wind/Hail only)",
             "In the event of a covered loss, any repair or replacement to your roof will be adjusted by one of the following applicable settlement options: Replacement Cost: Covers the cost to repair or replace property, with no deduction for depreciation. Actual Cash Value (ACV): Cost to repair or replace with new materials of like kind and quality, minus physical deterioration, and depreciation. ACV simply takes into account that the exterior materials of your roof have existed for several years and have suffered from a certain amount of wear and tear before the loss. Scheduled Depreciation: Is based on the age of your roof and exterior materials used. The value of your roof follows a schedule of depreciation based on the average deterioration that occurs to the materials of your roof over the course of time.",
             "A", overrides =>
        {
            overrides[LOBEnums.HO6] = new LobOverride { Availability = CoverageAvailability.Unavailable };
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA", DisplayTitle = "Settlement Option- Roof" };
            overrides[LOBEnums.MFH] = new LobOverride { Availability = CoverageAvailability.Unavailable };
        }),

        Spec("OtherStructures", "Repair/rebuild other structures",
             "This pays for damages to structures set apart from the main dwelling in the event of a covered loss. This includes structures such as detached garages, storage sheds, fences, gazebos, or pergolas.",
             "B", overrides =>
        {
            overrides[LOBEnums.HO6] = new LobOverride { Availability = CoverageAvailability.Unavailable };
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
        }),

        Spec("PersonalProperty", "Personal property",
             "Personal Property Coverage, also referred to as Contents Coverage, protects your belongings (furniture, appliances, clothing, and electronics) in the event of a covered loss. High value items like jewelry may require additional coverage.",
             "C", overrides =>
        {
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
            overrides[LOBEnums.MFH] = new LobOverride { DisplayTitle = "Personal Property" };
        }),

        Spec("ReplaceMentCostContents", "Settlement option - Personal property",
             "Replacement Cost: Pays to replace damaged or stolen property in the event of a covered loss. Lost belongings will be reimbursed for the amount it would take to buy the item brand new. Depreciation of the item's value is not considered. Actual Cash Value: Reimburses the item for its depreciated value at the time of the loss. Actual cash value does not guarantee an exact replacement of the lost or stolen property.",
             "C", overrides =>
        {
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
            overrides[LOBEnums.MFH] = new LobOverride { DisplayTitle = "Settlement Option- personal property" };
        }),

        Spec("LossOfUse", "Temporary living expenses",
             "If your home is damaged due to a covered loss and requires relocation during repairs, this coverage will pay for the increased living expenses. Examples of expenses may include hotels, rent, dining out, or laundry services above your normal costs.",
             "D", overrides =>
        {
            overrides[LOBEnums.HO6] = new LobOverride
            {
                Tooltip = "If your condo is damaged due to a covered loss and requires relocation during repairs, this coverage will pay for the increased living expenses. Examples of expenses may include hotels, rent, dining out, or laundry services above your normal costs."
            };
            overrides[LOBEnums.DF] = new LobOverride
            {
                Group = "NA",
                DisplayTitle = "Temporary living expense",
                Tooltip = "If your home is damaged due to a covered loss and requires relocation during repairs, this coverage will pay for the increased living expenses. Examples of expenses may include hotels, rent, dining out, or laundry services above your normal costs."
            };
            overrides[LOBEnums.MFH] = new LobOverride { DisplayTitle = "Temporary Living Expenses" };
        }),

        Spec("PersonalLiability", "Personal liability",
             "This coverage provides financial protection for damages or injuries to others that you may be responsible for. Personal liability also extends to damages or injuries caused by household relatives to better protect your family. These losses could include injuries to others on your property, damages to other's property, lawsuits, and legal fees.",
             "E", overrides =>
        {
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
        }),

        Spec("MedicalPayments", "Medical payments to others",
             "Designed to pay for necessary medical care to guests for minor injuries that occurred on your property. Unlike personal liability coverage, medical payments are available to others regardless of fault.",
             "F", overrides =>
        {
            overrides[LOBEnums.DF] = new LobOverride { Group = "NA" };
        })
    };
}

// ------------------------------------------------------------------------------------
// Deductible specification model (refactored to mirror coverage spec format)
// ------------------------------------------------------------------------------------
public sealed class DeductibleDefinitionSpec
{
    public CoverageEnums Coverage { get; init; }
    public string DefaultDisplayTitle { get; init; } = string.Empty;
    public string DefaultTooltip { get; init; } = string.Empty;
    public Dictionary<LOBEnums, DeductibleLobOverride> LobOverrides { get; init; } = new();
}

public sealed class DeductibleLobOverride
{
    public Dictionary<string, CoverageVerbiage> StateOverrides { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class DeductibleDefinitions
{
    private static CoverageEnums Map(string modelName) => Enum.Parse<CoverageEnums>(modelName, true);

    private static DeductibleDefinitionSpec Spec(
        string coverageModelName,
        string defaultTitle,
        string defaultTooltip,
        Action<Dictionary<LOBEnums, DeductibleLobOverride>>? configure = null)
    {
        var lob = new Dictionary<LOBEnums, DeductibleLobOverride>();
        configure?.Invoke(lob);
        return new DeductibleDefinitionSpec
        {
            Coverage = Map(coverageModelName),
            DefaultDisplayTitle = defaultTitle,
            DefaultTooltip = defaultTooltip,
            LobOverrides = lob
        };
    }

    // Shared state override verbiage (Florida / New York)
    private static readonly CoverageVerbiage NonHurricaneFL = new(
        "Non-Hurricane deductible",
        "A deductible is the out-of-pocket expense you agree to pay in the event of a non-hurricane covered loss.");
    private static readonly CoverageVerbiage HurricaneFL = new(
        "Hurricane deductible",
        "Hurricane Deductible is the out-of-pocket expense you agree to pay in the event of a covered loss. This deductible only applies to losses that are a direct result from a storm system that has been declared a hurricane by the National Hurricane Center of the National Weather Service. The deductible may be shown as a dollar amount or as a percentage of the Dwelling Coverage Limit (or Coverage A). For example, your Hurricane Deductible is 1% and the Dwelling Coverage Limit is $200,000. That means the Hurricane deductible (out-of-pocket expense) for your claim would be $2,000.");
    private static readonly CoverageVerbiage WindNY = new(
        "Wind deductible",
        "Wind Deductible is the out-of-pocket expense you agree to pay in the event of a covered loss. This deductible only applies to losses that are a direct result from wind. The deductible may be shown as a dollar amount or as a percentage of the Dwelling Coverage Limit (or Coverage A). For example, your Wind Deductible is 1% and the Dwelling Coverage Limit is $200,000. That means the Wind deductible (out-of-pocket expense) for your claim would be $2,000.");

    public static readonly IReadOnlyList<DeductibleDefinitionSpec> All = new List<DeductibleDefinitionSpec>
    {
        // Standard / All Perils deductible (model AllPerils for all LOBs; HO6 can label upstream if needed)
        Spec("AllPerils", "Standard deductible", "A deductible is the out-of-pocket expense you agree to pay in the event of a covered loss.", lob =>
        {
            lob[LOBEnums.HO3] = new DeductibleLobOverride { StateOverrides = { ["FL"] = NonHurricaneFL } };
            lob[LOBEnums.HO6] = new DeductibleLobOverride { StateOverrides = { ["FL"] = NonHurricaneFL } };
            lob[LOBEnums.DF]  = new DeductibleLobOverride { StateOverrides = { ["FL"] = NonHurricaneFL } };
        }),

        // Wind / Hail (with hurricane & NY wind overrides)
        Spec("WindHail", "Wind/Hail deductible", "Wind & Hail Deductible is the out-of-pocket expense you agree to pay in the event of a covered loss. This deductible only applies to losses that are a direct result from wind or hail. The deductible may be shown as a dollar amount or as a percentage of the Dwelling Coverage Limit (or Coverage A). For example, your Wind and Hail Deductible is 1% and the Dwelling Coverage Limit is $200,000. That means the Wind and Hail deductible (out-of-pocket expense) for your claim would be $2,000.", lob =>
        {
            lob[LOBEnums.HO3] = new DeductibleLobOverride { StateOverrides = { ["FL"] = HurricaneFL, ["NY"] = WindNY } };
            lob[LOBEnums.HO6] = new DeductibleLobOverride { StateOverrides = { ["FL"] = HurricaneFL, ["NY"] = WindNY } };
            lob[LOBEnums.DF]  = new DeductibleLobOverride { StateOverrides = { ["FL"] = HurricaneFL, ["NY"] = WindNY } };
        })
    };
}

// ------------------------------------------------------------------------------------
// CoverageDataProvider rebuilt from declarative specs (with deductibles)
// ------------------------------------------------------------------------------------
public static class CoverageDataProvider
{
    private static readonly IReadOnlyDictionary<LOBEnums, IReadOnlyList<CoverageDefinition>> _coverage;
    private static readonly IReadOnlyDictionary<LOBEnums, IReadOnlyList<DeductibleDefinition>> _deductibles;

    // Static constructor builds dictionaries once
    static CoverageDataProvider()
    {
        _coverage = BuildCoverage();
        _deductibles = BuildDeductibles();
#if DEBUG
        Validate();
#endif
    }

    private static IReadOnlyDictionary<LOBEnums, IReadOnlyList<CoverageDefinition>> BuildCoverage()
    {
        var map = new Dictionary<LOBEnums, List<CoverageDefinition>>();
        foreach (var lob in Enum.GetValues<LOBEnums>()) map[lob] = new List<CoverageDefinition>();

        foreach (var spec in CoverageDefinitions.All)
        {
            foreach (var lob in Enum.GetValues<LOBEnums>())
            {
                spec.LobOverrides.TryGetValue(lob, out var lobOverride);
                if (lobOverride?.Availability == CoverageAvailability.Unavailable) continue;
                var group = lobOverride?.Group ?? spec.DefaultGroup;
                var display = lobOverride?.DisplayTitle ?? spec.DefaultDisplayTitle;
                var tooltip = lobOverride?.Tooltip ?? spec.DefaultTooltip;
                map[lob].Add(new CoverageDefinition(group, spec.Coverage, new CoverageVerbiage(display, tooltip), new Dictionary<string, CoverageVerbiage>()));
            }
        }
        return map.ToDictionary(k => k.Key, v => (IReadOnlyList<CoverageDefinition>)v.Value);
    }

    private static IReadOnlyDictionary<LOBEnums, IReadOnlyList<DeductibleDefinition>> BuildDeductibles()
    {
        var map = new Dictionary<LOBEnums, List<DeductibleDefinition>>();
        foreach (var lob in Enum.GetValues<LOBEnums>()) map[lob] = new List<DeductibleDefinition>();

        foreach (var spec in DeductibleDefinitions.All)
        {
            foreach (var lob in Enum.GetValues<LOBEnums>())
            {
                spec.LobOverrides.TryGetValue(lob, out var lobOverride);
                if (lobOverride == null) continue; // no deductible for this lob
                var stateOverrides = lobOverride.StateOverrides;
                map[lob].Add(new DeductibleDefinition(spec.Coverage,
                    new CoverageVerbiage(spec.DefaultDisplayTitle, spec.DefaultTooltip),
                    stateOverrides));
            }
        }
        return map.ToDictionary(k => k.Key, v => (IReadOnlyList<DeductibleDefinition>)v.Value);
    }

#if DEBUG
    private static void Validate()
    {
        foreach (var (lob, list) in _coverage)
        {
            var dup = list.GroupBy(c => c.Coverage).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dup.Any()) throw new InvalidOperationException($"Duplicate coverage enum(s) found in {lob}: {string.Join(", ", dup)}");
        }
    }
#endif

    public static IReadOnlyList<CoverageDefinition> GetCoverages(LOBEnums lob) =>
        _coverage.TryGetValue(lob, out var list) ? list : Array.Empty<CoverageDefinition>();
    public static CoverageDefinition? FindCoverage(LOBEnums lob, CoverageEnums coverage) =>
        GetCoverages(lob).FirstOrDefault(c => c.Coverage == coverage);
    public static CoverageVerbiage? GetCoverageVerbiage(LOBEnums lob, CoverageEnums coverage, string? state = null) =>
        FindCoverage(lob, coverage)?.GetVerbiage(state);

    public static IReadOnlyList<DeductibleDefinition> GetDeductibles(LOBEnums lob) =>
        _deductibles.TryGetValue(lob, out var list) ? list : Array.Empty<DeductibleDefinition>();
    public static DeductibleDefinition? FindDeductible(LOBEnums lob, CoverageEnums coverage) =>
        GetDeductibles(lob).FirstOrDefault(d => d.Coverage == coverage);
    public static CoverageVerbiage? GetDeductibleVerbiage(LOBEnums lob, CoverageEnums coverage, string? state = null) =>
        FindDeductible(lob, coverage)?.GetVerbiage(state);
}