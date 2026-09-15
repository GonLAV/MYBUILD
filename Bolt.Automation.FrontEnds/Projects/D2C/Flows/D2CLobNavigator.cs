using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.Executor;
using Bolt.Automation.FrontEnds.Interfaces;
using Bolt.Automation.FrontEnds.PlaywrightBase.Infrastructure.Interface;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;

namespace Bolt.Automation.FrontEnds.Projects.D2C.Flows
{
    /// <summary>
    /// Walks a <see cref="D2CLobFlow"/> from the address page to Rates, absorbing the one place the
    /// app's real page sequence diverges from the flow definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists so a test does not have to. Deciding which page the app landed on means reading
    /// the browser URL, which is raw Playwright at the test layer — and branching on UI state is
    /// exactly the kind of logic the project's design rules put in a component rather than in a
    /// test body. A test calls <see cref="NavigateToRatesAsync"/> and gets a Rates page.
    /// </para>
    /// <para>
    /// Deliberately separate from <c>D2CTestHelpers</c>: this carries no database or API
    /// dependency, so a suite validating a freshly onboarded tenant can use it without pulling in
    /// the tenant-keyed data stores.
    /// </para>
    /// </remarks>
    public class D2CLobNavigator(
        PlaywrightExecutor executor,
        IPageFactory pageFactory,
        IPageHelper pageHelper,
        IAutomationLogger? logger = null)
    {
        /// <summary>
        /// Generous on purpose: this wait spans the rating call the interview makes after House
        /// Details, not just a client-side route change.
        /// </summary>
        private const int ResumePageTimeoutMs = 30000;

        /// <summary>
        /// Walks <paramref name="lobFlow"/> from <see cref="D2C_YourAddressPage"/> to Rates.
        /// </summary>
        /// <remarks>
        /// A flow without a conditional step is a single <c>Execute</c>.
        /// <para>
        /// A flow WITH one (<see cref="D2CLobFlow.HasConditionalRoofStep"/>) is walked in two
        /// segments, because the flow definition is a fixed page list while the app's real sequence
        /// is not: roof-replacement is asked only for older houses, and house age comes from the
        /// address. Rather than teach the executor to tolerate a missing page, this stops at House
        /// Details, continues by hand, then resumes from whichever page the app actually rendered.
        /// <c>ExecuteToPage</c> derives its start index from the page object it is given, so
        /// resuming at Safety Alarms leaves roof-replacement behind the start index — nothing to
        /// skip and nothing to probe.
        /// </para>
        /// </remarks>
        public async Task<D2C_RatesPage> NavigateToRatesAsync(
            D2CLobFlow lobFlow,
            Dictionary<string, string> formData)
        {
            List<Type> pagesToSkip = [.. lobFlow.PagesToSkip];

            if (!lobFlow.HasConditionalRoofStep)
            {
                return await executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    lobFlow.FlowType,
                    formData: formData,
                    fillForms: true,
                    pagesToSkip: pagesToSkip);
            }

            var houseDetails = await executor.Execute<D2C_YourAddressPage, D2C_HouseDetailsPage>(
                lobFlow.FlowType,
                formData: formData,
                fillForms: true,
                pagesToSkip: pagesToSkip);

            // Execute stops short of processing its end page, so House Details is filled and
            // advanced here. FillForm resolves its values from the field registry, so this fills
            // exactly what a flow-driven step would.
            await houseDetails.FillForm(formData);
            await houseDetails.ClickContinue();

            var resumePage = await ResolvePageAfterHouseDetailsAsync();

            return await executor.ExecuteToPage<D2C_RatesPage>(
                lobFlow.FlowType,
                resumePage,
                formData,
                fillForms: true,
                pagesToSkip: pagesToSkip);
        }

        /// <summary>
        /// Waits for the app to settle on either the roof-replacement step or the page after it,
        /// and returns the matching page object to resume the flow from.
        /// </summary>
        /// <remarks>
        /// Matched on the pages' own <c>UrlPart</c> constants rather than local literals, so a
        /// page-identifier change cannot leave this silently waiting out its timeout. The page
        /// object for the branch NOT taken is never constructed — constructing one validates it and
        /// throws, which is precisely the outcome being avoided here.
        /// </remarks>
        private async Task<IInterview> ResolvePageAfterHouseDetailsAsync()
        {
            string[] candidates = [D2C_RoofReplacementPage.UrlPart, D2C_SafetyAlarms.UrlPart];

            var matched = await pageHelper.WaitForNavigationOrUrlContainsAsync(candidates, ResumePageTimeoutMs);

            if (matched == D2C_RoofReplacementPage.UrlPart)
            {
                logger?.Info("Roof replacement was asked for this property — resuming the flow there");
                return pageFactory.CreatePage<D2C_RoofReplacementPage>();
            }

            logger?.Info("Roof replacement was not asked for this property — resuming the flow at safety alarms");
            return pageFactory.CreatePage<D2C_SafetyAlarms>();
        }
    }
}
