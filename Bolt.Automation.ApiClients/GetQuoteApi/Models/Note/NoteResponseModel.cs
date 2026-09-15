namespace Bolt.Automation.ApiClients.GetQuoteApi.Models.Note
{
    public class NoteResponseModel
    {
        public string? Type { get; set; }
        public DateTime DateCreated { get; set; }
        public string? CreatedBy { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
    }
}
