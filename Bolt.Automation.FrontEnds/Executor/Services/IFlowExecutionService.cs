using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;

namespace Bolt.Automation.FrontEnds.Executor.Services
{
    /// <summary>
    /// Service responsible for core flow execution logic
    /// </summary>
    public interface IFlowExecutionService
    {
        /// <summary>
        /// Executes the flow pages from startIndex to endIndex
        /// </summary>
        Task<TEnd> ExecuteFlowPages<TEnd>(
            FlowsHelpers flow,
            int startIndex,
            int endIndex,
            IInterview currentPage,
            Dictionary<string, string> formData,
            bool fillForms,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks = null,
            bool skipCurrentPageForm = false,
            Func<IInterview, Task>? perPageAction = null // Added parameter
        )
        where TEnd : class, IInterview;

        /// <summary>
        /// Executes a single page in the flow
        /// </summary>
        Task<IInterview> ExecuteSinglePage(
            FlowsHelpers flow,
            int pageIndex,
            IInterview currentPage,
            Dictionary<string, string> formData,
            bool fillForms,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks,
            int endIndex,
            int startIndex,
            bool skipCurrentPageForm = false,
            Func<IInterview, Task>? perPageAction = null // Added parameter
        );

        /// <summary>
        /// Processes a single page: runs callbacks, fills form, clicks continue, waits for page ready
        /// </summary>
        Task ProcessPage(
            IInterview page,
            Type pageType,
            Dictionary<string, string> formData,
            bool fillForms,
            Dictionary<Type, PageCallbackManager.PageCallbackConfig>? pageCallbacks,
            Func<IInterview, Task>? perPageAction = null // Added parameter
        );

        /// <summary>
        /// Ensures the current page is of the expected end page type
        /// </summary>
        Task<TEnd> EnsureEndPageTypeAsync<TEnd>(IInterview page) where TEnd : class, IInterview;
    }
}