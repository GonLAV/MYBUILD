using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.ApiClients.PlatformApi.Entities.CRMNote;

namespace Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider
{
    public static class CRMNoteDataProvider
    {
        private static readonly Dictionary<NoteType, string> NoteTypeValues = new()
        {
            { NoteType.BuyBundle,                  "BuyBundle" },
            { NoteType.BuyMonoline,                "BuyMonoline" },
            { NoteType.Bridge,                     "Bridge" },
            { NoteType.INETChangedSelectedCarrier,  "INET changed selected carrier" },
            { NoteType.RetrievalByMPS,             "Retrieval by MPS" }
        };

        public static CRMNoteRequestModel CreateCRMNoteData(
            string externalId,
            string sourceName,
            string carrier,
            Lobs lob,
            NoteType noteType = NoteType.Bridge) =>
            CreateCRMNoteData(externalId, sourceName, carrier, lob.ToString(), noteType);

        /// <summary>
        /// Overload for callers that read the LOB off a carrier result rather than choosing it. The
        /// platform matches the note against an active successful result, and results report codes
        /// (e.g. "PersonalHome") that the <see cref="Lobs"/> enum does not carry.
        /// </summary>
        public static CRMNoteRequestModel CreateCRMNoteData(
            string externalId,
            string sourceName,
            string carrier,
            string lobCode,
            NoteType noteType = NoteType.Bridge)
        {
            return new CRMNoteRequestModel
            {
                BOLTExternalId = externalId,
                TransType = nameof(TransitionType.CRMNote),
                NoteType = NoteTypeValues[noteType],
                Carrier = carrier,
                BOLTLOBCd = lobCode,
                Org = nameof(Orgs.ProgressivePL),
                SourceName = sourceName,
                RqUID = Guid.NewGuid(),
                ClientDt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };
        }
    }
}
