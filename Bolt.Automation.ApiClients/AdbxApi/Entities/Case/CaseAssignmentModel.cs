
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public class CaseAssignmentModel
    {
        public Guid ResourceId { get; set; }
        public Guid? OwnerId { get; set; }
        public string? OwnerDisplayName { get; set; }
        public Guid? AssignedToId { get; set; }
        public string? AssignedToDisplayName { get; set; }
        public Guid? CreatorId { get; set; }
        public string? CreatorDisplayName { get; set; }
    }
}
