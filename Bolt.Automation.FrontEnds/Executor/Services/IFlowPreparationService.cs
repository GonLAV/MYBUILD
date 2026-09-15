using Bolt.Automation.FrontEnds.InterviewFlowHelpers;

namespace Bolt.Automation.FrontEnds.Executor.Services
{
    /// <summary>
    /// Service responsible for preparing flows and merging form data
    /// </summary>
    public interface IFlowPreparationService
    {
        /// <summary>
        /// Prepares the flow and returns the flow object and start/end indices
        /// </summary>
        (FlowsHelpers flow, int startIndex, int endIndex) PrepareFlow(
            Enum flowType,
            Type startType,
            Type endType,
            List<Type>? pagesToSkip,
            List<PageInsertion>? pagesToAdd = null);

        /// <summary>
        /// Gets the start and end indices for the flow pages
        /// </summary>
        (int startIndex, int endIndex) GetPageIndices(
            FlowsHelpers flow,
            Enum flowType,
            Type startType,
            Type endType);

        /// <summary>
        /// Merges flow default data with user-provided form data
        /// </summary>
        Dictionary<string, string> MergeFormData(
            Dictionary<string, object>? flowDefaults,
            Dictionary<string, string>? userFormData);
    }
}