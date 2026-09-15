using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.CreateConsumer;
using Bolt.Automation.ApiClients.CaseManagerApi.Entities.Consumers.UpdateConsumer;
using Refit;

namespace Bolt.Automation.ApiClients.CaseManagerApi.Interfaces
{
    public partial interface ICaseManagerConsumersApi
    {
        [Post("/{tenant}/consumers")]
        Task<ApiResponse<CreateUpdateConsumerResponse>> CreateConsumerAsync([AliasAs("tenant")] string tenant, [Body] CreateConsumerRequest request);

        [Put("/{tenant}/consumers")]
        Task<ApiResponse<CreateUpdateConsumerResponse>> UpdateConsumerAsync([AliasAs("tenant")] string tenant, [Body] UpdateConsumerRequest request);
    }
}
