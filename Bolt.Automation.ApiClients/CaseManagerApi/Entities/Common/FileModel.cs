namespace Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common
{
    public class FileModel
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        //Should base 64 string
        public string Data { get; set; }
    }
}
