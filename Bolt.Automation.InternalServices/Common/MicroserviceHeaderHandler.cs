using System.Net.Http.Headers;
using Bolt.Automation.Common.Context;

namespace Bolt.Automation.InternalServices.Common;

internal static class MicroserviceHeaderHandler
{
    private const string TenantHeader = "X-Bolt-Tenant";
    private const string CorrelationIdHeader = "X-Bolt-CorrelationId";

    private const string UserIdHeader = "X-Bolt-UserId";
    private const string WorkspaceGroupIdHeader = "X-Bolt-WorkspaceGroupId";

    public static void Apply(HttpRequestHeaders headers, IScopeContext scopeContext, string? requestMadeBy)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(scopeContext);

        var tenant = scopeContext.Data.Tenant?.ToString();
        AddIfNotNullOrWhiteSpace(headers, TenantHeader, tenant);

        AddIfNotNullOrWhiteSpace(headers, CorrelationIdHeader, Guid.NewGuid().ToString());

        var user = scopeContext.Data.CurrentUser;
        if (user is null)
            return;

        AddIfNotNullOrWhiteSpace(headers, UserIdHeader, user.Id);
        AddIfNotNullOrWhiteSpace(headers, WorkspaceGroupIdHeader, user.WorkSpaceGroupId);
    }

    private static void AddIfNotNullOrWhiteSpace(HttpRequestHeaders headers, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!headers.Contains(key))
        {
            headers.Add(key, value);
        }
    }
}


