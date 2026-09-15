using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Cases.CreateCaseMessages;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Common;
using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Utils;

namespace Bolt.Automation.TestDataProvider.Providers.CaseManagerAPIDataProvider
{
    public static class CreateCaseMessageDataProvider
    {
        public static CreateCaseMessageRequest CreateCaseMessageData(IScopeContext scopeContext)
        {
            return new CreateCaseMessageRequest
            {
                Note = new NoteModel
                {
                    Text = "CM > ADBX " + RandomManager.GetRandomString(6),
                    Subject = "ADBX - Case Message " + RandomManager.GetRandomString(6),
                },
                MessageAudience = "Everyone",
                MessageDirection = "UnderwriterToAgent",
                DateCreated = EnvironmentUtils.GetCurrentDateTimeForEnvironment(scopeContext.Data.Environment.ToString())
            };
        }
    }
}
