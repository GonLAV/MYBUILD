using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.TestData.CommonTestData
{
    public static class MessageData
    {
        public static readonly MessageModel MessageNoteData = new ()
        {
            Description= "Auto" + RandomManager.GetRandomString(15),
            Subject = "AutoSubject " + RandomManager.GetRandomString(6),
        };
    }

    public record MessageModel
    {
        public string? Description { get; set; }
        public string? Subject { get; set; }
    }

    
}
