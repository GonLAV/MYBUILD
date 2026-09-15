using Bolt.Automation.ApiClients.PartnerPortalApi.Entities.Common;

namespace Bolt.Automation.TestDataProvider.TestData.PartnerPortalTestData
{
    public static class ProgressPaginationRequestTestData
    {
        public static GetItemsPaginationRequest Create(string searchParam)
        {
            return new GetItemsPaginationRequest
            {
                SearchColumns = new List<string>
                {
                    "FirstName",
                    "LastName",
                    "Email",
                    "InviteStatus",
                    "ExternalId"
                },
                Take = 10,
                Skip = 0,
                SearchParam = searchParam,
                IsSearch = true,
                IsFirsLoad = false,
                TableName = "InviteDetails"
            };
        }
    }
}
