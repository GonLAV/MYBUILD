namespace Bolt.Automation.FrontEnds.Projects.Interview.Pages;

public record CarrierDeclination(string Carrier, string Reason);

/// <summary>
/// <paramref name="Offline"/> holds carriers from CL's "Offline" tab - carriers that require
/// offline/manual quoting rather than an instant online rate. <paramref name="Other"/> catches
/// carriers from any remaining market-category tab that isn't Admitted/Declinations-Failures/
/// Non-admitted/Offline - e.g. CL's "Additional Carriers" tab - keyed by the tab's label with
/// its carrier count stripped.
/// <para>
/// The four <c>*TabPresent</c> flags record whether that market-category tab actually rendered
/// on the page, as distinct from rendering with zero carriers - an empty list alone can't tell
/// the two apart (e.g. a Personal Auto results page has no "Offline" tab at all, but would
/// otherwise report the same <c>Offline: []</c> as a CL page whose Offline tab rendered "(0)").
/// </para>
/// </summary>
public record MarketRatesResult(
    List<string> Admitted,
    List<CarrierDeclination> DeclinationsFailures,
    List<string> NonAdmitted,
    List<string> Offline,
    Dictionary<string, List<string>> Other,
    bool AdmittedTabPresent,
    bool DeclinationsFailuresTabPresent,
    bool NonAdmittedTabPresent,
    bool OfflineTabPresent);
