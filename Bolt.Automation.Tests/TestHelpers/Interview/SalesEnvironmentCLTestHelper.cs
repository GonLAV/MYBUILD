using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.Tests.TestHelpers.ADBX;

namespace Bolt.Automation.Tests.TestHelpers.Interview;

/// <summary>
/// Opens a UNIFY sales-environment commercial quote and hands back the interview's first page. The
/// journey from there belongs to the test; shared answers live in <c>ApplicationTestData</c>.
/// </summary>
public class SalesEnvironmentCLTestHelper(
    IScopeContext scopeContext,
    AdbxTestHelper adbxHelper,
    IPageFactory pageFactory,
    TestContextAccessor testContextAccessor)
{
    /// <summary>What makes an account a sales-environment one.</summary>
    public const string AccountSource = "Sales Environment";

    /// <summary>Business profile these tests share, from TC 253224's policy data.</summary>
    public const string Address = "13215 Veterans Memorial Dr, Houston, TX 77014";
    public const string Industry = "Linen Supply";
    public const string Phone = "7134545454";

    /// <summary>Opens a quote for a brand-new commercial account, landing on the Business Profile page.</summary>
    /// <remarks>
    /// Everything defaults to the sales-environment profile; pass values to drive a different one. The
    /// names resolve per call because the registry defaults are per-process statics, so a whole run would
    /// otherwise share one name.
    /// </remarks>
    public async Task<ProductBusinessProfilePageCL> StartCommercialQuoteAsync(
        UserTestData? user = null,
        string accountSource = AccountSource,
        string address = Address,
        string phone = Phone)
    {
        var (_, businessProfilePage) = await adbxHelper.StartCommercialQuoteFromNewAccountAsync(
            user ?? ResolveSalesEnvironmentAdmin(),
            new Dictionary<string, string>
            {
                [ADBX_FieldNames.AccountSource] = accountSource,
                [ADBX_FieldNames.AccountBusinessName] = NameSelector.GetCompanyName(),
                [ADBX_FieldNames.AccountFirstName] = NameSelector.GetFirstName(),
                [ADBX_FieldNames.AccountLastName] = NameSelector.GetLastName(),
                [ADBX_FieldNames.AccountPhone] = phone,
                [ADBX_FieldNames.AccountAddress] = address,
            });

        return businessProfilePage;
    }

    /// <summary>Opens a quote on an existing commercial account, landing on the Business Profile page.</summary>
    /// <remarks>
    /// The account comes off the Accounts tab, so it carries whatever profile it already holds rather
    /// than <see cref="Address"/>.
    /// </remarks>
    public async Task<ProductBusinessProfilePageCL> StartCommercialQuoteFromExistingAccountAsync(UserTestData? user = null)
    {
        await adbxHelper.StartCommercialQuoteFromExistingAccountAsync(user ?? ResolveSalesEnvironmentAdmin());

        scopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.Interview);
        return pageFactory.CreatePage<ProductBusinessProfilePageCL>();
    }

    private UserTestData ResolveSalesEnvironmentAdmin() =>
        testContextAccessor.CurrentUserCollection.SalesEnvironmentAdmin
            ?? throw new TestSetupException("UNIFY Staging SalesEnvironmentAdmin user not configured");
}
