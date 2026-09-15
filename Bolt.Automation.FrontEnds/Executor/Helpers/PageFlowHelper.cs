using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;

namespace Bolt.Automation.FrontEnds.Executor.Helpers
{
    public class PageFlowHelper
    {
        private readonly IPageFactory _pageFactory;
        private readonly IAutomationLogger? _logger;

        public PageFlowHelper(IPageFactory pageFactory, IAutomationLogger? logger)
        {
            _pageFactory = pageFactory;
            _logger = logger;
        }

        public IInterview CreateAndValidatePage(Type pageType)
        {
            var page = _pageFactory.CreatePage(pageType)
                ?? throw new PageCreationException(pageType.Name);
            return (IInterview)page;
        }

        public void ValidateIndices(FlowsHelpers flow, Enum flowType, Type startType, Type endType, int startIdx, int endIdx)
        {
            if (startIdx < 0)
                throw new TestSetupException($"Start page '{startType.Name}' not found in flow '{flowType}'. Pages: {string.Join(", ", flow.Pages?.Select(p => p.Name) ?? [])}");
            if (endIdx < 0)
                throw new TestSetupException($"End page '{endType.Name}' not found in flow '{flowType}'. Pages: {string.Join(", ", flow.Pages?.Select(p => p.Name) ?? [])}");
            if (endIdx < startIdx)
                throw new TestSetupException($"End page '{endType.Name}' comes before start page '{startType.Name}' in flow '{flowType}'.");
            if (flow.Pages?.Count == 0)
                throw new TestSetupException($"Flow '{flow.FlowType}' has no pages after removing skipped pages.");
        }
    }
}
