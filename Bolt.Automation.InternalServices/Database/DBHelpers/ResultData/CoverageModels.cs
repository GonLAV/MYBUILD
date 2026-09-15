using System;

namespace Bolt.Automation.InternalServices.Database.DBHelpers.ResultData;

public sealed record CoverageFieldResult(string CoverageName, IReadOnlyDictionary<string, string?> Fields);

/// <summary>
/// Strongly typed coverage object parsed from ResultData XML.
/// </summary>
public sealed record CoverageData(
    string Name,
    decimal? Limit,
    CoverageCustomType CustomType,
    bool UseInSubmission,
    bool ShowInResultPage,
    int? Order,
    string CoverageLevel,
    string? FreeText,
    bool Comparable,
    bool Compared,
    string ProposalCoverageLevel,
    string SortOrder,
    bool HasImage,
    bool BestOffer);

public enum CoverageCustomType { NA, FreeText, Unknown }

/// <summary>
/// Container for all coverages of the chosen attachment.
/// </summary>
public sealed class CoverageSet
{
    private readonly Dictionary<string, CoverageData> _byName;

    public CoverageSet(IEnumerable<CoverageData> coverages)
    {
        _byName = coverages.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<CoverageData> All => _byName.Values;

    public bool TryGet(string name, out CoverageData data) => _byName.TryGetValue(name, out data!);
}
