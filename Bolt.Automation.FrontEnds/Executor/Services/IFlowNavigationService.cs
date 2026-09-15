namespace Bolt.Automation.FrontEnds.Executor.Services
{
    /// <summary>
    /// Service responsible for flow navigation and environment management
    /// </summary>
    public interface IFlowNavigationService
    {
        /// <summary>
        /// Ensures the browser is on the start page for the flow
        /// </summary>
        Task EnsureOnStartPage(Enum flowType);

        /// <summary>
        /// Ensures the browser is on the start page for the flow, with optional startUrl override
        /// </summary>
        Task EnsureOnStartPage(Enum flowType, string? startUrl);

        /// <summary>
        /// Gets the start URL for the specified flow type
        /// </summary>
        //string? GetFlowStartUrl(Enum flowType);

    }
}