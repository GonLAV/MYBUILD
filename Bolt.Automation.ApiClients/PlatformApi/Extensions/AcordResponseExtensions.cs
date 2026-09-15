using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.Common.Context;

namespace Bolt.Automation.ApiClients.PlatformApi.Extensions
{
    public static class AcordResponseExtensions
    {
        /// <summary>
        /// Maps <see cref="AcordBaseResponse.BoltExternalId"/> from the API response
        /// to <see cref="TestContextData.ExternalId"/> in the current scope context
        /// and appends a snapshot to <see cref="TestContextData.IdentifierHistory"/>.
        /// </summary>
        public static T MapExternalId<T>(this T response, IScopeContext scopeContext) where T : AcordBaseResponse
        {
            if (response is null) return response;

            var externalId = response.BoltExternalId;
            if (!string.IsNullOrEmpty(externalId))
            {
                scopeContext.Set(ctx => ctx.ExternalId, externalId);
                scopeContext.Data.IdentifierHistory.Add(new IdentifierSnapshot
                {
                    Source = "QuoteStart",
                    ExternalId = externalId
                });
            }

            return response;
        }
    }
}
