using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface
{
    public interface IPageHelperFactory
    {
        IPageHelper CreateHelper(IPage page);
    }

    public class PageHelperFactory : IPageHelperFactory
    {
        private readonly IAutomationLogger? _logger;

        public PageHelperFactory(IAutomationLogger? logger = null)
        {
            _logger = logger;
        }

        public IPageHelper CreateHelper(IPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            return new PageHelper(page, _logger);
        }
    }
}
