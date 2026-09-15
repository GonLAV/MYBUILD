using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.D2C.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.D2C.FormData
{
    public static class FieldRegistryD2C
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            #region Lob
            [Lob] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-radio-buttons-multi-selection[@class='online-lobs']//button[@data-automation='{0}']",
                FieldType = UIFieldType.Button,
                DefaultValue = "Auto",
                Pages = [typeof(D2C_LobsPage)],
                Required = true
            },
            #endregion
            #region Address
            [OnlineAddress] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[name='addressAutoComplete']", "#addressAutoComplete" },
                FieldType = UIFieldType.Input,
                DefaultValue = "10317 CRADLEROCK DR DALLAS TX, 75217",
                Pages = [typeof(D2C_YourAddressPage), typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
            },
            #endregion
            #region Property Fields
            [PropertyType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-radio-buttons//label[input[@class='radio-input' and @value='{0}']]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Single Family Home",
                Pages = [typeof(D2C_PropertiesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PLTypeOfDwelling] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-radio-buttons//label[@for = '{0}']",
                FieldType = UIFieldType.Button,
                DefaultValue = "PersonalHome",
                Pages = [typeof(D2C_PropertiesPage)],
                Required = true,
                PreserveCasing = true
            },
            [IsPrimaryResidence] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-single-yes-no//label//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_PrimaryResidencePage)],
                Required = true,
                PreserveCasing = true
            },
            [DwellingUsage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(@class,'radio-button') and contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Primary",
                Pages = [typeof(D2C_PropertiesUsagePage)],
                Required = true,
                PreserveCasing = true
            },
            [OccupancyType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = '{0}']",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Vacant",
                Pages = [typeof(D2C_PropertiesUsagePage)],
                Required = true,
                PreserveCasing = true
            },
            [RoofReplaced] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[]
                {
                    "//div[contains(@class,'option')]//span[contains(text(),'{0}')]",
                    "//div[contains(@class,'option') and contains(.,'{0}')]",
                    "//label[contains(@class,'radio-button') and contains(.,'{0}')]"
                },
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = new HashSet<Type> { typeof(D2C_RoofReplacementPage) },
                Required = true,
                PreserveCasing = true
            },
            [YearRoofReplaced] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'RoofUpdatedYear']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1 year ago",
                Pages = [typeof(D2C_RoofReplacementPage)],
                DependsOn = RoofReplaced,
                DependsOnValue = "Yes"
            },
            [PLRoofMaterial] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='RoofType']/ancestor::app-dropdown",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Composition",
                Pages = new HashSet<Type> { typeof(D2C_HouseDetailsPage) },
                Required = true
            },
            #endregion
            #region Policy Fields
            [EffectiveDate] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[matdatepicker]",
                FieldType = UIFieldType.DatePicker,
                DefaultValue = DateTime.Now.AddDays(1).ToString("dd/MMM/yyyy"),
                Pages = [typeof(D2C_PolicyDatePickerPage)],
                Required = true
            },
            #endregion
            #region Applicant fields
            [FirstName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#FirstName",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetFirstName(fallback: "AutoTest"),
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_FindMyQuotePage), typeof(D2CFarmers_EnterInfoPage)],
                // The shared error-message component renders "<input id>_error" only while the field
                // is invalid, so this locator doubles as the has-error probe.
                Validation = new ValidationRules { Locator = "#FirstName_error" }
            },
            [LastName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#LastName",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetLastName(fallback: "TestLast"),
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_FindMyQuotePage), typeof(D2CFarmers_EnterInfoPage)],
                Validation = new ValidationRules { Locator = "#LastName_error" }
            },
            [DateOfBirth] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#DateOfBirth", "#DOB" },
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/1985",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_FindMyQuotePage), typeof(D2CFarmers_EnterInfoPage)],
                Validation = new ValidationRules { Locator = "#DateOfBirth_error" }
            },
            [Gender] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "ng-select[name='PersonalLineGender']", "ng-select[name='Gender']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Female",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage)],

            },
            [MaritalStatus] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'MaritalStatus']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Single",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage)],

            },
            [LicenseStatus] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'DriverLicenseStatus']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Valid",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage)],

            },
            [TypeOfResidence] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'TypeOfResidence']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Own home",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage)]

            },
            [PrimaryPhoneNumber] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#PrimaryPhoneNumber",
                FieldType = UIFieldType.Input,
                DefaultValue = "123" + RandomManager.GetRandomDigits(7),
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_PersonalDetailBundlePage)],

            },
            [Email] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#Email",
                FieldType = UIFieldType.Input,
                DefaultValue = RandomManager.GetRandomEmail(),
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_FindMyQuotePage)],

            },
            [EmploymentIndustry] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'DriverEmploymentIndustry']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Technology",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_ProgressiveDriversFQ), typeof(D2C_BristolWestDriversFQ)],

            },
            [Education] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'DriverEducation']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Phd",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_ProgressiveDriversFQ), typeof(D2C_BristolWestDriversFQ)],

            },
            [Occupation] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'DriverOccupationStr']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Analyst",
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_AddEditAnotherDriverPopUp), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_ProgressiveDriversFQ), typeof(D2C_BristolWestDriversFQ)],
                Required = true,
                DependsOn = EmploymentIndustry,
            },
            [DriverLicenseNumber] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[id='DriverLicenseNumber']", "#LicenseNumber" },
                FieldType = UIFieldType.Input,
                DefaultValue = "12441058",
                Required = true,
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_DriverDetailsFQ), typeof(D2C_PersonalDetailBundlePage), typeof(D2C_ProgressiveDriversFQ), typeof(D2C_BristolWestDriversFQ)],
                Validation = new ValidationRules
                {
                    MinLength = 8,
                    MaxLength = 15,
                    Pattern = @"^[A-Z0-9]+$"
                }
            },
            [AgreeToReceiveEmail] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[@for = 'IAgreeToReceiveEmailsByBolt']", "//label[@for = 'D2CAgreeToTerms']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Required = true,
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_PersonalDetailBundlePage)],
            },
            [AgreeToReceiveEmailTransactional] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[@for = 'transactionalTCPA']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Required = true,
                Pages = [typeof(D2C_PrimaryDriverPage), typeof(D2C_PersonalDetailsPage), typeof(D2C_PersonalDetailBundlePage)],
            },
            [DriverRelationshipToMainDriver] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name = 'DriverRelationshipToDriver1']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Other",
                Pages = [typeof(D2C_AddEditAnotherDriverPopUp)],
                Required = true,
            },
            #endregion
            #region Vehicle Fields
            [PLYear] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='PLYear']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "2020",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage)],
                Required = true
            },
            [PLMake] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='PLMake']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Toyota",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage)],
                Required = true,
                DependsOn = PLYear
            },
            [PLModel] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='PLModel']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Camry",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage)],
                Required = true,
                DependsOn = PLMake
            },
            [BodyStyle] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='BodyStyle']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Sedan",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage)],
                DependsOn = PLModel
            },
            [VehicleOwnerShip] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='OwnershipType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Owned",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage), typeof(D2C_PrimaryVehiclePage), typeof(D2C_VehiclesDetailsPageFQ)]
            },
            [AnnualMileage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[name='NumberOfMiles']:not([type='hidden'])", "input#NumberOfMiles" },
                FieldType = UIFieldType.Input,
                DefaultValue = "12000",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage), typeof(D2C_PrimaryVehiclePage), typeof(D2C_VehiclesDetailsPageFQ)]
            },
            [OEM] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='OEMCoverage']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_VehiclesDetailsPageFQ)]
            },
            [VIN] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//unmasked-input//following-sibling::input[@name='VIN']",
                FieldType = UIFieldType.Input,
                DefaultValue = "1HGBH41JXMN109186",
                Pages = [typeof(D2C_AddEditAnotherCarPopUp), typeof(D2C_VehiclesPage), typeof(D2C_PrimaryVehiclePage), typeof(D2C_VehiclesDetailsPageFQ), typeof(ProductCommercialVehiclesPageCL)]
            },
            [WasTheCarNew] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='WasTheCarNew']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_VehiclesDetailsPageFQ), typeof(D2C_BristolWestVehiclesFQ)],
                PreserveCasing = true
            },
            [GarageAddress] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "//mat-button-toggle-group[@id='GarageAddressDifferent']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_BristolWestVehiclesFQ)],
                PreserveCasing = true
            },
            [CostNewValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//unmasked-input//following-sibling::input[@name='CostNewValue']",
                FieldType = UIFieldType.Input,
                DefaultValue = "25000",
                Pages = [typeof(D2C_VehiclesDetailsPageFQ)]
            },
            [DateVehiclePurchased] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@name='DateVehiclePurchased']",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Today.AddYears(-3).ToString("MM/dd/yyyy"),
                Pages = [typeof(D2C_VehiclesDetailsPageFQ), typeof(D2C_ProgressiveVehiclesFQ), typeof(D2C_BristolWestVehiclesFQ)]
            },
            [PrimaryUseOfVehicle] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='PrimaryUseOfVehicle']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Pleasure",
                Pages = [typeof(D2C_VehiclesDetailsPageFQ), typeof(D2C_ProgressiveVehiclesFQ), typeof(D2C_BristolWestVehiclesFQ)]
            },
            [ProgressiveVehicleProtection] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQProgressiveVehicleProtection']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ), typeof(D2C_ProgressiveCoveragesFQ)]
            },
            [MilesToWork] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//unmasked-input//following-sibling::input[@name='MilesToWork']",
                FieldType = UIFieldType.Input,
                DefaultValue = "15",
                Pages = [typeof(D2C_VehiclesDetailsPageFQ)]
            },
            #endregion
            #region Coverages Fields
            [BodilyInjuryLiability] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='BI']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$100k per person / $300k per accident",
                Pages = [typeof(D2C_CoveragesPage)],
                Required = true
            },
            [ComprehensiveDeductible] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='CompDeductible']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$250",
                Pages = [typeof(D2C_CoveragesPage)],
                Required = true
            },
            [CollisionDeductible] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='CollDeductible']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$250",
                Pages = [typeof(D2C_CoveragesPage)],
                Required = true
            },
            #endregion
            #region House Details
            [PLSquareFootage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#PLSquareFootage",
                FieldType = UIFieldType.Input,
                DefaultValue = "2000",
                Pages = [typeof(D2C_HouseDetailsPage)],
                Required = true
            },
            [TypeOfHome] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'PLTypeOfDwelling']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Single Family House",
                Pages = [typeof(D2C_HouseDetailsPage)],
                Required = true,
                PreserveCasing = true
            },
            [PLYearBuilt] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#PLYearBuilt",
                FieldType = UIFieldType.Input,
                DefaultValue = "2005",
                Pages = [typeof(D2C_HouseDetailsPage)],
                Required = true
            },
            [DistanceToCoast] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#DistanceToCoast",
                FieldType = UIFieldType.Input,
                DefaultValue = "50",
                Pages = [typeof(D2C_HouseDetailsPage)],
                Required = true
            },
            [ArchitectureStyle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='ArchitectureStyle']/ancestor::app-dropdown",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Traditional",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [RoofType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'RoofType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Asphalt shingles",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [RoofShape] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'MitRoofShape']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Gable",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [TypeOfFoundation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='TypeOfFoundation']/ancestor::app-dropdown",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Slab",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [PLNumberOfStories] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='PLNumberOfStories']/ancestor::app-dropdown",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [PLNumberOfUnits] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='PLNumberOfUnits']",
                FieldType = UIFieldType.Input,
                DefaultValue = "1",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [ConstructionType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='PLConstructionType']/ancestor::app-dropdown",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Frame",
                Pages = [typeof(D2C_HouseDetailsPage)]
            },
            [SafetyProduct] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-checkbox-image[@name = '{0}']",
                FieldType = UIFieldType.Button,
                DefaultValue = "true",
                Pages = [typeof(D2C_SafetyAlarms)],
                PreserveCasing = true
            },
            #endregion
            #region Pet Fields
            [Pet] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@for='{0}']",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Dog",
                Pages = [typeof(D2C_PetsPage)],
                Required = true,
                PreserveCasing = true
            },
            [PetName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input#PLPetName",
                FieldType = UIFieldType.Input,
                DefaultValue = "Woofy",
                Pages = [typeof(D2C_PetsPage)],
                Required = true,
                InteractionOptions = new ElementInteractionOptions
                {
                    UseSequentialTyping = true
                }
            },
            [PetBreedType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='PLPetBreedType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Pure",
                Pages = [typeof(D2C_PetsPage)],
                Required = true
            },
            [PetBreed] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='PLPetBreed']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Akita",
                Pages = [typeof(D2C_PetsPage)],
                Required = true,
                DependsOn = PetBreedType
            },
            [PetGender] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='PLPetGender']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Male",
                Pages = [typeof(D2C_PetsPage)],
                Required = true
            },
            [PetDoB] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@name='PetDOB']",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/2020",
                Pages = [typeof(D2C_PetsPage)],
                Required = true
            },
            [PetSpayedOrNeutered] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@id='PLSpayedNeutered']/../app-yes-no//span[text()= ' {0} ']",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(D2C_PetsPage)],
                Required = true,
                PreserveCasing = true
            },
            [DoesYourPetHaveCushingsDiabetes] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@id='PLHasDisease']/../app-yes-no//span[text()= ' {0} ']",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(D2C_PetsPage)],
                Required = true,
                PreserveCasing = true
            },
            #endregion
            #region Auto Policy Fields
            [YearsWithPriorCarrierAuto] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[id='YearsWithPriorCarrierAuto']",
                FieldType = UIFieldType.Input,
                DefaultValue = "10",
                Pages = [typeof(D2C_SafecoPolicyDpolicyFQ)],
                Required = true
            },
            [YearsWithPriorCarrierAutoUsaa] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='YearsWithPriorCarrierAuto']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "At least 1 year but less than 3",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [AutoInsuranceCancelled] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='AutoInsuranceCancelled']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_SafecoPolicyDpolicyFQ), typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [CurrentInsuranceCompanyAuto] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "ng-select[name='CurrentPersonalAutoCarrier']", "//ng-select[@name='FQData.ProgressivePersonalAuto.CurrentPersonalAutoCarrier']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "AIG",
                Pages = new HashSet<Type> { typeof(D2C_SafecoPolicyDpolicyFQ), typeof(D2C_ProgressiveAdditionalQuestionsFQ) },
                Required = true
            },
            [YearsWithContinuousPersonalAutoCarrier] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[id='YearsWithContinuousCoverageAuto']",
                FieldType = UIFieldType.Input,
                DefaultValue = "10",
                Pages = new HashSet<Type> { typeof(D2C_SafecoPolicyDpolicyFQ), typeof(D2C_ProgressiveAdditionalQuestionsFQ) },
                Required = true
            },
            [VehicleDelivery] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name*='VehDeliveryUse']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_SafecoPolicyDpolicyFQ)],
                Required = true
            },
            [PriorCarrierExpirationDateAuto] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[name='PriorCarrierExpirationDateAuto']",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Today.AddDays(7).ToString("MM/dd/yyyy"),
                Pages = [typeof(D2C_SafecoPolicyDpolicyFQ), typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [SelectPriorLiabilityLimitsAuto] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='SelectPriorLiabilityLimitsAuto']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100K/300K",
                Pages = [typeof(D2C_SafecoPolicyDpolicyFQ)],
                Required = true
            },
            [UM] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='UM']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Reject",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [PropertyDamage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='PD']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "50,000",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [MedicalPayments] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='MP']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [PIPLimit] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='PIP']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "5,000",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [TowingAndLabor] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='TowingAndLabor']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No Coverage",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [TransportationExpense] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='TransportationExpense']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No Coverage",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [FullGlass] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FullGlass']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            #endregion
            #region Driver Details FQ Fields
            [DriverStateLicensed] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='DriverStateLicensed']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "TX",
                Pages = [typeof(D2C_DriverDetailsFQ), typeof(D2C_ProgressiveDriversFQ), typeof(D2C_BristolWestDriversFQ)],
                Required = true
            },
            [DriverDateLicensed] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[name='DriverDateLicensed']",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Today.AddYears(-10).ToString("MM/dd/yyyy"),
                Pages = [typeof(D2C_DriverDetailsFQ), typeof(D2C_ProgressiveDriversFQ), typeof(D2C_BristolWestDriversFQ)],
                Required = true
            },
            [SR22OrFinancialResponsibilityStatement] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='SR22OrFinancialResponsibilityStatement']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_DriverDetailsFQ), typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [SR22OrFinancialResponsibilityStatementBristol] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "//mat-button-toggle-group[@id='SR22OrFinancialResponsibilityStatement']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_BristolWestDriversFQ)],
                Required = true,
                PreserveCasing = true
            },
            [IsDefensiveDriver] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "//mat-button-toggle-group[@id='IsDefensiveDriver']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_BristolWestDriversFQ)],
                Required = true,
                PreserveCasing = true
            },
            [DriversLicenseBeenSuspendedOrRevoked] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='DriversLicenseBeenSuspendedOrRevoked']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_DriverDetailsFQ), typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [GarageAddressDifferent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='GarageAddressDifferent']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [AntiTheft] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='AntiTheft']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Active",
                Pages = [typeof(D2C_BristolWestVehiclesFQ)],
                Required = true
            },
            [AnyModifications] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='AnyModifications']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [ModificationsValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='ModificationsValue']",
                FieldType = UIFieldType.Input,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQNumDaysDrivenPerWeek] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='FQNumDaysDrivenPerWeek']",
                FieldType = UIFieldType.Input,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQVehicleUseForRideShareInd] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQVehicleUseForRideShareInd']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQVehDeliveryUseInd] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQVehDeliveryUseInd']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQBlindSpotWarningInd] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQBlindSpotWarningInd']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQLoanLeaseAmount] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQLoanLeaseAmount']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQLienholderName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQLienholderName']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveVehiclesFQ)],
                Required = true
            },
            [FQLicenseClass] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQLicenseClass']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Personal Auto",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [FQDriverRatingType] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQDriverRatingType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Rated",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [FQSnapshotPhoneNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='FQSnapshotPhoneNumber']",
                FieldType = UIFieldType.Input,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [FQIsStudent100Miles] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQIsStudent100Miles']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [IsGoodStudent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='IsGoodStudent']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [FQMatriculaCard] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQMatriculaCard']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            [FQMatriculaQualifiedToDrive] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='FQMatriculaQualifiedToDrive']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveDriversFQ)],
                Required = true
            },
            #endregion
            #region Additional Questions FQ Fields
            [BodilyInjuryLiabilityLimit] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[name='SelectPriorLiabilityLimitsAuto']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "50/100",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [YearsWithPriorCarrierAuto] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[id='YearsWithPriorCarrierAuto']",
                FieldType = UIFieldType.Input,
                DefaultValue = "10",
                Pages = [typeof(D2C_SafecoPolicyDpolicyFQ), typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [NumPIPClaims] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='FQData.ProgressivePersonalAuto.NumPIPClaims']",
                FieldType = UIFieldType.Input,
                DefaultValue = "5,000",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [OtherProgressivePolicies] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='FQData.ProgressivePersonalAuto.MultiPolicyInd']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [ReasonForNewPolicy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='ReasonForNewPolicy']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "At least 1 year but less than 3",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [PriorPolicyNum] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='FQData.ProgressivePersonalAuto.PriorCarrierPolicyNumAuto']",
                FieldType = UIFieldType.Input,
                DefaultValue = "5,000",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [Houseoccup] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='PL_Houseoccup']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [YearsAtAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='YearsAtAddress']",
                FieldType = UIFieldType.Input,
                DefaultValue = "3",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [MonthsAtAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='MonthsAtAddress']",
                FieldType = UIFieldType.Input,
                DefaultValue = "0",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            [EnrollSavingBank] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.ProgressivePersonalAuto.DsbPkgInd']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true,
                PreserveCasing = true
            },
            [DeductibleSavingsBankBalance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.ProgressivePersonalAuto.DsbBalanceInd']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true,
                PreserveCasing = true
            },
            [DriversRequiredToBeListed] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.ProgressivePersonalAuto.AllHdshldDrvListedInd']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true,
                PreserveCasing = true
            },
            [HouseHoldResidents] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.ProgressivePersonalAuto.eSignAcknowledgeDriversInd']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true,
                PreserveCasing = true
            },
            [VehicleHouseholdCoverage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.ProgressivePersonalAuto.eSignAcknowledgeVehiclesInd']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true,
                PreserveCasing = true
            },
            [TextMessageAboutPolicy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.ProgressivePersonalAuto.AllowText']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true,
                PreserveCasing = true
            },
            [AccidentForgiveness] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='AccidentForgiveness']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ), typeof(D2C_ProgressiveCoveragesFQ)],
                Required = true
            },
            [ConfirmEmailAddr] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='FQData.ProgressivePersonalAuto.ConfirmEmailAddr']",
                FieldType = UIFieldType.Input,
                DefaultValue = "ForceIBSScore900@safeco.com",
                Pages = [typeof(D2C_ProgressiveAdditionalQuestionsFQ)],
                Required = true
            },
            #endregion
            #region Payment Popup Fields
            [CardNumber] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='cardNumber']",
                FieldType = UIFieldType.Input,
                DefaultValue = "4111111111111111",
                Pages = [typeof(D2C_PaymentPopUp)],
                Required = true,
                Validation = new ValidationRules
                {
                    MinLength = 13,
                    MaxLength = 19,
                    Pattern = @"^[0-9]+$"
                }
            },
            [NameOnCard] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[id='NameOnAccountDiv']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Disney",
                Pages = [typeof(D2C_PaymentPopUp)],
                Required = true
            },
            [ExpirationDate] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='expDate']",
                FieldType = UIFieldType.Input,
                DefaultValue = "1128",
                Pages = [typeof(D2C_PaymentPopUp)],
                Required = true,
                Validation = new ValidationRules
                {
                    MinLength = 4,
                    MaxLength = 4,
                    Pattern = @"^[0-9]{4}$"
                }
            },
            [SecurityCode] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='cvc']",
                FieldType = UIFieldType.Input,
                DefaultValue = "125",
                Pages = [typeof(D2C_PaymentPopUp)],
                Required = true,
                Validation = new ValidationRules
                {
                    MinLength = 3,
                    MaxLength = 4,
                    Pattern = @"^[0-9]+$"
                }
            },
            [BillingAddress] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#billingAdd",
                FieldType = UIFieldType.Input,
                DefaultValue = "123 Main St, Dallas, TX 75201",
                Pages = [typeof(D2C_PaymentPopUp)]
            },
            [BillingZipCode] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='CardHolderZipCode']",
                FieldType = UIFieldType.Input,
                DefaultValue = "70210",
                Pages = [typeof(D2C_PaymentPopUp)],
                Required = true,
                Validation = new ValidationRules
                {
                    MinLength = 5,
                    MaxLength = 10,
                    Pattern = @"^[0-9]{5}(-[0-9]{4})?$"
                }
            },
            [MortgageAccountNumber] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[id='MortgageAccountNumberDiv']",
                FieldType = UIFieldType.Input,
                DefaultValue = "1234567890",
                Pages = [typeof(D2C_PaymentPopUp)]
            },
            [TermsOfService] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[type='checkbox']",
                FieldType = UIFieldType.Button,
                DefaultValue = "true",
                Pages = [typeof(D2C_PaymentPopUp)],
                Required = true
            },
            #endregion

            #region StillWaterFQ
            [IsMailAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id = 'IsMailAddress']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
                PreserveCasing = true
            },
            [PurchaseDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@id = 'PurchaseDate']//parent::div//input[@id = 'maskedInput']",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(-5).ToString("dd/MMM/yyyy"),
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true
            },
            [MultiFamilyUnit] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id = 'FQData.StillWaterPersonalHome.MultiFamilyUnit']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
                PreserveCasing = true
            },
            [RoadsideAssistance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id = 'FQData.StillWaterPersonalHome.RoadAccess']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
                PreserveCasing = true
            },
            [YearsAtAddressFQ] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id = 'YearsAtAddress']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
                PreserveCasing = true
            },
            [NumberOfFamilies] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'PL_NumOfFamilies']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true
            },
            [ExistingDamageOnDwelling] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id = 'ExistingDamageOnDwelling']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
                PreserveCasing = true
            },
            [OverhangingTreeBranches] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id = 'FQData.StillWaterPersonalHome.OverhangingTreeBranches']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyInformationPage)],
                Required = true,
                PreserveCasing = true
            },
            [ConvertedResidency] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.ConvertedResidency']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [RoofCondition] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.RoofCondition']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [BusinessOrDaycare] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.BusinessOrDaycare']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [FarmOrRanch] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.FarmOrRanch']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [NumberOfDogsOnPremises] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.NumberOfDogsOnPremises']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [ViciousExoticAnimals] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.ViciousExoticAnimals']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [UnusualVehicleStorage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.UnusualVehicleStorage']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [UnusualConst] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.UnusualConst']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [ExtendedVacancy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.ExtendedVacancy']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalPropertyPage)],
                Required = true,
                PreserveCasing = true
            },
            [FoundationPermanent] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.PermanentFoundation']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterPropertySafetyPage)],
                Required = true,
                PreserveCasing = true
            },
            [DistanceToFireHydrant] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='DistanceToFireHydrant']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "0-500 ft",
                Pages = [typeof(D2C_StillwaterPropertySafetyPage)],
                Required = true
            },
            [LimitedAccessCommunity] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.LimitedAccessLocation']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertySafetyPage)],
                Required = true,
                PreserveCasing = true
            },
            [SharedWalls] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.SharedWalls']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertySafetyPage)],
                Required = true,
                PreserveCasing = true
            },
            [ExteriorWallsConstruction] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='ExteriorWallsConstruction']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Structural Insulated Panels",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [NumberOfFireplaces] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='FQData.StillWaterPersonalHome.NumberOfFireplaces']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [NumOfKitchens] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='FQData.StillWaterPersonalHome.NumOfKitchens']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [KitchenConstructionQuality] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='FQData.StillWaterPersonalHome.KitchenConstructionQuality']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Builder's Grade",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [BathroomType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='BathroomType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Full",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [BathroomConstructionQuality] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='BathroomConstructionQuality']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Builder's Grade",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [NumberOfBathroom] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "#NumberOfBathrooms",
                FieldType = UIFieldType.Input,
                DefaultValue = "2",
                Pages = [typeof(D2C_StillwaterPropertyFeaturesPage)],
                Required = true
            },
            [RoofPolySpray] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.RoofPolySpray']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [RoofPolySprayLast3] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.RoofPolySprayLast3']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [RoofSlopeValid] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.RoofSlopeValid']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PL_AdditionalStructures_Pool] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='PL_AdditionalStructures_Pool']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PL_PoolFeatures_DivingBoard] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='PL_PoolFeatures_DivingBoard']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PL_PoolFeatures_Slide] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='PL_PoolFeatures_Slide']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PoolFence] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='PL_PoolFeatures_4ftFence']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PoolUnfenced] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.PoolUnfenced']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [TrampolineSafety] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.TrampolineSafety']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [HomeHaveAGarage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.HomeHaveAGarage']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true,
                PreserveCasing = true
            },
            [GarageType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'TypeGarageCarport']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Attached",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true
            },
            [PL_NumberCarSpace] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'PL_NumberCarSpace']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true
            },
            [PL_BasementFinish] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'PL_BasementFinish']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100%",
                Pages = [typeof(D2C_StillwaterAdditionalFeaturesPage)],
                Required = true
            },
            [ElectricCircuitBreaker] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='ElectricCircuitBreaker']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterPropertyUtilitiesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PLHeatingType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'PLHeatingType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Electric",
                Pages = [typeof(D2C_StillwaterPropertyUtilitiesPage)],
                Required = true
            },
            [HeatingUpdateYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'HeatingUpdateYN']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "In the past year",
                Pages = [typeof(D2C_StillwaterPropertyUtilitiesPage)],
                Required = true
            },
            [ElectricalUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'ElectricalUpdateYN']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "In the past year",
                Pages = [typeof(D2C_StillwaterPropertyUtilitiesPage)],
                Required = true
            },
            [PlumbingUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name = 'PlumbingUpdateYN']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "In the past year",
                Pages = [typeof(D2C_StillwaterPropertyUtilitiesPage)],
                Required = true
            },
            [SolidFuel] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.SolidFuel']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterPropertyUtilitiesPage)],
                Required = true,
                PreserveCasing = true
            },
            [PLHaveAnyLosses] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='PLHaveAnyLosses']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true,
                PreserveCasing = true
            },
            [NumberOfMortgage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='NumberOfMortgagees']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [ClaimDescription] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='PLLossDescription']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Credit card",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [DateOfViolation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@for= 'DateOfLoss']//parent::div//input[@id = 'maskedInput']",
                FieldType = UIFieldType.Input,
                DefaultValue = "05/05/2020",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [PersonalLineLossAmount] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'PersonalLineLossAmount']",
                FieldType = UIFieldType.Input,
                DefaultValue = "100",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [DateOfLossInput] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'DateOfLossInput']",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(-5).ToString("MM/dd/yyyy"),
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [AnyAdditionalInsured] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='AnyAdditionalInsured']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true,
                PreserveCasing = true
            },
            [COFirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'CoFirstName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Test",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [COMiddleName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'CoMiddleName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "A",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [COLastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'CoLastName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Automation",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [CODateOfBirth] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'CoDateOfBirth']",
                FieldType = UIFieldType.Input,
                DefaultValue = "05/05/1990",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [AnimalLiabilityInsurance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//mat-button-toggle-group[@id='FQData.StillWaterPersonalHome.AnimalLiabiityCoverage']//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true,
                PreserveCasing = true
            },
            [MortgageCompanyName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'MortgageCompanyName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Test Company",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [AgencyAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'AddressLine1']",
                FieldType = UIFieldType.Input,
                DefaultValue = "240 9th Ave",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [AgencyCity] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'City']",
                FieldType = UIFieldType.Input,
                DefaultValue = "New York",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [AgencyZipCode] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id = 'ZipCode']",
                FieldType = UIFieldType.Input,
                DefaultValue = "10001",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [MortgageAccountNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='MortgageAccountNumber']",
                FieldType = UIFieldType.Input,
                DefaultValue = "12345678",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true,
                DependsOn = NumberOfMortgage
            },
            [AgencyState] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@name='State']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "NY",
                Pages = [typeof(D2C_StillwaterCurrentInsurancePage)],
                Required = true
            },
            [InspectionNotification] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@for = 'FQData.StillWaterPersonalHome.InspectionNotification']/span",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Pages = [typeof(D2C_StillwaterCarrierDisclosurePage)],
                Required = true
            },
            [ChangeNotification] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[@for = 'FQData.StillWaterPersonalHome.ChangeNotification']/span",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Pages = [typeof(D2C_StillwaterCarrierDisclosurePage)],
                Required = true
            },
            #endregion
            [OpenDocuSign] = new UIElement
            {
                Strategy = LocatorType.Text,
                Locators = "Open DocuSign to complete",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(D2C_BristolWestEsignFQ)],
                Required = true
            },
        };
        static FieldRegistryD2C() => FieldRegistryProvider.Register(FrontEndType.D2C, Fields);
    }
}