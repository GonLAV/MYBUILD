
namespace Bolt.Automation.ApiClients.AdbxApi.Entities
{
    public class PaginationQuery
    {
        public string SortProperty { get; set; } = "dateCreated";
        public string SortOrder { get; set; } = "desc";
        public int? Take { get; set; } = 20;
        public int? Skip { get; set; } = 0;

        public static PaginationQuery Default => new PaginationQuery();
    }
}
