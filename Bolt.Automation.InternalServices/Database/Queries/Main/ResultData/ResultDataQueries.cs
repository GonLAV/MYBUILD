using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.InternalServices.Database.Contexts;
using LinqToDB;
using LinqToDB.Async;


namespace Bolt.Automation.InternalServices.Database.Queries.Main.ResultData;

/// <summary>
/// Raw query helpers for carrier ResultData + coverage attachments (no business logic).
/// </summary>
public class ResultDataQueries(MainDbContext db, IAutomationLogger logger) : MainQuery(db)
{
    private const int CoverageAttachmentType = 3;
    // Carrier request/response attachment: carries XWindFlag + returned WindHailDeductible values
    // that the coverage XML (type 3) omits (kb partner:pgr-covmod-wind-hail-na).
    private const int RequestAttachmentType = 2;

    /// <summary>
    /// Row DTO representing a coverage attachment XML candidate.
    /// </summary>
    public sealed record CoverageAttachmentRow(
        Guid ResultId,
        bool Selected,
        decimal? Premium,
        string XmlContent);

    /// <summary>
    /// Row DTO for a carrier request/response attachment (<see cref="RequestAttachmentType"/>).
    /// </summary>
    public sealed record RequestAttachmentRow(Guid ResultId, bool Selected, decimal? Premium, string XmlContent);

    /// <summary>
    /// Returns the active carrier request/response attachment XML rows (type 2) for a policy external
    /// id + carrier — the attachment that carries <c>XWindFlag</c> and the returned
    /// <c>WindHailDeductible</c> values.
    /// </summary>
    public async Task<IReadOnlyList<RequestAttachmentRow>> GetRequestAttachmentsAsync(
        string externalId,
        CarrierEnums carrier,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return [];

        short carrierId = MapCarrier(carrier);

        var rows = await (
            from rd in Db.ResultData
            join p in Db.Policies on rd.PolicyId equals p.Id
            join ra in Db.ResultAttachments on rd.Id equals ra.ResultId
            join rac in Db.ResultAttachmentContents on ra.Id equals rac.ResultAttachmentId
            where p.ExternalId == externalId
                  && rd.Carrier == carrierId
                  && rd.IsActive
                  && ra.AttachmentType == RequestAttachmentType
            select new { rd.Id, rd.Selected, rd.Premium, rac.TextAttachment })
            .ToListAsync(ct);

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.TextAttachment))
            .Select(r => new RequestAttachmentRow(r.Id, r.Selected ?? false, r.Premium, r.TextAttachment!))
            .ToList();
    }

    /// <summary>
    /// Raw query returning ALL active coverage attachment XML rows for a policy external id + carrier.
    /// </summary>
    public async Task<IReadOnlyList<CoverageAttachmentRow>> GetCoverageAttachmentsAsync(
        string externalId,
        CarrierEnums carrier,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return [];

        short carrierId = MapCarrier(carrier);

        var rows = await (
            from rd in Db.ResultData
            join p in Db.Policies on rd.PolicyId equals p.Id
            join ra in Db.ResultAttachments on rd.Id equals ra.ResultId
            join rac in Db.ResultAttachmentContents on ra.Id equals rac.ResultAttachmentId
            where p.ExternalId == externalId
                  && rd.Carrier == carrierId
                  && rd.IsActive
                  && ra.AttachmentType == CoverageAttachmentType
            select new
            {
                rd.Id,
                rd.Selected,
                rd.Premium,
                rac.TextAttachment
            })
            .ToListAsync(ct);

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.TextAttachment))
            .Select(r => new CoverageAttachmentRow(r.Id, r.Selected ?? false, r.Premium, r.TextAttachment!))
            .ToList();
    }

    private static short MapCarrier(CarrierEnums carrier) => carrier switch
    {
        CarrierEnums.PlymouthRock => 131,
        CarrierEnums.ASI => 94,
        CarrierEnums.Homesite => 109,
        _ => throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Carrier mapping not defined.")
    };
}