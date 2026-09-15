using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Executor.Services;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace Bolt.Automation.FrontEnds.Executor
{
    public class PlaywrightExecutor(
        IFlowNavigationService navigationService,
        IFlowPreparationService preparationService,
        IFlowExecutionService executionService,
        PageFlowHelper flowHelper,
        IAutomationLogger? logger = null)
    {
        private readonly IFlowNavigationService _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        private readonly IFlowPreparationService _preparationService = preparationService ?? throw new ArgumentNullException(nameof(preparationService));
        private readonly IFlowExecutionService _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        private readonly PageFlowHelper _flowHelper = flowHelper ?? throw new ArgumentNullException(nameof(flowHelper));

        /// <summary>
        /// Executes a flow from the specified start page to the end page.
        /// </summary>
        public async Task<TEnd> Execute<TStart, TEnd>(
            Enum flowType,
            Dictionary<string, string>? formData = null,
            bool fillForms = true,
            List<Type>? pagesToSkip = null,
            List<PageInsertion>? pagesToAdd = null,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks = null,
            string? startUrl = null)
            where TStart : class, IInterview
            where TEnd : class, IInterview
        {
            logger?.Info($"Executing flow: {flowType}");
            if (!string.IsNullOrEmpty(startUrl))
                await _navigationService.EnsureOnStartPage(flowType, startUrl);
            else
                await _navigationService.EnsureOnStartPage(flowType);

            var (flow, startIndex, endIndex) = _preparationService.PrepareFlow(flowType, typeof(TStart), typeof(TEnd), pagesToSkip, pagesToAdd);
            var mergedFormData = _preparationService.MergeFormData(flow.DefaultData, formData);

            var startPage = _flowHelper.CreateAndValidatePage(flow.Pages[startIndex]);
            return await _executionService.ExecuteFlowPages<TEnd>(flow, startIndex, endIndex, (IInterview)startPage, mergedFormData, fillForms, pageCallbacks);
        }

        public async Task<TEnd> ExecuteWithStartUrl<TStart, TEnd>(
            Enum flowType,
            string startUrl,
            Dictionary<string, string>? formData = null,
            bool fillForms = true)
            where TStart : class, IInterview
            where TEnd : class, IInterview
        {
            return await Execute<TStart, TEnd>(
                flowType,
                formData,
                fillForms,
                pagesToSkip: null,
                pageCallbacks: null,
                startUrl: startUrl
            );
        }

        /// <summary>
        /// Executes a flow from the current page to the specified end page.
        /// </summary>
        public async Task<TEnd> ExecuteToPage<TEnd>(
            Enum flowType,
            IInterview currentPage,
            Dictionary<string, string>? formData = null,
            bool fillForms = true,
            List<Type>? pagesToSkip = null,
            List<PageInsertion>? pagesToAdd = null,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks = null,
            Func<IInterview, Task>? perPageAction = null)
            where TEnd : class, IInterview
        {
            if (currentPage == null)
                throw new ArgumentNullException(nameof(currentPage), $"{nameof(currentPage)} cannot be null when executing flow '{flowType}'");

            var (flow, currentIndex, endIndex) = _preparationService.PrepareFlow(flowType, currentPage.GetType(), typeof(TEnd), pagesToSkip, pagesToAdd);
            var mergedFormData = _preparationService.MergeFormData(flow.DefaultData, formData);

            return await _executionService.ExecuteFlowPages<TEnd>(
                flow, currentIndex, endIndex, currentPage, mergedFormData, fillForms, pageCallbacks, skipCurrentPageForm: false, perPageAction: perPageAction);
        }
    }

    /// <summary>
    /// Factory for creating PlaywrightExecutor instances with custom browser manager configurations
    /// </summary>
    public static class PlaywrightExecutorFactory
    {
        /// <summary>
        /// Creates a PlaywrightExecutor with a custom browser manager factory for test scenarios
        /// </summary>
        public static PlaywrightExecutor CreateWithBrowserManagerFactory(
            IServiceProvider provider,
            Func<IBrowserManager> browserManagerFactory)
        {
            var logger = provider.GetService<IAutomationLogger>();
            var scopeContext = provider.GetRequiredService<IScopeContext>();
            
            var navigationService = new FlowNavigationService(browserManagerFactory(), scopeContext, logger);
            var preparationService = provider.GetRequiredService<IFlowPreparationService>();
            var executionService = provider.GetRequiredService<IFlowExecutionService>();
            var flowHelper = provider.GetRequiredService<PageFlowHelper>();
            
            return new PlaywrightExecutor(navigationService, preparationService, executionService, flowHelper, logger);
        }
    }
}