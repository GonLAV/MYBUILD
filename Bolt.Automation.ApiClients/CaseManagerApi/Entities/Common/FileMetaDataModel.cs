
namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common
{
    public class FileMetadataModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
    }
}
