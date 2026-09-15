using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Base;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Tooltips;
using Bolt.Automation.TestDataProvider.Extensions;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestData.Progressive;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Playwright;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using ValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;


namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class HQXConsumerTests : ProgressiveUITestBase
    {
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("HQX2")]
        [Category("Regression")]
        [TestCaseId(83070)]        
        [Author(Author.Helen)]
        [Description("Bridge to carrier via Finish & Buy, browser back lands on Online Buy kick-out, quote retrieval returns to carrier site.")]
        public async Task PGR_HQX2_OnlineBuy_KickOut_After_BrowserBack_FromCarrier()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var address = Addresses.GetAddress(AddressKey.OH);
            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(address);

            await _progressiveHelper.CallQuoteStartAsync(request, true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();
            var (friendlyId, lastName, zipCode) = await _logger.ExecuteStepAsync("Capture quote friendly id from Overview page", async () =>
            {
                var id = await overviewPage.GetFriendlyId();
                Assert.That(id, Is.Not.Null.And.Not.Empty.And.Not.EqualTo("NOT Found"),
                    "Failed to capture quote friendly id from Overview page");
                return (id, request.ApplicantDetails!.Surname!, request.PropertyAddr!.PostalCode!);
            });

            var ratesPage = await _logger.ExecuteStepAsync("Navigate from Overview to Rates without re-entering the deeplink", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_RatesPage>(FlowType.HQXShortFlow, overviewPage, fillForms: false);
                return PageFactory.CreatePage<HQXConsumer_RatesPage>();
            });

            await _logger.ExecuteStepAsync("Validate Rates page shows at least one carrier and ensure an online-buy-capable one is selected", async () =>
            {
                var mainBrickCarrier = await ratesPage.GetSelectedCarrierNameAsync();
                Assert.That(mainBrickCarrier, Is.Not.Null.And.Not.Empty,
                    "Rates page is displayed but no carrier is shown on the main rate card");

                var selected = await ratesPage.EnsureOnlineBuyCarrierSelectedAsync(
                    [CarrierEnums.Homesite.ToString(), CarrierEnums.Progressive.ToString()]);
                _logger.Info($"Selected online-buy carrier: {selected}");
            });

            var ratesUrl = _currentPage!.Url;
            await _logger.ExecuteStepAsync("Click Finish & Buy and verify user is bridged away from the rates page", async () =>
            {
                await ratesPage.ClickFinishAndBuyAsync();
                await _currentPage!.WaitForURLAsync(url => !url.Contains("/rates", StringComparison.OrdinalIgnoreCase),
                    new PageWaitForURLOptions { Timeout = 30_000 });
                Assert.That(_currentPage.Url, Is.Not.EqualTo(ratesUrl), "URL did not change after Finish & Buy click");
            });

            await _logger.ExecuteStepAsync("Browser back to Bolt - verify Online Buy kick-out is displayed", async () =>
            {
                await _pageHelper!.ClickBrowserBackButton();
                PageFactory.CreatePage<HQXConsumer_OnlineBuyKickOutPage>();
            });

            await _logger.ExecuteStepAsync("Retrieve quote and verify the API returns a carrier-site URL", async () =>
            {
                var (_, url) = await _progressiveHelper.CallQuoteRetrievalAsync(friendlyId, lastName, zipCode, navigate: false);
                Assert.That(url, Is.Not.Null.And.Not.Empty, "Quote retrieval did not return a URL");

                var host = new Uri(url!).Host;
                string[] boltHostSuffixes = ["boltqa.com", "bolttest.com", "boltinc.com"];
                Assert.That(boltHostSuffixes.Any(s => host.EndsWith(s, StringComparison.OrdinalIgnoreCase)), Is.False,
                    $"Retrieval did not return a carrier-site URL - host is still on a Bolt domain: {url}");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [TestCaseSource(typeof(HQXConsumerTestCases), nameof(HQXConsumerTestCases.DogBreedCases))]
        [Description("Verifies that dog breed and bite history questions are hidden by default in HQX 2.0 Homeowners flow")]
        public async Task PGR_HQX2_HO3_Hide_DogsBreed_BiteHistory(AddressKey addressKey)
        {
            var address = Addresses.GetAddress(addressKey);
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(address);
            await _progressiveHelper.CallQuoteStartAsync(request);

            await _logger.ExecuteStepAsync("Navigate to Details Page", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_DetailsPage>(FlowType.HQXShortFlow, null, false);
            });

            await _logger.ExecuteStepAsync("Check and interact with 'AnimalsOnThePremises_Dogs' field", async () =>
            {
                bool dogsFieldExists = await _pageHelper.ElementExists(AnimalsOnThePremises_Dogs);
                Assert.That(dogsFieldExists, Is.True, "Failed, There are dog(s) living on this property question is not displayed");

                await _pageHelper.InteractWithField(AnimalsOnThePremises_Dogs, "true");
            });

            await _logger.ExecuteStepAsync("Assert 'DogsWithBiteHistory' and 'DogsBreedsSelection' are hidden", async () =>
            {
                bool biteHistoryExists = await _pageHelper.ElementExists(DogsWithBiteHistory);
                bool breedsSelectionExists = await _pageHelper.ElementExists(DogsBreedsSelection);
                Assert.That(biteHistoryExists, Is.False, "Failed, Have any of the dogs ever bitten anyone? question is displayed");
                Assert.That(breedsSelectionExists, Is.False, "Failed, Select the dominant breed(s)? question is displayed");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(197878)]
        [Category("Regression")]
        [Category("HQX2")]
        [Author(Author.Helen)]
        [Description("CR 1350: Verifies pool and hot tub safety barrier selection sync in HQX 2.0 (FL).")]
        public async Task PGR_HQX2_Safety_Barriers_sync_between_Pool_and_HotTub()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.FL));

            await _progressiveHelper.CallQuoteStartAsync(request);

            await _logger.ExecuteStepAsync("Navigate to Details Page", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_DetailsPage>(FlowType.HQXShortFlow, null, false);
            });

            await _logger.ExecuteStepAsync("Enable Pool and Hot Tub", async () =>
            {
                await _pageHelper.InteractWithField(PL_AdditionalStructures_Pool, "true");
                await _pageHelper.InteractWithField(PL_AdditionalStructures_HotTub, "true");
            });

            var barrierPairs = new (string Pool, string HotTub)[]
            {
                (PL_PoolFeatures_4ftFence, PL_HotTubFeatures_4ftFence),
                (PL_PoolFeatures_ScreenEnclosure, PL_HotTubFeatures_ScreenEnclosure),
                (PL_PoolFeatures_SurroundingWall, PL_HotTubFeatures_SurroundingWall)
            };

            await _logger.ExecuteStepAsync("Validate child questions for Pool and Hot Tub", async () =>
            {
                foreach (var (pool, hotTub) in barrierPairs)
                {
                    var poolExists = await _pageHelper.ElementExists(pool);
                    var hotTubExists = await _pageHelper.ElementExists(hotTub);
                    Assert.That(poolExists, Is.True, $"Failed, child question {pool} is not displayed");
                    Assert.That(hotTubExists, Is.True, $"Failed, child question {hotTub} is not displayed");
                }
            });

            foreach (var (pool, hotTub) in barrierPairs)
            {
                await _logger.ExecuteStepAsync($"Validate barrier sync for {pool} and {hotTub}", async () =>
                {
                    await _pageHelper.InteractWithField(pool, "true");
                    var hotTubChecked = await _pageHelper.GetFieldValue(hotTub, ValueType.IsChecked);
                    Assert.That(hotTubChecked, Is.EqualTo("True"));

                    await _pageHelper.InteractWithField(pool, "false");
                    hotTubChecked = await _pageHelper.GetFieldValue(hotTub, ValueType.IsChecked);
                    Assert.That(hotTubChecked, Is.EqualTo("False"));
                });
            }
        }

        //TODO - Create additional tests for DF and MFH (Exclude FL, only run for OH and NY)
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [TestCaseSource(typeof(HQXConsumerTestCases), nameof(HQXConsumerTestCases.CoverageVerificationCases))]
        [Description("Validates coverage display names and help text tooltips are correct for different LOBs and states")]
        public async Task PGR_HQX2_Coverages_Verification_DisplayNames_HelpText(LOBEnums lob, AddressKey addressKey)
        {
            var address = Addresses.GetAddress(addressKey);
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(address);
            request.LOBCd = lob.ToPlatformApiCode();

            await _progressiveHelper.CallQuoteStartAsync(request);

            var ratesPage = await _progressiveHelper.NavigateToRatesPageAsync();

            var validation = await _logger.ExecuteStepAsync("Validate coverage display names and tooltips", async () =>
            {
                var state = address.StateProvCd?.Trim().ToUpperInvariant() ?? string.Empty;
                return await ratesPage.ValidateCoverageDisplayAsync(lob, state, log: true);
            });

            await _logger.ExecuteStepAsync("Assert validation results", async () =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(validation.CoverageTitles.Missing, Is.Empty,
                        $"Missing coverage titles: {string.Join(", ", validation.CoverageTitles.Missing)}");
                    Assert.That(validation.DeductibleTitles.Missing, Is.Empty,
                        $"Missing deductible titles: {string.Join(", ", validation.DeductibleTitles.Missing)}");
                    Assert.That(validation.TooltipIssues, Is.Empty,
                        $"Tooltip mismatches detected: {string.Join("; ", validation.TooltipIssues.Select(i => $"{i.Title} - {i.Reason}"))}");
                });
            });
        }


        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [TestCaseId(122695)]
        [Category("Sanity")]
        [Category("HQX2")]
        [Description("Validates questions relevancy and help text tooltips for FL")]
        public async Task PGR_HQX2_Questions_Relevancy_FL()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.FL));

            await _progressiveHelper.CallQuoteStartAsync(request, true);

            var labels = ProgressiveTestHelper.GetFieldLabels(
                PL_FlooringMaterial,
                PL_InteriorWallMaterial,
                NumberOfFirePlaces,
                PL_HeatedByOil,
                DeadBoltLocks,
                WindowAndOpeningProtection);

            async Task<TPage> ValidateQuestionsAsync<TPage>(
               string stepName,
               Func<TPage> createPage,
               Func<TPage, Task<IReadOnlyList<string>>> getQuestions,
               IEnumerable<string> excludedLabels,
               IEnumerable<string>? includedLabels = null,
               Func<TPage, Task>? afterAction = null)
               where TPage : HQXConsumerBase
            {
                return await _logger.ExecuteStepAsync(stepName, async () =>
                {
                    var page = createPage();
                    var questions = await getQuestions(page);
                    var (unexpectedlyDisplayed, missing) = _progressiveHelper.GetQuestionLabelDiff(
                        questions, excludedLabels, includedLabels, page.PageDisplayName);
                    Assert.Multiple(() =>
                    {
                        Assert.That(unexpectedlyDisplayed, Is.Empty,
                            $"{page.PageDisplayName}: excluded questions are displayed: {string.Join(", ", unexpectedlyDisplayed)}");
                        Assert.That(missing, Is.Empty,
                            $"{page.PageDisplayName}: expected questions are not displayed: {string.Join(", ", missing)}");
                    });

                    if (afterAction != null)
                    {
                        await afterAction(page);
                    }

                    return page;
                });
            }

            await ValidateQuestionsAsync(
                "Overview page - validate interior questions",
                () => PageFactory.CreatePage<HQXConsumer_OverviewPage>(),
                async page =>
                {
                    await page.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.Interior);
                    return await page.GetQuestionLabelsAsync(HQXConsumer_OverviewPage.SectionNames.Interior);
                },
                [labels[PL_FlooringMaterial], labels[PL_InteriorWallMaterial], labels[NumberOfFirePlaces]],
                afterAction: page => page.ClickContinue());

            await ValidateQuestionsAsync(
                "Details page - validate questions",
                () => PageFactory.CreatePage<HQXConsumer_DetailsPage>(),
                page => page.GetQuestionLabelsAsync(),
                [labels[PL_HeatedByOil]],
                afterAction: async page =>
                {
                    await page.FillForm();
                    await page.ClickContinue();
                });

            var discountsPage = await ValidateQuestionsAsync(
                "Discounts page - validate questions",
                () => PageFactory.CreatePage<HQXConsumer_DiscountsPage>(),
                page => page.GetQuestionLabelsAsync(),
                [labels[PL_FlooringMaterial], labels[PL_InteriorWallMaterial], labels[NumberOfFirePlaces], labels[DeadBoltLocks]],
                [labels[WindowAndOpeningProtection]]);

            await _logger.ExecuteStepAsync("Discounts page - validate WindowAndOpeningProtection tooltip", async () =>
            {
                var tooltipService = new TooltipValidationService(discountsPage.Page, _pageHelper, _logger);
                var issues = await tooltipService.ValidateTooltipsAsync(
                    new Dictionary<string, string>
                    {
                        [labels[WindowAndOpeningProtection]] =
                            "All Windows, Doors and Garages are protected: All openings such as windows, skylights, entry doors, and garage doors are protected by impact-tested and certified hurricane materials. Plywood is not considered an acceptable material. A wind mitigation form must be completed by a licensed inspector to qualify for this credit. " +
                            "All Windows are protected: All windows and skylights are protected by impact-tested and certified hurricane materials. Plywood is not considered an acceptable material. A wind mitigation form must be completed by a licensed inspector to qualify for this credit."
                    });

                Assert.That(issues, Is.Empty, $"Tooltip validation failed: {string.Join("; ", issues)}");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(238395)]
        [Category("Sanity")]
        [Category("HQX2")]
        [Author(Author.Helen)]
        [Description("Validates previous address field shows/hides based on YearsAtAddress and navigates to Owner page on continue")]
        public async Task PGR_HQX2_Previous_Address_Display_Rules()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park));
            await _progressiveHelper.CallQuoteStartAsync(request);

            var previousAddressLocator = _currentPage!.Locator(DiscountsFields.Fields[PreviousAddress].Locators.First());

            var discountsPage = await _logger.ExecuteStepAsync("Navigate to Discounts page and verify previous address appears when YearsAtAddress is 0", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_DiscountsPage>(FlowType.HQXShortFlow, null, true);
                var page = PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                await _pageHelper!.InteractWithField(YearsAtAddress, "0");
                bool previousAddressExists = await _pageHelper.ElementExists(PreviousAddress);
                Assert.That(previousAddressExists, Is.True, "Previous address field is not displayed when YearsAtAddress is 0");
                return page;
            });

            await _logger.ExecuteStepAsync("Change YearsAtAddress to 3 and verify previous address field disappears", async () =>
            {
                await _pageHelper!.InteractWithField(YearsAtAddress, "3");
                bool disappeared = await _pageHelper.WaitForElementToDisappearAsync(previousAddressLocator, timeout: 5000);
                Assert.That(disappeared, Is.True, "Previous address field is still displayed when YearsAtAddress is 3");
            });

            await _logger.ExecuteStepAsync("Change YearsAtAddress to 1, fill previous address via Google suggest and click Continue to verify Owner page is displayed", async () =>
            {
                await discountsPage.FillForm(new Dictionary<string, string> { [YearsAtAddress] = "1" });
                await _pageHelper!.InteractWithField(PreviousAddress, "123 Main St");
                await discountsPage.ClickContinue();
                PageFactory.CreatePage<HQXConsumer_OwnerPage>();
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("Sanity")]
        [Category("HQX2")]
        [TestCaseId(238441)]
        [Description("Validates previous address compound field component: placeholder, unit field, Google suggest, manual entry fields and validation")]
        [Author(Author.Helen)]
        public async Task PGR_HQX2_Previous_Address_Manual_Entry()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park));
            await _progressiveHelper.CallQuoteStartAsync(request);

            var previousAddressLocator = _currentPage!.Locator(DiscountsFields.Fields[PreviousAddress].Locators.First());

            var discountsPage = await _logger.ExecuteStepAsync("Navigate to Discounts page and set YearsAtAddress to 0", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_DiscountsPage>(FlowType.HQXShortFlow, null, true);
                var page = PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                await _pageHelper!.InteractWithField(YearsAtAddress, "0");
                return page;
            });

            await _logger.ExecuteStepAsync("Click Continue with empty previous address and verify validation message", async () =>
            {
                await discountsPage.ClickContinue();
                var message = await _pageHelper!.GetFieldValidationMessageAsync("PreviousAddress.AddressLine1");
                Assert.That(message, Is.Not.Null.And.Not.Empty, "No validation message is presented for the empty previous address field");
                await previousAddressLocator.ScrollIntoViewIfNeededAsync();
            });

            await _logger.ExecuteStepAsync("Verify previous address field has Address Details placeholder and has a unit field", async () =>
            {
                bool hasAddressDetailsPlaceholder = await _pageHelper!.ElementExists(PreviousAddress, expectedPlaceholder: "Address Details");
                bool unitFieldExists = await _pageHelper.ElementExists(PreviousAddressUnit);
                Assert.That(hasAddressDetailsPlaceholder, Is.True, "Previous address field does not have 'Address Details' placeholder");
                Assert.That(unitFieldExists, Is.True, "Unit field is not displayed next to the previous address field");
            });

            await _logger.ExecuteStepAsync("Type in previous address without selecting a suggestion and click Enter manually link, then verify expanded address fields are shown", async () =>
            {
                await previousAddressLocator.PressSequentiallyAsync("123 Main", new() { Delay = 50 });
                await _pageHelper.InteractWithField(EnterAddressManuallyLink);
                Assert.Multiple(async () =>
                {
                    Assert.That(await _pageHelper!.ElementExists(PreviousAddressLine1), Is.True, "Address Line 1 field is not displayed");
                    Assert.That(await _pageHelper.ElementExists(PreviousAddressCity), Is.True, "City field is not displayed");
                    Assert.That(await _pageHelper.ElementExists(PreviousAddressState), Is.True, "State field is not displayed");
                    Assert.That(await _pageHelper.ElementExists(PreviousAddressZip), Is.True, "ZipCode field is not displayed");
                    Assert.That(await _pageHelper.ElementExists(PreviousAddressUnit), Is.True, "Unit field is not displayed");
                });
            });

            await _logger.ExecuteStepAsync("Click Continue with empty expanded fields and verify validation messages", async () =>
            {
                await discountsPage.ClickContinue();
                var message = await _pageHelper!.GetFieldValidationMessageAsync("PreviousAddress.AddressLine1");
                Assert.That(message, Is.Not.Null.And.Not.Empty, "No validation messages are presented when continuing with empty expanded address fields");
            });
        }

        //[Test]
        //[RunIn(includeProduction: true)]
        //[Tenant(Tenant.PROGRESSIVEPL)]
        //[Author(Author.Helen)]
        //[TestCaseId(237232)]
        //[Category("Regression")]
        //[Category("HQX2")]
        //[Category("Condo")]
        //[Description("Validates mailing address compound field on Owner page: appears when checkbox is ticked, shows Google suggestions, and expands to manual entry fields")]
        public async Task PGR_HQX2_Compound_Address_Mailing_Address()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park));
            await _progressiveHelper.CallQuoteStartAsync(request);

            var mailingAddressLocator = _currentPage!.Locator(PersonalInfoFields.Fields[MailingAddress].Locators.First());

            var ownerPage = await _logger.ExecuteStepAsync("Navigate to Owner page and verify mailing address field appears when checkbox is ticked", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_OwnerPage>(FlowType.HQXShortFlow, null, true);
                var page = PageFactory.CreatePage<HQXConsumer_OwnerPage>();
                await _pageHelper!.InteractWithField(MailingAddressDifferent, "true");
                bool mailingAddressExists = await _pageHelper.ElementExists(MailingAddress);
                Assert.That(mailingAddressExists, Is.True, "Mailing address field is not displayed after checking the mailing address checkbox");
                return page;
            });

            await _logger.ExecuteStepAsync("Clear mailing address and click Continue to verify validation message", async () =>
            {
                await _pageHelper!.InteractWithElement(LocatorType.CSS, PersonalInfoFields.Fields[MailingAddress].Locators.First(), ElementAction.Clear);
                await ownerPage.ClickContinue();
                var message = await _pageHelper.GetFieldValidationMessageAsync("MailingAddress.AddressLine1");
                Assert.That(message, Is.Not.Null.And.Not.Empty, "No validation message is presented for the empty mailing address field");
                await mailingAddressLocator.ScrollIntoViewIfNeededAsync();
            });

            await _logger.ExecuteStepAsync("Type mailing address and verify Google suggest returns valid values and Enter manually link appears", async () =>
            {
                await mailingAddressLocator.PressSequentiallyAsync("123 Main", new() { Delay = 50 });
                var suggestionsVisible = await _pageHelper!.WaitForElementAsync(
                    _currentPage!.Locator(".pac-item").First,
                    timeout: 5000,
                    waitForVisibility: true) != null;
                Assert.That(suggestionsVisible, Is.True, "Google suggest did not return any values after typing in the mailing address field");
                bool enterManuallyLinkExists = await _pageHelper.ElementExists(EnterAddressManuallyLink, timeout: 3000);
                Assert.That(enterManuallyLinkExists, Is.True, "\"Can't find your address? Enter it manually.\" hyperlink is not displayed below the mailing address field");
            });

            await _logger.ExecuteStepAsync("Click Enter manually link and verify expanded address fields are shown", async () =>
            {
                await _pageHelper!.InteractWithField(EnterAddressManuallyLink);
                var line1Exists = await _pageHelper.ElementExists(MailingAddressLine1);
                var cityExists = await _pageHelper.ElementExists(MailingAddressCity);
                var stateExists = await _pageHelper.ElementExists(MailingAddressState);
                var zipExists = await _pageHelper.ElementExists(MailingAddressZip);
                var unitExists = await _pageHelper.ElementExists(MailingAddressUnit);
                Assert.Multiple(() =>
                {
                    Assert.That(line1Exists, Is.True, "Mailing Address Line 1 field is not displayed");
                    Assert.That(cityExists, Is.True, "Mailing Address City field is not displayed");
                    Assert.That(stateExists, Is.True, "Mailing Address State field is not displayed");
                    Assert.That(zipExists, Is.True, "Mailing Address ZipCode field is not displayed");
                    Assert.That(unitExists, Is.True, "Mailing Address Unit field is not displayed");
                });
            });

            await _logger.ExecuteStepAsync("Click Continue with empty expanded fields and verify validation messages", async () =>
            {
                await ownerPage.ClickContinue();
                var messages = await _pageHelper!.GetValidationMessagesAsync();
                Assert.That(messages, Is.Not.Empty, "No validation messages are presented when continuing with empty expanded mailing address fields");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(248329)]
        [Category("HQX2")]
        [Category("Sanity")]
        [Author(Author.Helen)]
        [Description("FL HO3 consumer quote (home 20 years old): the 'Has the heating, plumbing or electrical been replaced?' parent question appears on Discounts. Selecting Yes reveals the plumbing/heating/electrical child questions; No hides them. The flow then continues through to Rates.")]
        public async Task PGR_HQX2_Utilities_Replaced_Question_Reveals_Children_Then_Reaches_Rates()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            // Create an HO3 (Home) consumer quote in FL whose home was built 20 years ago, which is
            // what gates the utilities-replaced question (FL + HO3 + yearBuilt 20-49 years old).
            var request = QuoteStartPrefillDataProvider.GetHomeBuiltAgePrefillData(
                Addresses.GetAddress(AddressKey.FL_Bradenton), ageInYears: 20);
            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            // Child question labels — asserted against the visible question set. The child wrappers
            // stay attached in the DOM (aria-hidden/inert) when the parent is No, so a presence check
            // can't tell revealed from hidden; GetQuestionLabelsAsync only returns visible questions.
            const string plumbingLabel = "Has the plumbing system been replaced?";
            const string heatingLabel = "Has the heating system been replaced?";
            const string electricalLabel = "Has the electrical system been replaced?";

            HQXConsumer_DiscountsPage discountsPage = null!;

            await _logger.ExecuteStepAsync("Navigate to Discounts page and assert the utilities-replaced question is presented", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_DiscountsPage>(FlowType.HQXShortFlow, overviewPage, null, true);
                discountsPage = PageFactory.CreatePage<HQXConsumer_DiscountsPage>();

                var parentPresented = await _pageHelper!.ElementExists(UtilitiesUpdated);
                Assert.That(parentPresented, Is.True,
                    "The 'Has the heating, plumbing or electrical been replaced?' parent question should be presented on the Discounts page");
            });

            await _logger.ExecuteStepAsync("Select Yes for the parent question and assert child questions are revealed", async () =>
            {
                await _pageHelper!.InteractWithField(UtilitiesUpdated, "true");

                var visibleLabels = await discountsPage.GetQuestionLabelsAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(visibleLabels, Does.Contain(plumbingLabel), "Plumbing update child question should be presented when the parent is Yes");
                    Assert.That(visibleLabels, Does.Contain(heatingLabel), "Heating update child question should be presented when the parent is Yes");
                    Assert.That(visibleLabels, Does.Contain(electricalLabel), "Electrical update child question should be presented when the parent is Yes");
                });
            });

            await _logger.ExecuteStepAsync("Change the parent question to No and assert child questions are hidden", async () =>
            {
                await _pageHelper!.InteractWithField(UtilitiesUpdated, "false");

                var visibleLabels = await discountsPage.GetQuestionLabelsAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(visibleLabels, Does.Not.Contain(plumbingLabel), "Plumbing update child question should be hidden when the parent is No");
                    Assert.That(visibleLabels, Does.Not.Contain(heatingLabel), "Heating update child question should be hidden when the parent is No");
                    Assert.That(visibleLabels, Does.Not.Contain(electricalLabel), "Electrical update child question should be hidden when the parent is No");
                });
            });

            await _logger.ExecuteStepAsync("Continue through to the Rates page", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_RatesPage>(FlowType.HQXShortFlow, discountsPage, null, true);
                var ratesPage = PageFactory.CreatePage<HQXConsumer_RatesPage>();
                Assert.That(ratesPage, Is.Not.Null, "Rates page should be presented after submitting the quote");
            });
        }
    }
}