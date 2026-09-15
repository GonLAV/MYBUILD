using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using ValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;


namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class PgrPrefillTests : ProgressiveUITestBase
    {
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [Category("Prefill")]
        [TestCaseId(227409)]
        [Description("Verifies that three prefill questions display empty values initially and property summary updates after submission")]
        public async Task PGR_HQX2_3PQ_Displayed_Property_Summary_Updated_After_Second_Call()
        {
            await _logger.ExecuteStepAsync("Setup: Call QuoteStart API with prefill data", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
                var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.Mapped_OH_Dayton);
                var (_, startUrl) = await _progressiveHelper.CallQuoteStartAsync(request,true);
            });

            var threePrefillQuestionsPage = PageFactory.CreatePage<HQXConusmer_3PQ>();

            await _logger.ExecuteStepAsync("Verify prefill fields are empty on 3PQ page", async () =>
            {
                await _logger.ExecuteStepAsync("Check 'What year was this home built?' field is empty", async () =>
                {
                    string yearBuiltValue = await _pageHelper.GetFieldValue(PLYearBuilt);
                    Assert.That(string.IsNullOrEmpty(yearBuiltValue), Is.True, "Year built field should be empty");
                });

                await _logger.ExecuteStepAsync("Check 'What is the square footage of this home?' field is empty", async () =>
                {
                    string squareFootageValue = await _pageHelper.GetFieldValue(PLSquareFootage);
                    Assert.That(string.IsNullOrEmpty(squareFootageValue), Is.True, "Square footage field should be empty");
                });

                await _logger.ExecuteStepAsync("Check 'What is the style of architecture for this home?' field shows default", async () =>
                {
                    string architectureStyleValue = await _pageHelper.GetFieldValue(ArchitectureStyle, ValueType.Value);
                    Assert.That(architectureStyleValue, Is.EqualTo("Select"));
                });
            });

            await _logger.ExecuteStepAsync("Fill 3PQ fields and submit", async () =>
            {
                await _pageHelper.InteractWithField(PLYearBuilt, "2022");
                await _pageHelper.InteractWithField(PLSquareFootage, "1,234");
                await _pageHelper.InteractWithField(ArchitectureStyle, "Basic");
                await threePrefillQuestionsPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Validate property section values in overview page", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXConsumer_OverviewPage>();
                bool propertySectionValid = await overviewPage.ValidateSectionValues(HQXConsumer_OverviewPage.SectionNames.Property,
                    "Single Family House",
                    "Basic",
                    "Year built 2022",
                    "1,234 Sq. Ft"
                );
                Assert.That(propertySectionValid, Is.True, "Property section values are not displayed as expected in overview.");
            });
        }
    }
}
