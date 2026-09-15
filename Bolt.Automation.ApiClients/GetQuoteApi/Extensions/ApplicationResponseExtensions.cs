using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.Common.Context;

namespace Bolt.Automation.ApiClients.GetQuoteApi.Extensions
{
    public static class ApplicationResponseExtensions
    {
        /// <summary>
        /// Maps identifiers from a PostApplication API response to the scope context
        /// and appends a snapshot to <see cref="TestContextData.IdentifierHistory"/>:
        /// <list type="bullet">
        ///   <item><see cref="PostApplicationResponseModel{T}.Id"/> → <see cref="TestContextData.ExternalId"/></item>
        ///   <item><see cref="PostApplicationResponseModel{T}.FriendlyId"/> → <see cref="TestContextData.FriendlyId"/></item>
        ///   <item><see cref="PostApplicationResponseModel{T}.ApplicantId"/> → <see cref="TestContextData.ApplicantId"/></item>
        /// </list>
        /// </summary>
        public static PostApplicationResponseModel<T> MapIdentifiers<T>(
            this PostApplicationResponseModel<T> response,
            IScopeContext scopeContext)
        {
            if (response is null) return response;

            if (!string.IsNullOrEmpty(response.Id))
                scopeContext.Set(ctx => ctx.ExternalId, response.Id);

            if (!string.IsNullOrEmpty(response.FriendlyId))
                scopeContext.Set(ctx => ctx.FriendlyId, response.FriendlyId);

            if (!string.IsNullOrEmpty(response.ApplicantId))
                scopeContext.Set(ctx => ctx.ApplicantId, response.ApplicantId);

            scopeContext.Data.IdentifierHistory.Add(new IdentifierSnapshot
            {
                Source = "CreateApplication",
                ExternalId = response.Id,
                FriendlyId = response.FriendlyId,
                ApplicantId = response.ApplicantId
            });

            return response;
        }
    }
}
