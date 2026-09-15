using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.TestDataProvider.Context;

namespace Bolt.Automation.Tests.TestHelpers;

/// <summary>
/// Runs an API call under a specific identity and restores the previous one afterwards.
/// </summary>
/// <remarks>
/// Touches only <c>CurrentUser</c> (API auth), never the browser session. Restoring matters because Refit
/// clients bind the identity at creation, so a leaked swap re-authenticates later calls as the wrong user.
/// </remarks>
public sealed class TestIdentityScope(IScopeContext scopeContext, TestContextAccessor testContextAccessor)
{
    /// <summary>The Admin identity — the only one carrying a GetQuote OAuth token.</summary>
    public UserTestData Admin => testContextAccessor.CurrentUserCollection.Admin
        ?? throw new InvalidOperationException(
            "No Admin user is configured for this tenant in this environment — required for GetQuote API auth");

    /// <summary>The Consumer identity — drives consumer quotes and the Progressive-facing Platform calls.</summary>
    public UserTestData Consumer => testContextAccessor.CurrentUserCollection.Consumer
        ?? throw new InvalidOperationException(
            "No Consumer user is configured for this tenant in this environment");

    public Task<T> AsAdminAsync<T>(Func<Task<T>> apiCall) => AsAsync(Admin, apiCall);

    public Task<T> AsConsumerAsync<T>(Func<Task<T>> apiCall) => AsAsync(Consumer, apiCall);

    public async Task<T> AsAsync<T>(UserTestData user, Func<Task<T>> apiCall)
    {
        var previousUser = scopeContext.Get(ctx => ctx.CurrentUser);
        scopeContext.Set(ctx => ctx.CurrentUser, user);
        try
        {
            return await apiCall();
        }
        finally
        {
            if (previousUser is not null)
            {
                scopeContext.Set(ctx => ctx.CurrentUser, previousUser);
            }
        }
    }
}
