using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;

namespace Bolt.Automation.FrontEnds.Executor.Services
{
    public class FlowExecutionService : IFlowExecutionService
    {
        private readonly PageFlowHelper _flowHelper;
        private readonly PageCallbackManager _callbackManager;
        private readonly IPageFactory _pageFactory;
        private readonly IFlowNavigationService _navigationService;
        private readonly IAutomationLogger? _logger;
        private readonly IScopeContext ScopeContext;


        public FlowExecutionService(PageFlowHelper flowHelper, PageCallbackManager callbackManager, IPageFactory pageFactory,
            IFlowNavigationService navigationService, IAutomationLogger? logger = null, IScopeContext? scopeContext = null)
        {
            _flowHelper = flowHelper ?? throw new ArgumentNullException(nameof(flowHelper));
            _callbackManager = callbackManager ?? throw new ArgumentNullException(nameof(callbackManager));
            _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            ScopeContext = scopeContext ?? throw new ArgumentNullException(nameof(scopeContext));
            _logger = logger;
        }

        public async Task<TEnd> ExecuteFlowPages<TEnd>(
            FlowsHelpers flow,
            int startIndex,
            int endIndex,
            IInterview currentPage,
            Dictionary<string, string> formData,
            bool fillForms,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks = null,
            bool skipCurrentPageForm = false,
            Func<IInterview, Task>? perPageAction = null)
            where TEnd : class, IInterview
        {
            var startTime = DateTime.UtcNow;
            int formsFilledCount = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                currentPage = await ExecuteSinglePage(flow, i, currentPage, formData, fillForms, pageCallbacks, endIndex, startIndex, skipCurrentPageForm, perPageAction);
                if (fillForms)
                    formsFilledCount++;
            }

            var duration = DateTime.UtcNow - startTime;
            var pageCount = endIndex - startIndex;
            var formSummary = formsFilledCount > 0 ? $", {formsFilledCount} form(s) filled" : "";
            _logger?.Info($"Flow segment completed: {pageCount} page(s) processed{formSummary} in {duration.TotalSeconds:F1}s");
            return await EnsureEndPageTypeAsync<TEnd>(currentPage);
        }

        public async Task<IInterview> ExecuteSinglePage(
            FlowsHelpers flow,
            int pageIndex,
            IInterview currentPage,
            Dictionary<string, string> formData,
            bool fillForms,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks,
            int endIndex,
            int startIndex,
            bool skipCurrentPageForm = false,
            Func<IInterview, Task>? perPageAction = null)
        {
            Type currentPageType = flow.Pages[pageIndex];

            bool shouldFillForm = fillForms && !(pageIndex == startIndex && skipCurrentPageForm);
            await ProcessPage(currentPage, currentPageType, formData, shouldFillForm, pageCallbacks, perPageAction);

            return (pageIndex < endIndex - 1 ? _flowHelper.CreateAndValidatePage(flow.Pages[pageIndex + 1]) : currentPage);
        }

        public async Task ProcessPage(
            IInterview page,
            Type pageType,
            Dictionary<string, string> formData,
            bool fillForms,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks,
            Func<IInterview, Task>? perPageAction = null)
        {
            var skipForm = await _callbackManager.RunPageCallbacks(pageCallbacks, pageType, page,
                PageCallbackManager.CallbackTiming.BeforeFillForm, PageCallbackManager.CallbackTiming.InsteadOfFillForm);

            if (fillForms && !skipForm)
            {
                await page.FillForm(formData);
            }
            else if (skipForm)
            {
                _logger?.Debug($"Form filling skipped for {pageType.Name}");
            }

            if (perPageAction != null)
                await perPageAction(page);

            await _callbackManager.RunPageCallbacks(pageCallbacks, pageType, page, PageCallbackManager.CallbackTiming.AfterFillForm);
            
            // Check if we should skip ClickContinue
            var skipClickContinue = await _callbackManager.RunPageCallbacks(pageCallbacks, pageType, page, 
                PageCallbackManager.CallbackTiming.InsteadOfClickContinue);
            
            if (!skipClickContinue)
            {
                await page.ClickContinue();
            }
            else
            {
                _logger?.Debug($"ClickContinue skipped for {pageType.Name} - InsteadOfClickContinue callback executed");
            }
            
            await Task.Delay(1000);
        }

        public async Task<TEnd> EnsureEndPageTypeAsync<TEnd>(IInterview page) where TEnd : class, IInterview
        {
            if (page is TEnd endPage) return endPage;

            if (_pageFactory.CreatePage(typeof(TEnd)) is TEnd expectedPage)
            {
                return expectedPage;
            }
            throw new NavigationException($"Expected end page type {typeof(TEnd).Name}, got {page.GetType().Name}");
        }
    }
}