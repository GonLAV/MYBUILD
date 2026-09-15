using Bolt.Automation.ApiClients.PlatformApi;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.Tests.TestExtension.Base;

public class ProgressiveUITestBase : UITestBase
{
    protected IPlatformApiClientFactory _platformApiFactory = null!;
    protected ProgressiveTestHelper _progressiveHelper = null!;

    protected ProgressiveUITestBase() : base()
    {
        ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
    }

    protected override void ResolveServices()
    {
        _platformApiFactory = _uiTestScope.ServiceProvider.GetRequiredService<IPlatformApiClientFactory>();
    }

    protected override void InitializeComponents()
    {
        _progressiveHelper = new ProgressiveTestHelper(
            _logger, ScopeContext, Executor, PageFactory, _platformApiFactory, BrowserManager);
    }
}
