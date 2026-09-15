using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure
{
    public class PageFactory : IPageFactory
    {
        private readonly IBrowserManager _browserManager;
        private readonly IAutomationLogger _logger;
        private readonly IScopeContext _scopeContext;

        public PageFactory(IBrowserManager browserManager, IAutomationLogger logger, IScopeContext scopeContext)
        {
            _browserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
        }

        public IBase CreatePage(Type pageType)
        {
            if (!typeof(IBase).IsAssignableFrom(pageType))
            {
                throw new ArgumentException($"Type {pageType.Name} must implement IBase", nameof(pageType));
            }

            try
            {
                IPage page = _browserManager.GetCurrentTab() ?? _browserManager.GetPageAsync().GetAwaiter().GetResult();
                if (page == null)
                    throw new InvalidOperationException("No active page found or could not create a new page.");

                if (string.IsNullOrEmpty(page.Url) || page.Url == "about:blank")
                {
                    _logger.Warning($"Page is at about:blank or has no URL. This may indicate a navigation issue.");
                }

                var screenshotManager = _browserManager.GetScreenshotManager();
                var pageHelper = new PageHelper(page, _logger, screenshotManager, _scopeContext);

                object? instance;
                var constructorWithScopeContext = pageType.GetConstructor(new[] {
                    typeof(IBrowserManager),
                    typeof(IPageHelper),
                     typeof(IScopeContext),
                    typeof(bool),
                    typeof(IAutomationLogger)
                });

                instance = Activator.CreateInstance(pageType, _browserManager, pageHelper, _scopeContext, true, _logger);

                if (instance is null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {pageType.Name}.");
                }

                return (IBase)instance!;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create page instance of type {pageType.Name}: {ex.Message}");
                throw new PageCreationException(pageType.Name, ex);
            }
        }

        public T CreatePage<T>() where T : IBase
        {
            return (T)CreatePage(typeof(T));
        }
    }
}