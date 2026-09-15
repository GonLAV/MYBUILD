using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.Users;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.AdbxTests
{
    /// <summary>
    /// Pure-API coverage of the ADBX journeys endpoints. Unlike its siblings in this folder there
    /// is no browser here, so it derives from <see cref="TestBase"/> rather than UITestBase.
    /// </summary>
    public class JourneyTests : TestBase
    {
        private const string FarmersJourney = "Farmers";

        private IAdbxApiClientFactory _adbxApiFactory = null!;

        protected override void ResolveServices()
        {
            _adbxApiFactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
        }

        /// <summary>
        /// Authenticates as <paramref name="user"/> and reads the journey types ADBX offers them.
        /// The client factory keys its cache on tenant+username, so switching CurrentUser and
        /// asking again mints a second client with its own STS token; both stay usable.
        /// </summary>
        private async Task<IReadOnlyList<string>> GetAvailableJourneyTypesAsync(UserTestData user)
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var api = await _adbxApiFactory.CreateApiClientAsync();

            var journeyTypes = await api.GetAvailableJourneyTypes()
                .EnsureSuccessContentAsync($"Failed to read available journey types for '{user.Username}'");

            return journeyTypes.ToList();
        }

        private static bool IsFarmersJourney(string journeyType) =>
            string.Equals(journeyType, FarmersJourney, StringComparison.OrdinalIgnoreCase);

        private static string Describe(IReadOnlyList<string> journeyTypes) =>
            journeyTypes.Count == 0 ? "(none)" : string.Join(", ", journeyTypes);

        [Test]
        [Tenant(Tenant.UNIFY)]
        [Category("ADBX")]
        [Category("Journeys")]
        [Author(Author.Sandy)]
        [TestCaseId(254487)]
        [Description("Available journey types are scoped per subtenant: Farmers is offered to the farmersdhub admin, withheld from the marketlib agent, and is the only difference between the two lists")]
        public async Task UNIFY_AvailableJourneyTypes_Differ_By_Subtenant_Test()
        {
            var marketLibJourneyTypes = await _logger.ExecuteStepAsync(
                "Read available journey types as the marketslib agent, whose subtenant is not configured for Farmers",
                async () =>
                {
                    var journeyTypes = await GetAvailableJourneyTypesAsync(TestContextAccessor.CurrentUserCollection.MarketLibAgent!);

                    Assert.That(journeyTypes.Any(IsFarmersJourney), Is.False,
                        $"Marketslib agent should not be offered the '{FarmersJourney}' journey, got: {Describe(journeyTypes)}");

                    return journeyTypes;
                },
                $"Expected result: the call succeeds and '{FarmersJourney}' is not among the journey types returned");

            await _logger.ExecuteStepAsync(
                "Read available journey types as the farmersdhub admin, whose subtenant is configured for Farmers",
                async () =>
                {
                    var journeyTypes = await GetAvailableJourneyTypesAsync(TestContextAccessor.CurrentUserCollection.FarmersAdmin!);

                    Assert.Multiple(() =>
                    {
                        Assert.That(journeyTypes.Any(IsFarmersJourney), Is.True,
                            $"farmersdhub admin should be offered the '{FarmersJourney}' journey, got: {Describe(journeyTypes)}");

                        // Deliberately not an exact count. How many journey types a subtenant is offered
                        // is configuration and moves between environments; what the story fixes is that
                        // Farmers is the ONLY thing the two subtenants differ by, which holds everywhere.
                        Assert.That(journeyTypes.Where(type => !IsFarmersJourney(type)), Is.EquivalentTo(marketLibJourneyTypes),
                            $"apart from '{FarmersJourney}' both users should be offered the same journey types - farmersdhub: {Describe(journeyTypes)}, marketslib: {Describe(marketLibJourneyTypes)}");
                    });
                },
                $"Expected result: the call succeeds, '{FarmersJourney}' is among the journey types returned, and it is the only entry the two lists differ by");
        }
    }
}
