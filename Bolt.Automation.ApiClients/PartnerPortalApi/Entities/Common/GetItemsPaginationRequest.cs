
using Refit;

namespace Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common
{
    public class GetItemsPaginationRequest
    {
        public bool IsFirsLoad { get; set; }
        public string? TableName { get; set; }
        public int Take { get; set; }
        public int Skip { get; set; }
        public string? SortingBy { get; set; }
        public string? SortingOrder { get; set; }
        public string? SearchParam { get; set; }
        public bool IsSearch { get; set; }
        [Query(CollectionFormat.Multi)]
        public List<string> SearchColumns { get; set; }
        public List<string> AllowedFilter { get; set; }
    }
}
