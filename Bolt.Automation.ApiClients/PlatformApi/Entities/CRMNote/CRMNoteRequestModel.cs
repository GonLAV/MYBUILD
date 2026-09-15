using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PlatformApi.Entities.CRMNote
{
    public enum NoteType
    {
        BuyBundle,
        BuyMonoline,
        Bridge,
        INETChangedSelectedCarrier,
        RetrievalByMPS
    }

    public record CRMNoteRequestModel : AcordBaseRequest
    {
        public string? NoteType { get; set; }
        public string? BOLTExternalId { get; set; }
        public string? Carrier { get; set; }
        public string? BOLTLOBCd { get; set; }
        public string? ClientDt { get; set; }
    }
}
