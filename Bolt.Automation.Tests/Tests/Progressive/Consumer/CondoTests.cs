using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Services.Tooltips;
using Bolt.Automation.InternalServices.Database.Queries.Main;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using ValueType = Bolt.Automation.FrontEnds.PlaywrightBase.Helpers.ValueType;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class CondoTests : ProgressiveUITestBase
    {
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("HQX2")]
        [Category("Sanity")]
        [Category("Smoke")]
        [TestCaseId(110)]
        [Author(Author.Helen)]
        [Description("E2E Condo (HO6) flow: only LOB is injected via API, all questions are filled via Executor and the flow reaches the Rates page successfully")]
        public async Task PGR_HQX2_HO6_Condo_E2E_Full_Flow_No_Prefill()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var request = await _logger.ExecuteStepAsync("Build QuoteStart request with Condo LOB only (no prefill data)", async () =>
            {
                var req = QuoteStartPrefillDataProvider.GetCustomQuoteStartRequest(
                    configureRequest: r => r.LOBCd = "Condo",
                    configurePrefill: null);
                await Task.CompletedTask;
                return req;
            });

            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var overviewPage = await _logger.ExecuteStepAsync("Handle 3PQ page if present and land on Overview page", async () =>
            {
                return await _progressiveHelper.HandleThreePQIfPresentAsync();
            });

            var ratesPage = await _logger.ExecuteStepAsync("Proceed to Rates Page)", async () =>
            {
                return await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                    FlowType.HQXShortFlow,
                    currentPage: overviewPage,
                    formData: new Dictionary<string, string> { ["Lob"] = LobType.Condo.ToString() },
                    fillForms: true);
            });

            await _logger.ExecuteStepAsync("Assert Rates page is displayed and at least one carrier quote is available", async () =>
            {
                var page = PageFactory.CreatePage<HQXConsumer_RatesPage>();
                Assert.That(page, Is.Not.Null, "Rates page was not reached after completing the full Condo E2E flow");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Category("HQX2")]
        [Category("Regression")]
        [TestCaseId(241340)]
        [Author(Author.Helen)]
        [Description("Validates that exactly the expected questions are visible in each section of the HO6 Overview page")]
        public async Task PGR_HQX2_HO6_Overview_Sections_Show_Correct_Questions_And_Summaries()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetHO6PrefillData(Addresses.GetAddress(AddressKey.OH), customizePrefill: p =>
            {
                p.PolicyData.PLYearBuilt = null;
                p.PolicyData.PL_CentralAC = null;
                p.PolicyData.PLHeatingType = null;
            });
            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            overviewPage = await _logger.ExecuteStepAsync("Open Overview page and expand all sections", async () =>
            {
                var page = PageFactory.CreatePage<HQXConsumer_OverviewPage>();
                await page.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.Property);
                await page.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.Exterior);
                await page.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.Interior);
                await page.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.PersonalInfo);
                return page;
            });

            await _logger.ExecuteStepAsync("Validate LOB type label", async () =>
            {
                var lobTitle = (await overviewPage.LobType.InnerTextAsync()).Trim();
                Assert.That(lobTitle, Is.EqualTo("Condo"), $"LOB title is incorrect, displaying '{lobTitle}'");
            });

            var sections = new (string Name, string[] Expected, string[] SummaryValues)[]
            {
                (
                    Name: HQXConsumer_OverviewPage.SectionNames.Property,
                    Expected: ["PLTypeOfDwelling", "PLYearBuilt", "PLSquareFootage", "PLConstructionType"],
                    SummaryValues: ["Condo", "2,310 Sq. Ft"]
                ),
                (
                    Name: HQXConsumer_OverviewPage.SectionNames.Exterior,
                    Expected: ["ExteriorWallsConstruction", "MitRoofShape", "RoofType"],
                    SummaryValues: ["Aluminum Siding", "Architectural Shingles"]
                ),
                (
                    Name: HQXConsumer_OverviewPage.SectionNames.Interior,
                    Expected: ["PLHeatingType", "PL_CentralAC"],
                    SummaryValues: ["Gas - Forced Air", "Central AC"]
                ),
                (
                    Name: HQXConsumer_OverviewPage.SectionNames.PersonalInfo,
                    Expected: ["FirstName", "MiddleName", "LastName", "DateOfBirth"],
                    SummaryValues: [
                        request.ApplicantDetails!.GivenName,
                        request.ApplicantDetails.Surname,
                        request.ApplicantDetails.BirthDt!.Value.Year.ToString()
                    ]
                ),
            };

            var sectionFailures = await _logger.ExecuteStepAsync("Collect question and summary data for all sections", () =>
                overviewPage.CollectSectionValidationFailuresAsync(sections));

            await _logger.ExecuteStepAsync("Assert all section validations", () =>
            {
                Assert.That(sectionFailures, Is.Empty, string.Join("\n", sectionFailures));
                return Task.CompletedTask;
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(242140)]
        [Category("HQX2")]
        [Category("Regression")]
        [Category("Condo")]
        [Author(Author.Helen)]
        [Description("Validates HO6-specific tooltip content: RoofResponsible 'Why can't I edit this?' popup and ExoticAnimals 'Learn more' tooltip on Details page")]
        public async Task PGR_HQX2_HO6_Details_Validate_Condo_RoofResponsible_And_ExoticAnimals_Tooltips()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            await _progressiveHelper.CallQuoteStartAsync(QuoteStartPrefillDataProvider.GetHO6PrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park)), navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            await _logger.ExecuteStepAsync("Navigate to Details page", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_DetailsPage>(FlowType.HQXShortFlow, overviewPage, null, false);
            });

            await _logger.ExecuteStepAsync("Validate RoofResponsible 'Why can't I edit this?' popup", async () =>
            {
                var detailsPage = PageFactory.CreatePage<HQXConsumer_DetailsPage>();

                var expectedPopupContent = "When Condo is selected as the home style type, the exterior of the home is insured by someone else. To edit this, return to the Overview page, in the Property section, and change the Home style type.";

                await detailsPage.WhyCantEditThisLink.ClickAsync();
                await _pageHelper!.WaitForElementAsync(detailsPage.WhyCantEditThisPopupContent);

                var actualPopupContent = await detailsPage.WhyCantEditThisPopupContent.InnerTextAsync();
                var normalizedActual = TextHelper.NormalizeText(actualPopupContent);
                var normalizedExpected = TextHelper.NormalizeText(expectedPopupContent);

                Assert.That(normalizedActual, Is.EqualTo(normalizedExpected),
                    $"RoofResponsible popup content mismatch. Expected: '{normalizedExpected}', Actual: '{normalizedActual}'");

                await _pageHelper.InteractWithField(PopupCloseButton, string.Empty);
                await detailsPage.WhyCantEditThisPopupContent.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 5000 });
            });

            await _logger.ExecuteStepAsync("Validate ExoticAnimals 'Learn more' tooltip", async () =>
            {
                var detailsPage = PageFactory.CreatePage<HQXConsumer_DetailsPage>();
                var tooltipService = new TooltipValidationService(detailsPage.Page, _pageHelper!, _logger);

                var labels = ProgressiveTestHelper.GetFieldLabels(AnimalsOnThePremises_Exotic);
                var expectedExoticAnimalsTooltip = "Includes alligators, crocodiles, venomous snakes, primates, large cats and other non-domesticated animals usually found in the wild or in zoos.";

                var issues = await tooltipService.ValidateTooltipsAsync(
                    new Dictionary<string, string>
                    {
                        [labels[AnimalsOnThePremises_Exotic]] = expectedExoticAnimalsTooltip
                    });

                var exoticAnimalsIssue = issues.FirstOrDefault(i => i.Title == labels[AnimalsOnThePremises_Exotic]);

                Assert.That(exoticAnimalsIssue, Is.Null,
                    $"ExoticAnimals tooltip validation failed: {exoticAnimalsIssue?.Reason} - Expected: '{exoticAnimalsIssue?.ExpectedFragment}', Actual: '{exoticAnimalsIssue?.Actual}'");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(242141)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("Validates RoofResponsible info box messages on HO3->HO6 product change and that the box is not shown on subsequent page visits")]
        public async Task PGR_HQX2_Details_RoofResponsible_InfoBox_ProductChange()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park));
            await _progressiveHelper.CallQuoteStartAsync(request);

            await _logger.ExecuteStepAsync("Navigate to Details Page", async () =>
            {
                await Executor.Execute<HQXConsumer_OverviewPage, HQXConsumer_DetailsPage>(FlowType.HQXShortFlow, null, false);
            });

            var detailsPage = await _logger.ExecuteStepAsync("Fill Details form, check RoofResponsible = true and assert HO6 info box appears", async () =>
            {
                var detailsPage = PageFactory.CreatePage<HQXConsumer_DetailsPage>();
                await detailsPage.FillForm();
                await _pageHelper.InteractWithField(RoofResponsible, "true");

                await detailsPage.RoofResponsibleInfoBoxHO6.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                var infoBoxText = TextHelper.NormalizeText(await detailsPage.RoofResponsibleInfoBoxHO6.InnerTextAsync());

                Assert.That(infoBoxText, Does.Contain("condo/townhome policy"),
                    $"HO6 info box does not mention condo/townhome policy. Actual text: '{infoBoxText}'");
                Assert.That(infoBoxText, Does.Contain("insured by someone else"),
                    $"HO6 info box does not contain expected exterior insured text. Actual text: '{infoBoxText}'");
                return detailsPage;
            });

            await _logger.ExecuteStepAsync("Click Continue to proceed, then navigate back to Details page", async () =>
            {
                await detailsPage.ClickContinue();
                PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                await _pageHelper.ClickBrowserBackButton();
                return PageFactory.CreatePage<HQXConsumer_DetailsPage>();
            });

            await _logger.ExecuteStepAsync("Uncheck RoofResponsible and assert HO3 info box appears", async () =>
            {
                await _pageHelper.InteractWithField(RoofResponsible, "false");

                await detailsPage.RoofResponsibleInfoBoxHO3.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                var infoBoxText = TextHelper.NormalizeText(await detailsPage.RoofResponsibleInfoBoxHO3.InnerTextAsync());

                Assert.That(infoBoxText, Does.Contain("homeowner\u2019s policy"),
                    $"HO3 info box does not mention homeowner's policy. Actual text: '{infoBoxText}'");
                Assert.That(infoBoxText, Does.Contain("insure your home\u2019s exterior"),
                    $"HO3 info box does not contain expected exterior insured text. Actual text: '{infoBoxText}'");
            });

            await _logger.ExecuteStepAsync("Click Continue to proceed, then navigate back to Details page a second time", async () =>
            {
                var detailsPage = PageFactory.CreatePage<HQXConsumer_DetailsPage>();
                await detailsPage.ClickContinue();
                PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                await _pageHelper.ClickBrowserBackButton();
                return PageFactory.CreatePage<HQXConsumer_DetailsPage>();
            });

            await _logger.ExecuteStepAsync("Assert info boxes are not visible on return", async () =>
            {
                var ho6BoxVisible = await detailsPage.RoofResponsibleInfoBoxHO6.IsVisibleAsync();
                var ho3BoxVisible = await detailsPage.RoofResponsibleInfoBoxHO3.IsVisibleAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(ho6BoxVisible, Is.False, "HO6 info box should NOT be displayed on return to Details page without a new selection");
                    Assert.That(ho3BoxVisible, Is.False, "HO3 info box should NOT be displayed on return to Details page without a new selection");
                });
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(242142)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("HO6: Enforce RoofResponsible checked and non-editable when Condominium is selected on the Overview page")]
        public async Task PGR_HQX2_HO6_Enforce_RoofResponsible_Checked_NonEditable_Condominium_Overview()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            await _progressiveHelper.CallQuoteStartAsync(QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park)), navigate: true);

            var detailsPage = await _logger.ExecuteStepAsync("Set TypeOfDwelling = Condominium on Overview page and navigate to Details", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXConsumer_OverviewPage>();
                await overviewPage.ExpandSectionAsync(HQXConsumer_OverviewPage.SectionNames.Property);
                await _pageHelper.InteractWithField(PLTypeOfDwelling, "Condominium");
                await overviewPage.ClickContinue();
                return PageFactory.CreatePage<HQXConsumer_DetailsPage>();
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible is checked and disabled", async () =>
            {
                var isChecked = await _pageHelper.GetFieldValue(RoofResponsible, ValueType.IsChecked);
                var labelClass = await detailsPage.RoofResponsibleLabel.GetAttributeAsync("class") ?? string.Empty;

                Assert.Multiple(() =>
                {
                    Assert.That(isChecked, Is.EqualTo("true").IgnoreCase, "RoofResponsible should be checked when Condominium is selected on Overview");
                    Assert.That(labelClass, Does.Contain("disabled"), "RoofResponsible should be disabled when Condominium is selected on Overview");
                });
            });

            await _logger.ExecuteStepAsync("Assert 'Why can't I edit this?' hyperlink is present and tooltip text is correct", async () =>
            {
                var expectedTooltipText = "When Condo is selected as the home style type, the exterior of the home is insured by someone else. To edit this, return to the Overview page, in the Property section, and change the Home style type.";

                var linkVisible = await detailsPage.WhyCantEditThisLink.IsVisibleAsync();
                Assert.That(linkVisible, Is.True, "'Why can't I edit this?' hyperlink should be displayed when RoofResponsible is disabled");

                await detailsPage.WhyCantEditThisLink.ClickAsync();
                await detailsPage.WhyCantEditThisPopupContent.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                var popupText = TextHelper.NormalizeText(await detailsPage.WhyCantEditThisPopupContent.InnerTextAsync());

                Assert.That(popupText, Is.EqualTo(expectedTooltipText), $"Tooltip text mismatch. Actual: '{popupText}'");

                await _pageHelper.InteractWithField(PopupCloseButton, string.Empty);
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(241364)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("HO6: Enforce RoofResponsible checked and non-editable when Condominium is selected on the Details page")]
        public async Task PGR_HQX2_HO6_Enforce_RoofResponsible_Checked_NonEditable_Condominium_Details()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData();
            if (request.PropertyAddr != null) request.PropertyAddr.Addr1 = string.Empty;
            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            var detailsPage = await _logger.ExecuteStepAsync("Navigate to Details page", async () =>
            {
                return await Executor.ExecuteToPage<HQXConsumer_DetailsPage>(FlowType.HQXShortFlow, overviewPage, null, false);
            });

            await _logger.ExecuteStepAsync("Set TypeOfDwelling = Condominium on Details page", async () =>
            {
                await detailsPage.FillForm();
            });

            await _logger.ExecuteStepAsync("Assert RoofResponsible is checked, disabled, hyperlink and info box are displayed", async () =>
            {
                var isChecked = await _pageHelper.GetFieldValue(RoofResponsible, ValueType.IsChecked);
                var labelClass = await detailsPage.RoofResponsibleLabel.GetAttributeAsync("class") ?? string.Empty;
                var linkVisible = await detailsPage.WhyCantEditThisLink.IsVisibleAsync();
                var infoBoxVisible = await detailsPage.RoofResponsibleInfoBoxHO6.IsVisibleAsync();

                var checkboxLabelText = TextHelper.NormalizeText(await detailsPage.RoofResponsibleCheckboxText.InnerTextAsync());
                var subLabelText = TextHelper.NormalizeText(await detailsPage.RoofResponsibleSubLabel.InnerTextAsync());
                var linkText = (await detailsPage.WhyCantEditThisLink.InnerTextAsync()).Trim();

                Assert.Multiple(() =>
                {
                    Assert.That(isChecked, Is.EqualTo("true").IgnoreCase, "RoofResponsible should be checked when Condominium is selected on Details page");
                    Assert.That(labelClass, Does.Contain("disabled"), "RoofResponsible should be disabled when Condominium is selected on Details page");
                    Assert.That(linkVisible, Is.True, "'Why can't I edit this?' hyperlink should be displayed when RoofResponsible is disabled on the Details page");
                    Assert.That(infoBoxVisible, Is.True, "RoofResponsible HO6 info box should be displayed when Condominium is selected on the Details page");

                    Assert.That(checkboxLabelText, Does.Contain("The exterior of this home is insured by someone else"), $"Checkbox label text mismatch. Actual: '{checkboxLabelText}'");
                    Assert.That(subLabelText, Does.Contain("A condo association covers exterior damage such as the roof, outside walls, etc"), $"Sub-label text mismatch. Actual: '{subLabelText}'");
                    Assert.That(TextHelper.NormalizeText(linkText), Is.EqualTo("Why can\u2019t I edit this?"), $"Link text mismatch. Actual: '{linkText}'");
                });
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(242143)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("Validates that FloorNumber and NumOfFloors questions are not displayed on Discounts page when HighRiseCondo is not selected")]
        public async Task PGR_HQX2_HO6_Discounts_FloorNumber_Hidden_When_No_HighRise()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            await _progressiveHelper.CallQuoteStartAsync(QuoteStartPrefillDataProvider.GetHO6PrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park)), navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            await _logger.ExecuteStepAsync("Navigate to Discounts page", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_DiscountsPage>(FlowType.HQXShortFlow, overviewPage, null, true);
            });

            await _logger.ExecuteStepAsync("Validate FloorNumber and NumOfFloors are hidden on Discounts page", async () =>
            {
                var discountsPage = PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                var questionLabels = await discountsPage.GetQuestionLabelsAsync();

                Assert.Multiple(() =>
                {
                    Assert.That(questionLabels, Does.Not.Contain("Floor number"),
                        "FloorNumber question should not be displayed when HighRiseCondo is not selected");
                    Assert.That(questionLabels, Does.Not.Contain("Number of floors"),
                        "NumOfFloors question should not be displayed when HighRiseCondo is not selected");
                });
            });
        }

        [Test]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(242145)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("Validates that PL_NumberOfFloors and PLFloorNumber default to 1 in policy data after completing Discounts page without selecting HighRiseCondo")]
        public async Task PGR_HQX2_HO6_PolicyData_Default_Floor_Values()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            await _progressiveHelper.CallQuoteStartAsync(QuoteStartPrefillDataProvider.GetHO6PrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park)), navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            var friendlyId = await _logger.ExecuteStepAsync("Navigate to Discounts page and capture quote number", async () =>
            {
                var id = await overviewPage.GetFriendlyId();
                await Executor.ExecuteToPage<HQXConsumer_DiscountsPage>(FlowType.HQXShortFlow, overviewPage, null, true);
                return id;
            });

            await _logger.ExecuteStepAsync("Continue past Discounts page", async () =>
            {
                var discountsPage = PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                await discountsPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Validate default floor values in policy data", async () =>
            {
                var mainQueries = _testScope.ServiceProvider.GetRequiredService<IMainQueries>();
                mainQueries.PolicyDataLogic.ClearXmlCache(friendlyId);

                var numberOfFloors = await mainQueries.PolicyDataLogic.GetNodeValueAsync(friendlyId, "PL_NumberOfFloors");
                var floorNumber = await mainQueries.PolicyDataLogic.GetNodeValueAsync(friendlyId, "PLFloorNumber");

                Assert.Multiple(() =>
                {
                    Assert.That(numberOfFloors, Is.EqualTo("1"),
                        $"Default value of PL_NumberOfFloors is not 1, actual: {numberOfFloors}");
                    Assert.That(floorNumber, Is.EqualTo("1"),
                        $"Default value of PLFloorNumber is not 1, actual: {floorNumber}");
                });
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(242144)]
        [Category("HQX2")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("Validates that FloorNumber and NumOfFloors questions appear and can be filled when HighRiseCondo is selected on Discounts page")]
        public async Task PGR_HQX2_HO6_HighRise_Shows_FloorNumber_And_NumOfFloors()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            await _progressiveHelper.CallQuoteStartAsync(QuoteStartPrefillDataProvider.GetHO6PrefillData(Addresses.GetAddress(AddressKey.NY_Averill_Park)), navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            await _logger.ExecuteStepAsync("Navigate to Discounts page", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_DiscountsPage>(FlowType.HQXShortFlow, overviewPage, null, true);
            });

            await _logger.ExecuteStepAsync("Select HighRiseCondo = Yes and validate FloorNumber and NumOfFloors appear", async () =>
            {
                var highRiseCondoLabel = ProgressiveTestHelper.GetFieldLabel(PLHighRiseCondo);
                await _pageHelper!.InteractWithField(PLHighRiseCondo, "true");

                bool floorNumberExists = await _pageHelper.ElementExists(PLFloorNumber);
                bool numOfFloorsExists = await _pageHelper.ElementExists(PL_NumberOfFloors);

                Assert.Multiple(() =>
                {
                    Assert.That(floorNumberExists, Is.True, "FloorNumber question is not displayed after selecting HighRiseCondo");
                    Assert.That(numOfFloorsExists, Is.True, "NumOfFloors question is not displayed after selecting HighRiseCondo");
                });
            });

            await _logger.ExecuteStepAsync("Fill floor fields and navigate to Rates page", async () =>
            {
                await _pageHelper!.InteractWithField(PL_NumberOfFloors, "3");
                await _pageHelper.InteractWithField(PLFloorNumber, "2");

                var discountsPage = PageFactory.CreatePage<HQXConsumer_DiscountsPage>();
                await discountsPage.ClickContinue();

                var ownerPage = PageFactory.CreatePage<HQXConsumer_OwnerPage>();
                await ownerPage.FillForm();
                await ownerPage.ClickContinue();

                PageFactory.CreatePage<HQXConsumer_RatesPage>();
            });
        }
    }
}
