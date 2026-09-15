using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Executor.Helpers;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.InterviewFlowHelpers;

namespace Bolt.Automation.FrontEnds.Executor.Services
{
    public class FlowPreparationService(
        PageFlowHelper flowHelper,
        IAutomationLogger? logger = null,
        IScopeContext? scopeContext = null) : IFlowPreparationService
    {
        private readonly PageFlowHelper _flowHelper = flowHelper ?? throw new ArgumentNullException(nameof(flowHelper));
        private readonly IAutomationLogger? _logger = logger;
        private readonly IScopeContext? _scopeContext = scopeContext;

        /// <summary>
        /// Prepares the flow with lazy loading - only loads the specific flow when needed.
        /// </summary>
        public (FlowsHelpers flow, int startIndex, int endIndex) PrepareFlow(
            Enum flowType,
            Type startType,
            Type endType,
            List<Type>? pagesToSkip,
            List<PageInsertion>? pagesToAdd = null)
        {
            // Lazy load the flow (only loads if not already registered)
            var originalFlow = FlowsHelpers.FlowRegistry.Get(flowType);
            _logger?.Debug($"Flow '{flowType}' loaded for execution");

            var flow = originalFlow.Clone();

            if (pagesToSkip?.Count > 0)
            {
                var removedCount = flow.Pages.RemoveAll(pagesToSkip.Contains);
                _logger?.Debug($"Skipped {removedCount} of {pagesToSkip.Count} requested pages from flow '{flowType}'");

                if (removedCount < pagesToSkip.Count)
                {
                    var notInFlow = pagesToSkip.Where(p => !originalFlow.Pages.Contains(p)).Select(p => p.Name);
                    _logger?.Warning($"Pages requested to skip but not present in flow '{flowType}': {string.Join(", ", notInFlow)}");
                }
            }

            if (pagesToAdd?.Count > 0)
            {
                foreach (var insertion in pagesToAdd)
                {
                    int anchorIndex = flow.Pages.IndexOf(insertion.After);
                    if (anchorIndex < 0)
                        throw new TestSetupException(
                            $"Cannot insert '{insertion.Page.Name}' after '{insertion.After.Name}' - anchor page not found in flow '{flowType}' (was it skipped?). Pages: {string.Join(", ", flow.Pages.Select(p => p.Name))}");

                    flow.Pages.Insert(anchorIndex + 1, insertion.Page);
                    _logger?.Debug($"Inserted '{insertion.Page.Name}' after '{insertion.After.Name}' in flow '{flowType}'");
                }
            }

            var (startIndex, endIndex) = GetPageIndices(flow, flowType, startType, endType);
            return (flow, startIndex, endIndex);
        }

        /// <summary>
        /// Gets the start and end indices for the flow pages.
        /// </summary>
        public (int startIndex, int endIndex) GetPageIndices(
            FlowsHelpers flow,
            Enum flowType,
            Type startType,
            Type endType)
        {
            int startIndex = flow.Pages.IndexOf(startType);
            int endIndex = flow.Pages.IndexOf(endType);

            _flowHelper.ValidateIndices(flow, flowType, startType, endType, startIndex, endIndex);
            return (startIndex, endIndex);
        }

        /// <summary>
        /// Merges flow default data with user-provided form data.
        /// </summary>
        public Dictionary<string, string> MergeFormData(
            Dictionary<string, object>? flowDefaults,
            Dictionary<string, string>? userFormData) =>
            MergeDataManager.MergeFormData(flowDefaults, userFormData);
    }
}