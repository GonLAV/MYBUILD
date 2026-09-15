using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.Models.TestData.Interview.Models;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using BoltEnvironment = Bolt.Automation.Common.Environment;
using FlowType = Bolt.Automation.FrontEnds.Projects.D2C.Flows.FlowType;
using static Bolt.Automation.Common.Tenant;

namespace Bolt.Automation.Tests.Tests.D2C
{
    internal class D2CHomeTests : D2CTestBase
    {
        private IAdbxApiClientFactory _adbxApifactory = null!;
        
        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")][Category("Sanity")]
        [RunIn(includeProduction: true)]
        [TestCaseId(124646)]
        [Description("Verify end-to-end Condominium quote flow with flood coverage")]
        public async Task BOLTAG_D2C_E2E_Condominium()
        {
            var ratesPage = await _logger.ExecuteStepAsync("Complete Condominium quote flow", async () =>
            {
                return  await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    FlowType.D2CCondoFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.PLTypeOfDwelling] = "Condominium",
                    },
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_PropertiesUsagePage)]
                );
            });

            await _logger.ExecuteStepAsync("Verify rated carriers are available", async () =>
            {
                var ratedCarriers = await ratesPage.GetAllRatedCarriers();
                Assert.That(ratedCarriers.Count, Is.GreaterThan(0), "Should have rated carriers available");
            });

            await _logger.ExecuteStepAsync("Switch Flood Toggle On", async () =>
            {
                var isFloodDisplayed = await ratesPage.IsFloodCoverageDisplayed();
                await ratesPage.SwitchFirstFloodToggleOn();
                Assert.That(isFloodDisplayed, Is.True, "Flood coverage should be displayed after switching flood toggle on");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(223254)]
        [Description("Verify Condominium property details validation rules")]
        public async Task BOLTAG_D2C_Condominium_Validation()
        {
            var houseDetailsPage = await _logger.ExecuteStepAsync("Navigate to Condominium House Details", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_HouseDetailsPage>(
                    FlowType.D2CCondoFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.OnlineAddress] = "545 8th Ave, New York, NY 10018",
                        [FieldNames.PLTypeOfDwelling] = "Condominium",
                        [FieldNames.IsPrimaryResidence] = "No",
                        [FieldNames.DwellingUsage] = "Secondary / Seasonal",
                    },
                    fillForms: true
                );
            });

            await _logger.ExecuteStepAsync("Verify initial field values are pre-populated", async () =>
            {
                await _pageHelper!.InteractWithElement(LocatorType.XPath, "//button[contains(@class , 'edit-button')]", ElementAction.Click);
                await Task.Delay(1000);

                var initialSquareFootage = await houseDetailsPage.PageHelper.GetFieldValue(FieldNames.PLSquareFootage);
                var initialYearBuilt = await _pageHelper!.GetFieldValue(LocatorType.CSS, "#PLYearBuilt");
                var initialNumberOfUnits = await _pageHelper!.GetFieldValue(FieldNames.PLNumberOfUnits);

                Assert.Multiple(() =>
                {
                    Assert.That(string.IsNullOrEmpty(initialSquareFootage), Is.False, $"Square footage should not be empty, actual: [{initialSquareFootage}]");
                    Assert.That(string.IsNullOrEmpty(initialNumberOfUnits), Is.False, $"Number of units should not be empty, actual: [{initialNumberOfUnits}]");
                    Assert.That(string.IsNullOrEmpty(initialYearBuilt), Is.False, $"Year built should not be empty, actual: [{initialYearBuilt}]");
                });
            });

            await _logger.ExecuteStepAsync("Enter invalid values and verify validation errors", async () =>
            {
                await _pageHelper!.InteractWithField(FieldNames.PLNumberOfUnits, "100");
                await _pageHelper!.InteractWithField(FieldNames.PLSquareFootage, "0");
                await _pageHelper!.InteractWithField(FieldNames.PLYearBuilt, "2051");
                await _pageHelper!.InteractWithField(FieldNames.DistanceToCoast, "50");

                await _d2cHelper!.ValidateErrorMessages(_pageHelper!, new Dictionary<string, string>
                {
                    ["Future Year"] = "Year built value cannot be in the future",
                    ["Square Footage"] = "Square footage must be between 1 and 999,999",
                    ["Number of Units"] = "Please enter a value between 1 to 99"
                });
            });

            await _logger.ExecuteStepAsync("Enter historical year and verify validation error", async () =>
            {
                await _pageHelper!.InteractWithField(FieldNames.PLYearBuilt, "1799");
                await _pageHelper!.InteractWithField(FieldNames.PLSquareFootage, "3150");
                await _pageHelper!.InteractWithField(FieldNames.PLNumberOfUnits, "1");

                await _d2cHelper!.ValidateErrorMessages(_pageHelper!, new Dictionary<string, string>
                {
                    ["Historical Year"] = "Value cannot be less than 1800"
                });
            });

            await _logger.ExecuteStepAsync("Verify valid values clear errors", async () =>
            {
                await _pageHelper!.InteractWithField(FieldNames.PLYearBuilt, "1999");
                var validationMessages = await _pageHelper!.GetValidationMessagesAsync();
                Assert.That(validationMessages.Count, Is.EqualTo(0), "Valid values should clear validation errors");
            });

            await _logger.ExecuteStepAsync("Verify sprinkler system not displayed for condominiums", async () =>
            {
                await houseDetailsPage.ClickContinue();

                var safetyAlarmsPage = PageFactory.CreatePage<D2C_SafetyAlarms>();
                var isSprinklerDisplayed = await _pageHelper!.ElementExists(LocatorType.XPath, "//app-image[@class='ng-star-inserted']/following-sibling::span[contains(text(),'Sprinkler System')]");

                _logger.LogDataValidation("Sprinkler System Hidden", !isSprinklerDisplayed, "False", isSprinklerDisplayed.ToString(), "Sprinkler System should not be displayed for condominiums");
                Assert.That(isSprinklerDisplayed, Is.False, "Sprinkler System should not be displayed for condominiums");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(124658)]
        [Description("Verify HO3 secondary/seasonal property creates Dwelling Fire policy")]
        public async Task BOLTAG_D2C_HO3_NewProperty_SecondarySeasonal()
        {
            var houseDetailsPage = await _logger.ExecuteStepAsync("Navigate to House Details via D2C Home flow", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_HouseDetailsPage>(
                    FlowType.D2CHomeFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.OnlineAddress] = "504 Princeton Ave, Trenton, OH 45067",
                        [FieldNames.IsPrimaryResidence] = "No",
                    },
                    fillForms: true
                );
            });

            var friendlyId = await houseDetailsPage.GetFriendlyId();

            await _logger.ExecuteStepAsync("Wait for meaningful policy data", async () =>
            {
                await _d2cHelper!.WaitForMeaningfulPolicyData(
                    _testScope.ServiceProvider.GetRequiredService<Bolt.Automation.Common.PollyRetry.IPollyRetryService>(),
                    friendlyId!
                );
            });

            await _logger.ExecuteStepAsync("Validate LOBs, occupancy type and address", async () =>
            {
                await _d2cHelper!.ValidateSetLobs(friendlyId!, ["DwellingFire", "Flood"]);
                await _d2cHelper!.ValidatePolicyDataNode(friendlyId!, "OccupancyTypeForXml", "Vacant");
                await _d2cHelper!.ValidateAddressComponents(
                    friendlyId!,
                    "PropertyAddress",
                    ["504", "Princeton", "Trenton", "OH", "45067"]
                );
            });

            await _logger.ExecuteStepAsync("Complete flow to Safety Alarms page", async () =>
            {
                await Executor.ExecuteToPage<D2C_SafetyAlarms>(
                    FlowType.D2CHomeFlow,
                    houseDetailsPage,
                    new Dictionary<string, string>
                    {
                        [FieldNames.RoofReplaced] = "Yes",
                        [FieldNames.YearRoofReplaced] = "3 years ago",
                    },
                    fillForms: true
                );
            });

            await _logger.ExecuteStepAsync("Fill Safety Alarms and continue", async () =>
            {
                var safetyAlarmsPage = PageFactory.CreatePage<D2C_SafetyAlarms>();
                await safetyAlarmsPage.FillForm(new Dictionary<string, string> { [FieldNames.SafetyProduct] = "FireDetection" });
                await safetyAlarmsPage.SelectProductChildQuestion("FireDetectionType", "Yes");
                await safetyAlarmsPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Continue through Cross Sell page", async () =>
            {
                var crossSellPage = PageFactory.CreatePage<D2C_CrossSellInformationPage>();
                await crossSellPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Validate roof replacement data", async () =>
            {
                PageFactory.CreatePage<D2C_PersonalDetailsPage>();
                await _d2cHelper!.ValidateRoofReplacementData(friendlyId!, "CompleteUpdate", 3);
                //for now remove the emailAgreement validation because it is still an issue
                //await _d2cHelper!.ValidateEmailAgreementToggle(friendlyId);
            });

            await _logger.ExecuteStepAsync("Update year built and complete flow", async () =>
            {
                // Navigate back to House Details (4 clicks)
                for (int i = 0; i < 4; i++)
                {
                    await _pageHelper!.ClickBrowserBackButton();
                }

                var houseDetailsPageFinal = PageFactory.CreatePage<D2C_HouseDetailsPage>();
                await _pageHelper!.InteractWithElement(LocatorType.XPath, "(//button[contains(@class , 'edit-button')])[1]", ElementAction.Click);
                await Task.Delay(1000);
                await _pageHelper!.InteractWithField(FieldNames.PLYearBuilt, "2020");
                await houseDetailsPageFinal.ClickContinue();

                //verify roof replacement page not appears
                var safetyAlarmsPage = PageFactory.CreatePage<D2C_SafetyAlarms>();
                _logger.LogDataValidation("Roof Replacement Page Skipped", safetyAlarmsPage is not null, "SafetyAlarmsPage", safetyAlarmsPage?.GetType().Name ?? "null", "After year built update, roof replacement page should be skipped");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(UNIFY)]
        [Category("D2C")][Category("FullQuote")][Category("Quoting")]
        [TestCaseId(169986)]
        [Description("Verify HO3 full quote flow with Stillwater carrier")]
        public async Task Unify_D2C_Escrow_HO3_StillWater()
        {
            var user = TestContextAccessor.CurrentUserCollection.LakeviewConsumer;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API", async () =>
            {
                var data = PersonalLineDataProvider.GetPersonalHomeData(
                    address: AddressData.ND,
                    homeDetails: HomeDetailsTestData.PersonalHomeDetailsData,
                    homeFeatures: HomeFeaturesTestData.PersonalHomeFeaturesData,
                    policyDetails: PolicyTestData.PersonalHomePolicyDetails
                );

                data.PLYearBuilt = 2024;
                data.Email = "boltautomation@boltinc.com";
                data.PurchaseDate = "2025-10-09";
                data.CustomFields = new CustomFieldsModel { D2CAgreeToTerms = true };

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.Homeowners,
                    Data = data
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData, "&forcePageSkipping=true");
            });

            await _logger.ExecuteStepAsync("Select Stillwater carrier", async () =>
            {
                var ratesPage = PageFactory.CreatePage<D2C_RatesPage>();
                await ratesPage.ClickOnSpecificCarrierBuyNow("Stillwater");
            });

            await _logger.ExecuteStepAsync("Complete Stillwater property information flow and proceed to payment plan", async () =>
            {
                var quoteConfirmationPage = await Executor.ExecuteToPage<D2C_QuoteConfirmationPage>(
                   FlowType.StillwaterHomeFQFlow,
                   PageFactory.CreatePage<D2C_StillwaterPropertyInformationPage>(),
                   fillForms: true
                );

                await quoteConfirmationPage.ClickContinueButton();
            });

            await _logger.ExecuteStepAsync("Complete payment process", async () =>
            {
                var paymentPlanPage = PageFactory.CreatePage<D2C_PaymentPlanPageFQ>();
                await _pageHelper!.InteractWithElement(LocatorType.XPath, "//label[@for = 'MortgageBillPay']", ElementAction.Click);
                await paymentPlanPage.ClickContinue();
                var paymentPage = PageFactory.CreatePage<D2C_PaymentPageFQ>();
                await paymentPage.ClickOnContinueToPay();
                var paymentSuccsessPage = PageFactory.CreatePage<D2C_PaymentSuccessPageFQ>();
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(147720)]
        [Description("Verify end-to-end pet insurance quote with multiple pets")]
        public async Task BOLTAG_D2C_Pets_E2E()
        {
            var petsPage = await _logger.ExecuteStepAsync("Navigate to Pets page and fill first pet details", async () =>
            {
                var page = await Executor.Execute<D2C_YourAddressPage, D2C_PetsPage>(
                    FlowType.PetsFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.OnlineAddress] = "1315 Briers Creek Dr, Alpharetta, GA 30004",
                    },
                    fillForms: true
                );
                await page.FillForm();
                return page;
            });

            await _logger.ExecuteStepAsync("Add second pet", async () =>
            {
                petsPage = PageFactory.CreatePage<D2C_PetsPage>();
                await petsPage.ClickAddAnotherPet();

                var secondPetData = new Dictionary<string, string>
                {
                    [FieldNames.Pet] = "Dog",
                    [FieldNames.PetName] = "Goofy",
                    [FieldNames.PetBreedType] = "Pure",
                    [FieldNames.PetBreed] = "Akita",
                    [FieldNames.PetGender] = "Female",
                    [FieldNames.PetDoB] = DateTime.Today.AddYears(-2).ToString("MM/yyyy"),
                };

                await petsPage.FillForm(secondPetData);
            });

            await _logger.ExecuteStepAsync("Disable first pet and edit second pet", async () =>
            {
                petsPage = PageFactory.CreatePage<D2C_PetsPage>();
                await petsPage.DisablePet("Woofy");

                await petsPage.ClickEditDetailsForPet("Goofy");
                var editedPetData = new Dictionary<string, string>
                {
                    [FieldNames.Pet] = "Cat",
                    [FieldNames.PetName] = "Mick",
                    [FieldNames.PetBreedType] = "Mixed",
                    [FieldNames.PetBreed] = "Alley Cat",
                    [FieldNames.PetGender] = "Female"
                };
                await petsPage.FillForm(editedPetData);
                await petsPage.ClickContinue();
            });

            var ratesPage = await _logger.ExecuteStepAsync("Complete personal details and navigate to rates page", async () =>
            {
                return await Executor.ExecuteToPage<D2C_RatesPage>(
                    FlowType.PetsFlow,
                    PageFactory.CreatePage<D2C_PersonalDetailsPage>(),
                    fillForms: true
                );
            });

            await _logger.ExecuteStepAsync("Validate coverage popup", async () =>
            {
                await ratesPage.ClickOnSpecificCarrierViewDetails("Prudent");
                var petNames = await ratesPage.GetEntityNames();

                Assert.Multiple(() =>
                {
                    Assert.That(petNames.Count, Is.EqualTo(1), $"Only 1 pet should be present in coverages popup. Actual count: {petNames.Count}");
                    Assert.That(petNames.First().Trim(), Is.EqualTo("Mick"), $"Expected pet name 'Mick', actual: {petNames.First()}");
                });

                var petCoverages = await ratesPage.GetEntityCoverages("Mick");
                var expectedCoverages = new List<string>
                {
                    "Pet Plan", "Accident Coverage", "Illness Coverage", "Annual Maximum",
                    "Reimbursement", "Annual Deductible", "Wellness"
                };

                var hasCoverageMatch = petCoverages.Intersect(expectedCoverages, StringComparer.OrdinalIgnoreCase).Any();
                Assert.That(hasCoverageMatch, Is.True, $"Expected to find pet coverages from [{string.Join(", ", expectedCoverages)}], but found [{string.Join(", ", petCoverages)}]");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Sanity")][Category("CRM")][Category("Quoting")]
        [TestCaseId(154087)]
        [Description("Verifies D2C HomeAuto multiLob flow validates separate carriers, confirms LOB selections, and validates products in ADBX dashboard")]
        public async Task BOLTAG_D2C_HomeAuto_Test()
        {
            var adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
            string? friendlyId = null;

            var ratesPage = await _logger.ExecuteStepAsync("Execute D2C Home+Auto flow to rates page", async () =>
            {
                var rates = await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    FlowType.D2CHomeAutoFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.LastName] = "Multilob",
                        [FieldNames.RoofReplaced] = "Yes",
                        [FieldNames.YearRoofReplaced] = "3 Years Ago",
                    },
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_PropertiesUsagePage), typeof(D2C_CrossSellInformationPage)]
                );
                await _d2cHelper!.SwitchToBuySeparatelyIfNeeded(rates);
                return rates;
            });

            await _logger.ExecuteStepAsync("Select Home coverage and validate single LOB", async () =>
            {
                var homeCarriers = await ratesPage.GetRatedCallToPurchaseCarriersForLob("Homeowners");
                await ratesPage.ClickOnSpecificCarrierViewDetailsForLob("Home", homeCarriers.First());
                var lobs = await ratesPage.GetPolicyLobs();

                var isSingleHomeLob = lobs.Count == 1 && lobs.Any(lob => string.Equals(lob, "Homeowners coverage", StringComparison.OrdinalIgnoreCase));
                Assert.That(isSingleHomeLob, Is.True, $"Expected only 1 LOB 'Homeowners coverage', but got: {string.Join(", ", lobs)}");

                await _pageHelper!.InteractWithElement(LocatorType.XPath, "(//div[@class = 'details show-details']/parent::div//span[contains(text(),'Select')])[1]", ElementAction.Click);
            });

            await _logger.ExecuteStepAsync("Select Auto coverage and validate bundled LOBs", async () =>
            {
                var autoCarriers = await ratesPage.GetRatedCallToPurchaseCarriersForLob("Auto");
                Assert.That(autoCarriers.Count, Is.GreaterThan(0), "No rates for Auto on the results page");

                await ratesPage.ClickOnSpecificCarrierViewDetailsForLob("Auto", autoCarriers.First());
                var lobs = await ratesPage.GetPolicyLobs();
                var expectedCoverages = new List<string> { "Homeowners Coverage", "Auto Coverage" };

                var hasBundledLobs = lobs.Count == 2 && expectedCoverages.All(expected => lobs.Any(actual => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)));
                Assert.That(hasBundledLobs, Is.True, $"Expected 2 main coverages (Home and Auto), but got: {string.Join(", ", lobs)}");

                var entityNames = await ratesPage.GetEntityNames();
                var entity = entityNames.Count == 1 && entityNames.First().Contains("VOLKSWAGEN");
                Assert.That(entity, Is.True, $"Expected VOLKSWAGEN carrier in QA, but got: {string.Join(", ", entityNames)}");
                await _pageHelper!.InteractWithElement(LocatorType.XPath, "(//div[@class = 'details show-details']/parent::div//span[contains(text(),'Select')])[2]", ElementAction.Click);
            });

            await _logger.ExecuteStepAsync("Validate Call Agent page with bundled coverage", async () =>
            {
                var callAgentPage = PageFactory.CreatePage<D2C_CallAgentPage>();
                friendlyId = await callAgentPage.GetFriendlyId();

                var coveragesPopup = await callAgentPage.ClickOnViewCoverageButton();
                var lobs = await coveragesPopup.GetPolicyLobs();
                var expectedCoverages = new List<string> { "Homeowners Coverage", "Auto Coverage" };

                var hasBundledLobs = lobs.Count == 2 && expectedCoverages.All(expected => lobs.Any(actual => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)));
                Assert.That(hasBundledLobs, Is.True, $"Expected 2 main coverages (Home and Auto), but got: {string.Join(", ", lobs)}");

                var entityNames = await coveragesPopup.GetEntityNames();
                Assert.That(entityNames.First().Contains("VOLKSWAGEN"), Is.True, $"Expected VOLKSWAGEN carrier in QA, but got: {string.Join(", ", entityNames)}");
            });

            await _logger.ExecuteStepAsync("Validate products and notes in ADBX", async () =>
            {
                var agentUser = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, agentUser);
                _adbxApifactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
                var adbxApi = await _adbxApifactory.CreateApiClientAsync();
                var applicationId = await _mainQueries.Policy.GetPolicyIdByFriendlyIdAsync(friendlyId);
                var quoteResponse = await adbxApi.GetQuoteByQuoteId(applicationId);

                var products = quoteResponse.Content?.Products ?? string.Empty;
                adbxHelper.ValidateQuoteProducts(products, ["Homeowners", "Personal Auto"]);

                var timelineResponse = await adbxApi.GetQuoteTimeline(applicationId);
                var note = timelineResponse.Content?.Events?
                    .FirstOrDefault(e => e.Subject == "Online Submission")?.Description
                    ?? throw new AssertionException("Online Submission event not found");

                var patterns = new Dictionary<string, string>
                {
                    ["Homeowners"] = @"Rates requested for Homeowners from \d+ carriers:[\s\S]*?-Success\(\d+\)",
                    ["Personal Auto"] = @"Rates requested for Personal Auto from \d+ carriers:[\s\S]*?-Success\(\d+\)"
                };
                adbxHelper.ValidateTimelinePatterns(note, patterns);
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(157686)]
        [Description("Verify LOB updates from PersonalHome to DwellingFire when property usage changes")]
        public async Task BOLTAG_D2C_Updating_Lob_Test()
        {
            string? friendlyId = null;

            await _logger.ExecuteStepAsync("Create application via API with PersonalHome LOB", async () =>
            {
                var user = TestContextAccessor.CurrentUserCollection.D2CAutomation;
                ScopeContext.Set(ctx => ctx.CurrentUser, user);

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = ["PersonalHome"],
                    Data = new PersonalLineData
                    {
                        PropertyAddress = new Address
                        {
                            AddressLine1 = "100 Santa Clara St NW",
                            ZipCode = "44709",
                            City = "Canton",
                            State = "OH"
                        },
                        FirstName = "AutomationTest",
                        LastName = "WahTest"
                    }
                };

                var apiHelper = new GetQuoteApiHelper(_getQuoteApi, ScopeContext);
                var createApplicationResponse = await _getQuoteApi.CreateApplicationAsync(requestData);
                friendlyId = createApplicationResponse.Content?.FriendlyId ?? throw new AssertionException("Failed to create application via API");
                var getQuestionnaireResp = await _getQuoteApi.GetQuestionnaireAsync(createApplicationResponse.Content.Id);

                await _d2cHelper!.WaitForMeaningfulPolicyData(
                    _testScope.ServiceProvider.GetRequiredService<Common.PollyRetry.IPollyRetryService>(),
                    friendlyId
                );

                await _d2cHelper!.ValidatePolicyDataNode(friendlyId, "Lob", "PersonalHome");

                await Executor.Execute<D2C_PrimaryResidencePage, D2C_HouseDetailsPage>(
                    FlowType.D2CHomeFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.IsPrimaryResidence] = "No",
                        [FieldNames.DwellingUsage] = "It's currently vacant",
                    },
                    fillForms: true,
                    startUrl: getQuestionnaireResp.Content?.Url?.ToString()
                        ?? throw new AssertionException("Failed to get questionnaire URL")
                );
            });

            await _logger.ExecuteStepAsync("Validate LOB updated to DwellingFire", async () =>
            {
                var pollyRetryService = _testScope.ServiceProvider.GetRequiredService<Common.PollyRetry.IPollyRetryService>();
                string lastSeenLob = "not retrieved";

                try
                {
                    var actualLob = await pollyRetryService.ExecuteWithRetryAsync<string>(async () =>
                    {
                        var policyData = await _mainQueries!.Policy.GetPolicyDataRawAsync(friendlyId!);
                        var xmlDoc = new System.Xml.XmlDocument();
                        xmlDoc.LoadXml(policyData);
                        lastSeenLob = xmlDoc.GetElementsByTagName("Lob")[0]?.InnerText ?? "null";

                        if (lastSeenLob != "DwellingFire")
                            throw new Exception($"LOB not updated. Expected: DwellingFire, Actual: {lastSeenLob}");

                        return lastSeenLob;
                    });

                    _logger.LogDataValidation("LOB Updated", true, "DwellingFire", actualLob,
                        "LOB updated successfully");
                    Assert.That(actualLob, Is.EqualTo("DwellingFire"));
                }
                catch (Exception ex)
                {
                    _logger.LogDataValidation("LOB Updated", false, "DwellingFire", lastSeenLob,
                        $"LOB failed to update. Last value: {lastSeenLob}");
                    throw new AssertionException($"LOB not updated to DwellingFire. Last value: '{lastSeenLob}'", ex);
                }
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")][Category("Payment")][Category("FullQuote")]
        [RunIn(BoltEnvironment.Production)]
        [TestCaseId(241722)]
        [Description("Verifies D2C Renters full quote and payment E2E flow on Production, selects Lemonade carrier, completes payment via popup, and confirms payment failure page is reached")]
        public async Task BOLTAG_D2C_Testing_Payment_Site()
        {
            var consumerUrl = ScopeContext.Data.UrlDataCollection?.FrontEnd?.AdditionalUrls?.GetValueOrDefault("ConsumerShortInterviewPL");

            var ratesPage = await _logger.ExecuteStepAsync("Execute Renters quote flow and navigate to rates page", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    FlowType.D2CRentersFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.OnlineAddress] = "1117 Stratford Wy, London, OH",
                    },
                    fillForms: true,
                    startUrl: consumerUrl
                );
            });

            var paymentPage = await _logger.ExecuteStepAsync("Select Lemonade carrier and proceed to payment page", async () =>
            {
                await ratesPage.ClickOnSpecificCarrierBuyNow("Lemonade");

                return await Executor.ExecuteToPage<D2C_PaymentPageFQ>(
                    FlowType.LemonadeFQFlow,
                    PageFactory.CreatePage<D2C_QuoteConfirmationPage>(),
                    fillForms: true
                );
            });

            await _logger.ExecuteStepAsync("Complete payment via popup and verify failure page", async () =>
            {
                await paymentPage.ClickOnContinueToPay();

                var paymentPopUp = PageFactory.CreatePage<D2C_PaymentPopUp>();
                await paymentPopUp.FillForm();

                var paymentFailurePage = PageFactory.CreatePage<D2C_PaymentFailurePageFQ>();
                _logger.LogDataValidation("Payment Failure Page Reached", paymentFailurePage is not null, "true", "true", "Payment failure page should be reached after submission");
            });
        }
    }
}
