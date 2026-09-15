namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Task
{
    public class TaskResponseModel
    {
        public DateTime? DateCreated { get; set; }
        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
    }
}
