using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.Projects.ADBX.Pages.ADBX_UpdateLeadSourcePage;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    public class LeadSourceJourneyTests : AdbxUITestBase
    {

        [Test]
        [Tenant(Tenant.UNIFY)]
        [Category("ADBX")]
        [Category("Journeys")]
        [TestCaseId(254486)]
        [Author(Author.Sandy)]
        [Description("Conflicting Consumer Journeys are rejected when picked, never attached: a same-LOB cross-type pair and an Internal/Farmers pair each raise their own warning, while Farmers and External coexist across different LOBs")]
        public async Task UNIFY_LeadSource_JourneyTypes_Validation()
        {
            string LeadSourceName = "QATEST";
            var farmersAdmin = TestContextAccessor.CurrentUserCollection.FarmersAdmin!;

            var homePage = await AdbxHelper.LoginToHomeAsync(farmersAdmin, farmersAdmin.LoginUrl);
            var leadsSourcesPage = await AdbxHelper.NavigateToAdminMenuAsync<ADBX_LeadsSourcesPage>(homePage, "Leads Sources");

            var leadSourcePage = await _logger.ExecuteStepAsync(
                $"Open the lead source '{LeadSourceName}' and clear all journeys",
                async () =>
                {
                    var page = await leadsSourcesPage.OpenLeadSourceByNameAsync(LeadSourceName, PageFactory);
                    await page.WaitForJourneysLoadedAsync();
                    await page.ClearAllJourneysAsync();

                    Assert.That(await page.GetAttachedJourneysAsync(), Is.Empty);
                    return page;
                },
                "Expected result: the journeys field is emptied");

            await _logger.ExecuteStepAsync(
                $"Select '{HomeownersInternal}'",
                async () =>
                {
                    await leadSourcePage.SelectJourneyAsync(HomeownersInternal);

                    Assert.That(await leadSourcePage.GetAttachedJourneysAsync(), Is.EquivalentTo(new[] { HomeownersInternal }));
                },
                $"Expected result: '{HomeownersInternal}' is attached");

            await _logger.ExecuteStepAsync(
                $"Add '{HomeownersExternal}' - same Line of Business",
                async () =>
                {
                    await leadSourcePage.SelectJourneyAsync(HomeownersExternal);

                    Assert.That(await leadSourcePage.GetJourneysWarningAsync(), Is.EqualTo(SameLineOfBusinessWarning));
                    Assert.That(await leadSourcePage.GetAttachedJourneysAsync(), Is.EquivalentTo(new[] { HomeownersInternal }),
                        $"'{HomeownersExternal}' must not join a journey on the same Line of Business");
                },
                $"Expected result: the warning reads '{SameLineOfBusinessWarning}' and the journey is not added");

            await _logger.ExecuteStepAsync(
                $"Add '{DwellingFireFarmers}' - Farmers against Internal on a different Line of Business",
                async () =>
                {
                    await leadSourcePage.SelectJourneyAsync(DwellingFireFarmers);

                    Assert.That(await leadSourcePage.GetJourneysWarningAsync(), Is.EqualTo(OneJourneyTypePerSourceWarning),
                        "The Internal/Farmers conflict raises its own warning, distinct from the same-LOB one");
                    Assert.That(await leadSourcePage.GetAttachedJourneysAsync(), Is.EquivalentTo(new[] { HomeownersInternal }),
                        "Internal and Farmers cannot share a source even across different Lines of Business");
                },
                $"Expected result: the warning reads '{OneJourneyTypePerSourceWarning}' and the journey is not added");

            await _logger.ExecuteStepAsync(
                "Clear all journeys",
                async () =>
                {
                    await leadSourcePage.ClearAllJourneysAsync();

                    Assert.That(await leadSourcePage.GetAttachedJourneysAsync(), Is.Empty);
                },
                "Expected result: the journeys field is emptied");

            await _logger.ExecuteStepAsync(
                $"Add '{DwellingFireFarmers}' and '{HomeownersExternal}' - different Lines of Business",
                async () =>
                {
                    await leadSourcePage.SelectJourneyAsync(DwellingFireFarmers);
                    await leadSourcePage.SelectJourneyAsync(HomeownersExternal);

                    Assert.That(await leadSourcePage.GetAttachedJourneysAsync(),
                        Is.EquivalentTo(new[] { DwellingFireFarmers, HomeownersExternal }),
                        "Farmers and External share no Line of Business here, so both must attach");
                    Assert.That(await leadSourcePage.GetJourneysWarningAsync(), Is.Null,
                        "A source may hold more than one journey type when the types do not meet on a Line of Business");
                },
                "Expected result: both journeys are attached with no warning");

            await _logger.ExecuteStepAsync(
                $"Add '{HomeownersFarmers}' - same Line of Business",
                async () =>
                {
                    await leadSourcePage.SelectJourneyAsync(HomeownersFarmers);

                    Assert.That(await leadSourcePage.GetJourneysWarningAsync(), Is.EqualTo(SameLineOfBusinessWarning));
                    Assert.That(await leadSourcePage.GetAttachedJourneysAsync(),
                        Is.EquivalentTo(new[] { DwellingFireFarmers, HomeownersExternal }),
                        $"'{HomeownersFarmers}' and '{HomeownersExternal}' are both Homeowners, so the pick must be rejected");
                },
                $"Expected result: the warning reads '{SameLineOfBusinessWarning}' and the journey is not added");

            await _logger.ExecuteStepAsync(
                "Click Cancel",
                async () => await leadSourcePage.CancelAsync(PageFactory),
                "Expected result: the form is abandoned, nothing is saved, and the Leads Sources grid reopens");
        }
    }
}
