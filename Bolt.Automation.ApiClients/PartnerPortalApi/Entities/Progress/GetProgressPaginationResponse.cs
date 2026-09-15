using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Progress
{
    public class GetProgressPaginationResponse
    {
        public bool Success { get; set; }
        public int? ItemsCount { get; set; }
        public List<Invitation> Invitations { get; set; }
    }
}
