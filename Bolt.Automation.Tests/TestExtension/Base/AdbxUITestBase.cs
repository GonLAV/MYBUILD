using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.Tests.TestExtension.Base
{
    /// <summary>
    /// Base for UI tests whose front end is ADBX. Sets the front end once and owns the ADBX helpers,
    /// so fixtures no longer each declare and rebuild them.
    /// </summary>
    /// <remarks>
    /// The helpers are lazy rather than built in <c>InitializeComponents</c> because
    /// <c>_pageHelper</c> and the browser are not populated until <see cref="UITestBase"/>'s [SetUp]
    /// runs, which is after the constructor. Deferring to first use gets the same correctness with no
    /// override in every fixture. Caching is safe because the assembly runs
    /// <c>FixtureLifeCycle.InstancePerTestCase</c>, so each test case gets a fresh instance and
    /// therefore fresh helpers.
    /// </remarks>
    public abstract class AdbxUITestBase : UITestBase
    {
        private AdbxTestHelper? _adbxHelperField;
        private SsoHelper? _ssoHelperField;

        protected AdbxUITestBase() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
        }

        /// <summary>SSO helper, resolved off the test scope on first use.</summary>
        protected SsoHelper SsoHelper => _ssoHelperField ??= new SsoHelper(
            _logger, ScopeContext, _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>());

        /// <summary>
        /// ADBX helper for this test case. It knows nothing about SSO - for an SSO login use
        /// <see cref="SsoHelper"/> above, which sits right beside this one in IntelliSense.
        /// </summary>
        protected AdbxTestHelper AdbxHelper => _adbxHelperField ??= new AdbxTestHelper(
            _logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
    }
}
