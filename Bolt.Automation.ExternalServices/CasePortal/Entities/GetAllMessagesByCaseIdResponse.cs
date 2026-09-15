namespace Bolt.Automation.ExternalServices.CasePortal.Entities
{
    public record GetAllMessagesByCaseIdResponse
    {
        public ObjectProcessed? ObjectProcessed { get; init; }
        public string? Exception { get; init; }
        public string? Result { get; init; }
        public string? OutParameter { get; init; }
    }

    public record ObjectProcessed
    {
        public List<MessageItem> Messages { get; init; } = [];
    }

    public record MessageItem
    {
        public List<MessageFile> Files { get; init; } = [];
        public string? UserDetails { get; init; }
        public DateTime DateCreated { get; init; }
        public string? MessageAudience { get; init; }
        public MessageNote? Note { get; init; }
    }

    public record MessageFile
    {
        public string? Id { get; init; }
        public string? FileName { get; init; }
        public long FileSize { get; init; }
    }

    public record MessageNote
    {
        public string? Text { get; init; }
        public string? Subject { get; init; }
    }
}
