
namespace Bolt.Automation.ApiClients.AdbxApi.Entities.Case
{
    public class GetCaseNotesResultModel
    {
        public bool IsBoltAccessCase { get; set; }
        public List<ChannelViewModel> Channels { get; set; }
        public List<NoteListItemModel> Notes { get; set; }
        public GetCaseNotesResultModel()
        {
            Notes = new List<NoteListItemModel>();
        }
    }

    public record ChannelViewModel(string ChannelType, string EntityType);
}
