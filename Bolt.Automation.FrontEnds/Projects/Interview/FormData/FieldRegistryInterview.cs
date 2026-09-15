using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.Interview.Pages;
using Bolt.Automation.FrontEnds.Projects.Interview.Popups;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.Interview.FormData.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.Interview.FormData
{
    /// <summary>
    /// Field registry for Interview v3 page objects.
    /// Contains UIElement definitions for all interview fields.
    /// </summary>
    public static class FieldRegistryInterview
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            #region Start Page Fields
            [InterviewAddress] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-address input.pac-target-input",
                FieldType = UIFieldType.Input,
                DefaultValue = "10317 CRADLEROCK DR DALLAS TX, 75217",
                Pages = [typeof(Product_StartPage), typeof(ProductBusinessProfilePageCL)],
                Required = true
            },
            [FirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FirstName')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetFirstName(),
                Pages = [typeof(Product_StartPage), typeof(ProductBusinessProfilePageCL)],
                Required = true
            },
            [LastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.LastName')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetLastName(),
                Pages = [typeof(Product_StartPage), typeof(ProductBusinessProfilePageCL)],
                Required = true
            },
            [DateOfBirth] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DateOfBirth')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/1985",
                Pages = [typeof(Product_StartPage), typeof(Product_ApplicantPage)],
                Required = true
            },
            [PrimaryPhoneNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PrimaryPhoneNumber')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5555551234",
                Pages = [typeof(Product_StartPage), typeof(Product_ApplicantPage), typeof(ProductBusinessProfilePageCL)]
            },
            [Email] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.Email')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "autotest@test.com",
                Pages = [typeof(Product_StartPage), typeof(Product_ApplicantPage), typeof(ProductBusinessProfilePageCL)]
            },
            [OrganizationName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OrganizationName')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "Test Business",
                Pages = [typeof(Product_StartPage), typeof(ProductBusinessProfilePageCL)]
            },
            [EposNaicDescription] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.EposNaicDescription')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "Retail",
                Pages = [typeof(Product_StartPage)]
            },
            #endregion

            #region LOBs Page Fields
            [Lob] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//*[contains(@class,'PolicyData.Lobs[]')]//li[contains(.,'{0}')]", "//app-product-card[.//div[contains(@class,'product-title') and contains(normalize-space(.),'{0}')]]//label" },
                FieldType = UIFieldType.Button,
                DefaultValue = "Homeowners",
                Pages = [typeof(Product_LobsPage), typeof(ProductSelectionPageCL)],
                Required = true,
                PreserveCasing = true
            },
            #endregion

            #region Home Page Fields
            [PLYearBuilt] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLYearBuilt')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2005",
                Pages = [typeof(Product_HomePage)],
                Required = true
            },
            [PLSquareFootage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLSquareFootage')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2000",
                Pages = [typeof(Product_HomePage)],
                Required = true
            },
            [HomeLength] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeLength')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "60",
                Pages = [typeof(Product_HomePage)]
            },
            [HomeWidth] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeWidth')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "30",
                Pages = [typeof(Product_HomePage)]
            },
            [ArchitectureStyle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ArchitectureStyle')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                // Matches the value HomeDetailsData seeds, so the UI and API paths answer this alike.
                DefaultValue = "Contemporary",
                Pages = [typeof(Product_HomePage)]
            },
            [PLTypeOfDwelling] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLTypeOfDwelling')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Single Family",
                Pages = [typeof(Product_HomePage)]
            },
            [OccupancyType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OccupancyType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Owner Primary",
                Pages = [typeof(Product_HomePage)]
            },
            [WhereIsYourDwellingLocated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[@id= 'PolicyDataInCitySuburbDistrictCustomDropdownWhereisyourdwellinglocatedtrue_163_163']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "City",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [IsHomeNewPurchase] = new UIElement
            {
                // Renders as a Yes/No label pair, same shape as ShortTermRental/GatedOrLimited
                // below - the old span.switcher locator strict-mode-matched both options.
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PL_OccupiedOrPurchase')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [PurchaseDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PurchaseDate')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/2020",
                Pages = [typeof(Product_HomePage)],
                //DependsOn = IsHomeNewPurchase,
                //DependsOnValue = "Yes"
            },
            [PurchasePrice] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PurchasePrice')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "300000",
                Pages = [typeof(Product_HomePage)]
            },
            [CurrentlyForSale] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DwellingForSale')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [PLOriginalOwner] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OriginalOwner')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [HomeUnderConstruction] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeUnderConstruction')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [IsHomeUnderMajorRenovation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UnderMajorRenovation')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [IsHomeUnderRenovation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeRenovation')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [PriorRenovation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PriorRenovation')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [ExistingDamageOnDwelling] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ExistingDamageOnDwelling')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [UnitRentedToOthersCoverage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UnitRentedToOthers')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [ShortTermRental] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ShortTermRental')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [AnyResidenceEmployees] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnyResidentialEmployees')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [BusinessOrDaycare] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BusinessOrDaycare')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [IsHomeInsideCityLimits] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.InsideCityLimits')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [GatedOrLimited] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.GatedOrLimited')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [IsCommunityGuarded] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.Guarded24Hours')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true,
                DependsOn = GatedOrLimited,
                DependsOnValue = "Yes"
            },
            [IsYourHomeVisibleToNeighbors] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeVisibleToNeighbors')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [DistanceToFireHydrant] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DistanceToFireHydrant')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                // Label form of the _0_500ft code HomeDetailsData seeds.
                DefaultValue = "0-500 Feet",
                Pages = [typeof(Product_HomePage)]
            },
            [DistanceToFireStation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DistanceToFireStation')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Less than 5 miles",
                Pages = [typeof(Product_HomePage)]
            },
            [DistanceToCoast] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[(@id='PolicyData.DistanceToCoast')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "50",
                Pages = [typeof(Product_HomePage)]
            },
            [StormShutters] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.StormShutters')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [OpeningProtectionAndStrength] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OpeningProtection')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_HomePage)]
            },
            [ViciousExoticAnimals] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ViciousExoticAnimals')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [AnimalBites] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnimalBites')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [NonSmoker] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.NonSmoker')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            [SinkholeInvestigationOrClaim] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.SinkholeInvestigationOrClaim')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true

            },
            [IsInsuredCanceledDeclined] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.InsuranceDeclined')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_HomePage)],
                PreserveCasing = true
            },
            #endregion

            #region Structure Page Fields
            [PLConstructionType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLConstructionType')]//li[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "Frame",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [ConstructionQuality] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ConstructionQuality')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Standard",
                Pages = [typeof(Product_StructurePage)]
            },
            [ConstructionPercentage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ConstMasonryVeneerPct')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "0",
                Pages = [typeof(Product_StructurePage)]
            },
            [ConstructionMasonryPercentage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ConstMasonryPct')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "0",
                Pages = [typeof(Product_StructurePage)]
            },
            [WhichAppliesTheConstructionDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ConstructionDateType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Original",
                Pages = [typeof(Product_StructurePage)]
            },
            [TypeOfFoundation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.TypeOfFoundation')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Slab",
                Pages = [typeof(Product_StructurePage)]
            },
            [SlabType] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='PolicyData.SlabType'] label:has-text('{0}')",
                FieldType = UIFieldType.Radio,
                DefaultValue = "On Grade",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [PLNumberOfStories] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLNumberOfStories')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(Product_StructurePage)]
            },
            [NumberOfFloorsInTheBuilding] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.NumOfFloorsIncBasment')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1",
                Pages = [typeof(Product_StructurePage)]
            },
            [ExteriorWallsConstruction] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ExteriorWallsConstruction')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Stucco",
                Pages = [typeof(Product_StructurePage)],
            },
            [RoofType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.RoofType')]//li[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "Asphalt/Composition",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [MitRoofShapeType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitRoofShape')]//li[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "Gable",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [RoofRating] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.RoofRating')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Good",
                Pages = [typeof(Product_StructurePage)]
            },
            [RoofReplaced] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLRoofUpdated')]//span[contains(@class,'radio')][contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [WindMitigationCreditForm] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitCreditForm')]//span[contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [YearRoofUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.RoofUpdatedYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2020",
                Pages = [typeof(Product_StructurePage)],
                DependsOn = RoofReplaced,
                DependsOnValue = "Complete Update"
            },
            [MitCreditForm] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitCreditForm')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_StructurePage)]
            },
            [WindowAndOpeningProtection] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitWindowOpening')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_StructurePage)],
                DependsOn = MitCreditForm,
                DependsOnValue = "Yes"
            },
            [RoofCover] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitRoofCover')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Unknown",
                Pages = [typeof(Product_StructurePage)],
                DependsOn = MitCreditForm,
                DependsOnValue = "Yes"
            },
            [RoofDeckAttachment] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitRoofDeck')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Unknown",
                Pages = [typeof(Product_StructurePage)],
                DependsOn = MitCreditForm,
                DependsOnValue = "Yes"
            },
            [RoofWall] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitRoofWall')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Unknown",
                Pages = [typeof(Product_StructurePage)],
                DependsOn = MitCreditForm,
                DependsOnValue = "Yes"
            },
            [SecondaryWaterResistance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MitSecWaterResis')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Unknown",
                Pages = [typeof(Product_StructurePage)],
                DependsOn = MitCreditForm,
                DependsOnValue = "Yes"
            },
            [WhatIsTypeOfTheBuilding] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BuildingUseType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Residential",
                Pages = [typeof(Product_StructurePage)]
            },
            [WhatIsThePurposeOfTheBuilding] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='PolicyData.BuildingPurpose'] label:has-text('{0}')",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Residential",
                Pages = [typeof(Product_StructurePage)],
                PreserveCasing = true
            },
            [IsTheBuildingLocatedOverWater] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BuildingOverWater')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No",
                Pages = [typeof(Product_StructurePage)]
            },
            [DoesTheBuildingHaveExtension] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BuildingAdditionExtensionType')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_StructurePage)]
            },
            [ProtectiveSiding] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ProtectiveSiding')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_StructurePage)]
            },
            [HomeTiedDown] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeTiedDown')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_StructurePage)]
            },
            [HomePermanentFoundation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomePermanentFoundation')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_StructurePage)]
            },
            [LandOwnedByApplicant] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.LandOwnedByApplicant')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_StructurePage)]
            },
            [ModularHome] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ModularHome')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_StructurePage)]
            },

            [BasementType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BasementType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Below Grade Basement",
                Pages = [typeof(Product_StructurePage)]
            },
            #endregion

            #region Features Page Fields
            [UndergroundFuelTank] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UndergroundFuelTank')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_FeaturesPage)],
                PreserveCasing = true
            },
            [PLHeatingType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLHeatingType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Electric",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [PL_CentralAC] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PL_CentralAC')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [WaterHeaterType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WaterHeaterType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Electric",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [ElectricalType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ElectricalType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Circuit Breaker",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [PlumbingType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PlumbingType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Entirely Copper",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [WiringType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WiringType')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Copper",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [PLElectricalUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLElectricalUpdated')]//span[contains(@class,'radio')][contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Original",
                Pages = [typeof(Product_FeaturesPage)],
                PreserveCasing = true
            },
            [ElectricalUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ElectricalUpdatedYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2020",
                Pages = [typeof(Product_FeaturesPage)],
                DependsOn = PLElectricalUpdated,
                DependsOnValue = "Complete Update"
            },
            [PLPlumbingUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLPlumbingUpdated')]//span[contains(@class,'radio')][contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Original",
                Pages = [typeof(Product_FeaturesPage)],
                PreserveCasing = true
            },
            [PlumbingUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PlumbingUpdatedYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2020",
                Pages = [typeof(Product_FeaturesPage)],
                DependsOn = PLPlumbingUpdated,
                DependsOnValue = "Complete Update"
            },
            [PLHvacUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLHvacUpdated')]//span[contains(@class,'radio')][contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Original",
                Pages = [typeof(Product_FeaturesPage)],
                PreserveCasing = true
            },
            [HvacUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HvacUpdatedYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2020",
                Pages = [typeof(Product_FeaturesPage)],
                DependsOn = PLHvacUpdated,
                DependsOnValue = "Complete Update"
            },
            [PLWaterHeaterUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLWaterHeaterUpdated')]//span[contains(@class,'radio')][contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Original",
                Pages = [typeof(Product_FeaturesPage)],
                PreserveCasing = true
            },
            [WaterHeaterUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WaterHeaterUpdatedYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2020",
                Pages = [typeof(Product_FeaturesPage)],
                DependsOn = PLWaterHeaterUpdated,
                DependsOnValue = "Complete Update"
            },
            [PLRoofUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLRoofUpdated')]//span[contains(@class,'radio')][contains(.,'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Original",
                Pages = [typeof(Product_FeaturesPage)],
                PreserveCasing = true
            },
            [RoofUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.RoofUpdatedYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2020",
                Pages = [typeof(Product_FeaturesPage)],
                DependsOn = PLRoofUpdated,
                DependsOnValue = "Complete Update"
            },
            [FullBathNum] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FullBathNum')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [HalfBathNum] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HalfBathNum')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [NumberOfFirePlaces] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.NumberOfFirePlaces')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [PL_TypeFireplaces] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PL_TypeFireplaces')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Gas",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [SupplementalHeatSource] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.SupplementalHeatSource')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [WoodBurningStove] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WoodBurningStove')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [SmokeDetector] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.SmokeDetector')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [FireExtinguisher] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FireExtinguisher')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [SprinklerSystem] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.SprinklerSystem')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [DeadBoltLocks] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DeadBoltLocks')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [BurglarAlarm] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BurglarAlarm')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_FeaturesPage)]
            },
            [FireDetection] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FireDetection')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_FeaturesPage)]
            },
            #endregion

            #region Vehicle Page Fields
            [VIN] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//*[contains(@class,'PolicyData.VIN')]//input ", " //*[contains(@class,'vehicle-search-block')]//input[@placeholder='Type VIN Number']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "4T1BF1FK0FU105897",
                Pages = [typeof(Product_VehiclePage), typeof(BlockBindVehiclePageCL)]
            },
            [PLYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLYear') or (contains(@class,'PolicyDataVehicles') and contains(@class,'YearVehicleDropdownWithSearch'))]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "2020",
                Pages = [typeof(Product_VehiclePage), typeof(ProductCommercialVehiclesPageCL)],
                Required = true
            },
            [PLMake] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLMake') or (contains(@class,'PolicyDataVehicles') and contains(@class,'MakeVehicleDropdownWithSearch'))]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Toyota",
                Pages = [typeof(Product_VehiclePage), typeof(ProductCommercialVehiclesPageCL)],
                Required = true,
                DependsOn = PLYear
            },
            [PLModel] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLModel') or (contains(@class,'PolicyDataVehicles') and contains(@class,'ModelVehicleDropdownWithSearch'))]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Camry",
                Pages = [typeof(Product_VehiclePage), typeof(ProductCommercialVehiclesPageCL)],
                Required = true,
                DependsOn = PLMake
            },
            [BodyStyle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BodyStyle') or (contains(@class,'PolicyDataVehicles') and contains(@class,'BodyStyleCustomDropdown'))]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Sedan",
                Pages = [typeof(Product_VehiclePage), typeof(ProductCommercialVehiclesPageCL)],
                DependsOn = PLModel
            },
            [VehicleOwnerShip] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OwnershipType') or (contains(@class,'PolicyDataVehicles') and contains(@class,'CLLengthVehicleOwnership'))]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Owned",
                Pages = [typeof(Product_VehiclePage)]
            },
            [AnnualMileage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnnualMileage')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "12000",
                Pages = [typeof(Product_VehiclePage)],
            },
            [TruckSubCategory] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'TruckSubCategoryCustomDropdown')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Pickup",
                Pages = [typeof(Product_VehiclePage)]
            },
            [TrailerLength] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'TrailerLengthUnmaskedInput')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "20",
                Pages = [typeof(Product_VehiclePage)],
                DependsOn = TruckSubCategory,
                DependsOnValue = "Utility Trailer",
                InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
            },
            #endregion

            #region Operator Page Fields
            [OperatorDateOfBirth] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OperatorDateOfBirth') or contains(@class,'OperatorDOBDateInput')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/1985",
                Pages = [typeof(Product_OperatorPage)],
                Required = true
            },
            [OperatorGender] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'OperatorGender')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Male",
                Pages = [typeof(Product_OperatorPage)]
            },
            [Gender] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//*[contains(@class,'.Gender ')]//input[@id='{0}']/..", "//*[contains(@class,'PolicyData.PersonalLineGender')]//label[contains(.,'{0}')]/span" },
                FieldType = UIFieldType.Button,
                DefaultValue = "Male",
                PreserveCasing = true,
                Pages = [typeof(Product_OperatorPage), typeof(Product_ApplicantPage)]
            },
            [MaritalStatus] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//ng-select[contains(@id,'MaritalStatus')]", "//*[contains(@class, 'PolicyData.MaritalStatus')]//ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Single",
                Pages = [typeof(Product_OperatorPage), typeof(Product_ApplicantPage)]
            },
            [PermanentResidenceOfHousehold] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PermanentResidentofHousehold')]//span[contains(., '{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                PreserveCasing = true,
                Pages = [typeof(Product_OperatorPage)]
            },
            [VehicleUsage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'Wheredoesthisoperatorusethevehicles')]//span[contains(., '{0}') and @class='checkbox-body']",
                FieldType = UIFieldType.Button,
                DefaultValue = "true",
                PreserveCasing = true,
                Pages = [typeof(Product_OperatorPage)]
            },
            [DriversLicenseBeenSuspendedOrRevoked] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'DriversLicenseBeenSuspendedOrRevoked')]//span[contains(., '{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_OperatorPage)]
            },
            [OperatorRelationship] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DriverRelationship')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Self",
                Pages = [typeof(Product_OperatorPage)]
            },
            [OperatorLicenseState] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DriverStateLicensed')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "TX",
                Pages = [typeof(Product_OperatorPage)]
            },
            [DriverLicenseNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DriverLicenseNumber') or contains(@class,'DriverLicenseNumberUnmaskedInput')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "12345678",
                Pages = [typeof(Product_OperatorPage), typeof(ProductApplicantsAndDriversPageCL)]
            },
            [LicenseStatus] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DriverLicenseStatus')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Valid",
                Pages = [typeof(Product_OperatorPage)]
            },
            [DriverDateLicensed] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DriverDateLicensed')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/2005",
                Pages = [typeof(Product_OperatorPage)]
            },
            [SR22OrFinancialResponsibilityStatement] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] {
                    "//*[contains(@class,'PolicyData.SR22')]//span[contains(@class,'switcher')]",
                    "//*[contains(@class,'PolicyDataCLDrivers') and contains(@class,'SRRequired')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]"
                },
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_OperatorPage)],
                PreserveCasing = true
            },
            [HasCommercialLicense] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataCLDrivers') and contains(@class,'HasCommercialLicense')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_OperatorPage)],
                PreserveCasing = true,
                Required = true
            },
            [ExcludedDriver] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataCLDrivers') and contains(@class,'ExcludedDriver')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_OperatorPage)],
                PreserveCasing = true,
                Required = true
            },
            #endregion

            #region Policy Page Fields
            [EffectiveDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.EffectiveDate')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy"),
                Pages = [typeof(Product_PolicyPage), typeof(Product_MotorcyclePolicyPage), typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL), typeof(ProductCoverageDetailsPageCL)],
                Required = true
            },
            [CurrentPersonalHomeownerCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CurrentPersonalHomeownerCarrier')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Other",
                Pages = [typeof(Product_PolicyPage)]
            },
            [YearsWithPriorCarrierHome] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.YearsWithPriorCarrierHome')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5",
                Pages = [typeof(Product_PolicyPage)]
            },
            [YearsWithContinuousCoverageHome] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.YearsWithContinuousCoverageHome')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PriorBOPCarrierExpirationDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//*[@id = 'PolicyData.PriorCarrierExperationDate']//input[contains(@class,'form-control')]" },
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"),
                Pages = [typeof(ProductInsuranceHistoryPageCL)],
                Required = true,
                DependsOn = CurrentBopCarrier,
                InteractionOptions = new ElementInteractionOptions { Timeout = 5000 }
            },
            [PriorPersonalHomeLiability] = new UIElement
            {
                // Same brittle exact-@id issue as PLAllPerilsDeductible/WindstormDeductible above:
                // the hardcoded positional suffix (_85_85) shifts depending on which fields render
                // before it, so this stopped matching and the field was silently left unfilled.
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PariorLiabilityCoverageHome')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$300,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            // Untagged on purpose: on the API-seeded flows this question arrives already answered from
            // the payload, so tagging it would overwrite that value. Tests that start from a blank
            // account and actually see it unanswered drive it explicitly.
            [NumberOfMortgagees] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.NumberOfMortgagees')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "0",
                Pages = []
            },
            [CurrentInsuranceCompanyAuto] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CurrentPersonalAutoCarrier')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Other",
                Pages = [typeof(Product_PolicyPage)]
            },
            [YearsWithPriorCarrierAuto] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.YearsWithPriorCarrierAuto')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5",
                Pages = [typeof(Product_PolicyPage)]
            },
            [MonthsWithPriorCarrierAuto] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MonthsWithPriorCarrierAuto')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "0",
                Pages = [typeof(Product_PolicyPage)]
            },
            [YearsWithContinuousPersonalAutoCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.YearsWithContinuousCoverageAuto')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5",
                Pages = [typeof(Product_PolicyPage), typeof(ProductInsuranceHistoryPageCL)],
                DependsOn = PriorCarrierAutoCL
            },
            [PriorCarrierExpirationDateAuto] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PriorCarrierExpirationDateAuto')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"),
                Pages = [typeof(Product_PolicyPage)]
            },
            [SelectPriorLiabilityLimitsAuto] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.SelectPriorLiabilityLimitsAuto')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100K/300K",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PersonalLineReplacementCost] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PersonalLineReplacementCost')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "300000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PLOtherStructures] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLOtherStructures')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "30000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PLPersonalProperty] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLPersonalProperty')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "150000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [LossOfUse] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.LossOfUse')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "60000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PLPersonalLiability] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLPersonalLiability')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [DwellingMedicalPayments] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DwellingMedicalPayments')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PLAllPerilsDeductible] = new UIElement
            {
                // The old exact-@id locator hardcoded an Angular-generated positional suffix
                // (_230_230) that shifts depending on which fields render before it on the page.
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLAllPerilsDeductible')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [HurricaneDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnnualHurricaneDed')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "2%",
                Pages = [typeof(Product_PolicyPage)]
            },
            [BodilyInjuryLiability] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.BI')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100/300",
                Pages = [typeof(Product_PolicyPage)]
            },
            [LiabilityPropertyDamage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PD')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [UninsuredMotorist] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UM')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100/300",
                Pages = [typeof(Product_PolicyPage)]
            },
            [UnderinsuredMotorist] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UIM')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100/300",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PIPLimit] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PIP')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "10,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [MedicalPayments] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'MedicalPayments')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "5,000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [CollisionDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CollDeductible')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "500",
                Pages = [typeof(Product_PolicyPage)]
            },
            [ComprehensiveDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CompDeductible')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "500",
                Pages = [typeof(Product_PolicyPage)]
            },
            [TowingAndLabor] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'TowingAndLabor')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No Coverage",
                Pages = [typeof(Product_PolicyPage)]
            },
            [TransportationExpense] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'TransportationExpense')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No Coverage",
                Pages = [typeof(Product_PolicyPage)]
            },
            [FullGlass] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FullGlass')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [AutoInsuranceCancelled] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AutoInsuranceCancelled')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PropertyInsuranceCancelled] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PropertyInsuranceCancelled')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)],
                PreserveCasing = true
            },
            [IsExistingClientInAgency] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ExistingClient')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [IsAdditionalInterests] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLAdditionalInterests')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },

            // Additional UM/UIM Fields
            [UMPD] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UMPD')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Reject",
                Pages = [typeof(Product_PolicyPage)]
            },
            [UIMPD] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UIMPD')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Reject",
                Pages = [typeof(Product_PolicyPage)]
            },
            [UMStacking] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.StackedUM')]//span[contains(@class,'radio')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Non-Stacked",
                Pages = [typeof(Product_PolicyPage)],
                PreserveCasing = true
            },
            [UIMStacking] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.StackedUIM')]//span[contains(@class,'radio')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Non-Stacked",
                Pages = [typeof(Product_PolicyPage)],
                PreserveCasing = true
            },
            [HowMuchExcessUninsuredUnderinsuredMotorist] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UninsuredMotorist')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100/300",
                Pages = [typeof(Product_PolicyPage)]
            },
            [UninsuredMotoristOption] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UMOPTION')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Standard",
                Pages = [typeof(Product_PolicyPage)]
            },

            // PIP Fields
            [PIPDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PIPDeductible')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [PIPApplies] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PIPApplies')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Named Insured",
                Pages = [typeof(Product_PolicyPage)]
            },
            [WageLoss] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WageLoss')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },

            // Additional Auto Coverages
            [CL_TransportationExpense] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_TransportationExpense')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No Coverage",
                Pages = [typeof(Product_PolicyPage), typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },
            [RoadSideAssistance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CL_RoadSide')]//span[contains(@class,'radio')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)],
                PreserveCasing = true
            },

            // Medical/Disability Coverages
            [AutoDeathIndemnity] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AutoDeathIndemnity')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [TotalDisability] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.TotalDisability')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [IncomeLoss] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.IncomeLoss')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [FirstPartyMedicalBenefits] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FirstParty')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [FirstPartyBenefits] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FirstPartyBenefits')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [ExtraordinaryMedicalExpense] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ExtraMedicalExp')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [TortThreshold] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.TortThreshold')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Full Tort",
                Pages = [typeof(Product_PolicyPage)]
            },

            // Additional Deductibles
            [WindHailDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WindHailDedHome')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [WindstormDeductible] = new UIElement
            {
                // Same brittle exact-@id issue as PLAllPerilsDeductible above.
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WindstormDeductible')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1%",
                Pages = [typeof(Product_PolicyPage)]
            },

            // Liability Additional Fields
            [PersonalUmbrellaLimit] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.UmbrellaLimit')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "None",
                Pages = [typeof(Product_PolicyPage)]
            },
            [GeneralLiabilityLimit] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id , 'PolicyDataGeneralLiabilityLimit')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$300,000/$600,000",
                Pages = [typeof(Product_PolicyPage), typeof(Product_CLPolicyPage)]
            },
            [UnderlyingAutoLiabilityLimit] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AutosLimit')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100/300",
                Pages = [typeof(Product_PolicyPage)]
            },
            [CL_BIPD] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CL_BIPD')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "100/300",
                Pages = [typeof(Product_PolicyPage), typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },
            [CL_MedPay] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//*[contains(@class,'PolicyData.CL_MedPay')]//ng-select", "//*[contains(@class,'CL_MedPay')]//ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "1,000",
                Pages = [typeof(Product_PolicyPage), typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },

            // Dwelling Additional Fields
            [ActualCashValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ActualCashValue')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "250000",
                Pages = [typeof(Product_PolicyPage)]
            },

            // Additional Toggle Questions
            [HasInsuranceCompanyCancelledDeclinedRefusedRenewal] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DeclinedCanceledOrNonRenewed')]//label[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_PolicyPage), typeof(ProductInsuranceHistoryPageCL)]
            },
            [PLHaveAnyLosses] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PLHaveAnyLosses')]//label[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_PolicyPage), typeof(ProductInsuranceHistoryPageCL)]
            },
            [IsCoverageDeclinedCancelledPastThreeYears] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PropertyInsuranceCancelledNonRenewed')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [GapInCoverage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'LapsInCoverage')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)],
                PreserveCasing = true
            },
            [AutoHomeInsurance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AutoHomeInsurance')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [HomeAutoInsurance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HomeAutoInsurance')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage), typeof(Product_CLPolicyPage)],
                PreserveCasing = true
            },

            // Special Coverages
            [EarthquakeCoverage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.EarthquakeCov')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [IsNFIPFloodPolicy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FloodHazardAreaPolicy')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [LiabilityExtension] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.LiabilityExt')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [ExcessLiability] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HVHExcessLiability')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },
            [ValuableArticles] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.HVHInlandMarine')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)]
            },

            // Commercial/Business Fields
            [CurrentBopCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'CurrentBopCarrier')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Allied",
                Pages = [typeof(Product_PolicyPage), typeof(Product_CLPolicyPage), typeof(ProductInsuranceHistoryPageCL)],
            },
            [CurrentWCCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'CurrentWCCarrier')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = " I do not currently have insurance ",
                Pages = [typeof(Product_PolicyPage), typeof(ProductInsuranceHistoryPageCL)]
            },
            // "Insured has no existing commercial insurance coverages with any carrier e.g. New Business",
            // on the Insurance History page every CL flow shares (BOP, GL, WC and Commercial Auto).
            // Ticking it forces that LOB's prior-carrier dropdown to "I do not currently have insurance"
            // and disables it, then reveals a required "reason no prior ..." follow-up. Default "false"
            // because the registry already carries the prior-carrier answer for each LOB
            // (CurrentBopCarrier / CurrentWCCarrier / PriorCarrierAutoCL), and those only apply when the
            // box is clear. A scenario whose policy data really is a new business overrides this to
            // "true" from the test - see CL_ReasonNoPriorAutoInsurance below, which it reveals.
            [IsNewBusiness] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.IsNewBusiness')]//label[contains(@class,'checkbox-wrapper')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(ProductInsuranceHistoryPageCL)]
            },
            // Revealed on a Commercial Auto quote once IsNewBusiness is ticked, and required once shown.
            //
            // What skips it is the DependsOn gate, not the empty default: FormDataHelper.ShouldFillField
            // reads the *caller's* form data, so the field is skipped only on a quote that does not pass
            // IsNewBusiness at all. A test that ticks the box has to supply one of the dropdown's options,
            // or the fill attempts it with "" and eats a not-found timeout before IgnoreIfNotFound drops
            // it. That is what the WC scenario does today - it sets IsNewBusiness="true" and this control
            // does not exist on its page. Harmless, but it costs a timeout per WC run, and it is the
            // reason to register the WC and BOP equivalents (both still missing) rather than widen this
            // one: each LOB reveals its own "reason no prior ..." control.
            [CL_ReasonNoPriorAutoInsurance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[@id='PolicyData.CL_ReasonNoPriorAutoInsurance']//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ProductInsuranceHistoryPageCL)],
                DependsOn = IsNewBusiness,
                DependsOnValue = "true"
            },
            [EmployeeClassificationSearch] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-employee-classification ng-select input[role='combobox']",
                FieldType = UIFieldType.SearchDropdown,
                DefaultValue = "",
                Pages = [typeof(Product_EmployeePage)],
                InteractionOptions = new ElementInteractionOptions
                {
                    Timeout = 15000
                }
            },
            [EmployeeClassificationPayroll] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-employee-classification *[id*='TotalPayroll'] input",
                FieldType = UIFieldType.Input,
                DefaultValue = "50000",
                Pages = [typeof(Product_EmployeePage)],
                // Ordering only (no DependsOnValue gate) - the payroll input isn't rendered until
                // a class is selected, so GetOrderedFields must process the search field first.
                DependsOn = EmployeeClassificationSearch,
                InteractionOptions = new ElementInteractionOptions
                {
                    Timeout = 10000
                }
            },
            // Owners & Officers' per-officer employee class typeahead (same ng-select-typeahead
            // widget/semantics as EmployeeClassificationSearch above, different page/locator).
            [OfficerEmployeeClassificationSearch] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[id*='ClassCodeCustomAutocomplete'] input[role='combobox']",
                FieldType = UIFieldType.SearchDropdown,
                DefaultValue = "",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                InteractionOptions = new ElementInteractionOptions
                {
                    Timeout = 15000
                }
            },
            [PriorCarrierExpirationDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "(//*[@id = 'PolicyData.WcExpirationDate']//input)[1]", "//*[@id = 'PolicyData.PriorCarrierExperationDate']//input[contains(@class,'form-control')]" },
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"),
                Pages = [typeof(Product_PolicyPage), typeof(ProductInsuranceHistoryPageCL)]
            },
            [AnnualRevenue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'AnnualSales')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "500000",
                Pages = [typeof(Product_PolicyPage), typeof(ProductBusinessProfilePageCL)]
            },
            [CurrentPremium] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CurrentPremiumWC')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5000",
                Pages = [typeof(Product_PolicyPage)]
            },
            [WCContinuousCoverage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.WCContinousCoverage')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "Yes",
                Pages = [typeof(Product_PolicyPage)]
            },
            [TypeOfResidence] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'PolicyDataTypeOfResidence')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Rent Home",
                Pages = [typeof(Product_MotorcyclePolicyPage)]
            },
            [MotorcycleCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'PolicyDataPriorCarrierMotorcycle')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "AAA",
                Pages = [typeof(Product_MotorcyclePolicyPage)]
            },
            [MotorcyclePriorCarrierExpirationDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PriorCarrierExpirationDate')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.DatePicker,
                DefaultValue = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy"),
                Pages = [typeof(Product_MotorcyclePolicyPage)]
            },
            [MotorcycleIndictedOrConvictedFelony] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'ConvictedOfFelony')]//span[contains(., '{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_MotorcyclePolicyPage)]
            },
            #endregion

            #region Motorcycle Coverage Page Fields
            [MotorcycleLiabilityLimit] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleLiability')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "25/50",
                Pages = [typeof(Product_MotorcycleCoveragePage)],
                Required = true
            },
            [MotorcyclePropertyDamage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcyclePropertyDamage')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$20,000",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleUninsuredMotorist] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleUninsuredCustomDropdown')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "25/50",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleUninsuredMotoristPropertyDamage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleUninsuredPropertyDamage')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$20,000",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleMedicalPayments] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleMP')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$1,000",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleRoadAssistance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'RoadSideCoverage')]//span[contains(., '{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleComprehensiveDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleComprehensiveDed')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$500",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleCollisionDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleCollisionDed')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$500",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [MotorcycleAccesoriesValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[contains(@id,'AccessoriesCoverage')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "500",
                Pages = [typeof(Product_MotorcycleCoveragePage)]
            },
            [CurrentAnnualPremium] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DiscloseHomeCurrentPremium')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_PolicyPage)],
                PreserveCasing = true
            },
            #endregion

            #region Applicant Page Fields
            [ApplicantSSN] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.SSN')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "123456789",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [MiddleName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MiddleName')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "M",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [ApplicantMailingAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MailingAddress')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "123 Main St",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [MailingAddressDifferent] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.MailingAddressDifferent')]//span[contains(@class,'switcher')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [ForeclosureOrRepossessionOrBankruptcy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ForeclosureOrRepossessionOrBankruptcy')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_ApplicantPage)],
                PreserveCasing = true
            },

            [YearsAtAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[contains(@id,'PolicyDataYearsAtAddress')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "3",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [MonthsAtAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[contains(@id,'PolicyDataMonthsAtAddress')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "3",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [AgreeToReceiveEmail] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.IAgreeToReceiveEmailsByBolt')]//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                PreserveCasing = true,
                Pages = [typeof(Product_ApplicantPage)]
            },
            [EmploymentIndustry] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class, 'PolicyData.EmploymentIndustry')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Art/Design/Media",
                Pages = [typeof(Product_ApplicantPage)]
            },
            [Occupation] = new UIElement
            {
                // The option list is fetched from the server once EmploymentIndustry is answered, so the
                // control can take tens of seconds to appear. DependsOn pins the parent-first order and
                // the timeout gives the fill pass a real wait; IgnoreIfNotFound still lets pages that
                // never render this question move on.
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class, 'PolicyData.OccupationStr')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Other",
                Pages = [typeof(Product_ApplicantPage)],
                DependsOn = EmploymentIndustry,
                InteractionOptions = new ElementInteractionOptions { Timeout = 30000 }
            },
            // Tagged to the Applicant page: UI-driven entry points (blank ADBX account, consumer
            // interview from a URL) have no payload to answer these, and the value the seeded flows send
            // matches the default here, so tagging does not change what those quotes end up with.
            [IsMailAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.IsMailAddress')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_ApplicantPage)],
                PreserveCasing = true
            },
            [AnyAdditionalInsured] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnyAdditionalInsured')]//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_ApplicantPage)],
                PreserveCasing = true
            },
            #endregion

            #region Results Page Fields
            [EmailQuoteButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(@class,'Email Quote')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(Product_ResultsPage)]
            },
            [DownloadAllFormsButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(@class,'Download all')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(Product_ResultsPage)]
            },
            [DownloadAllFormsButtonPopup] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(@class,'All Download Forms')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(Interview_ApplicationFormsPopup)]
            },
            #endregion

            #region Commercial Lines (WC) Fields
            [FederalIDNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.FederalIDNumber')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "999951255",
                Pages = [typeof(Product_BusinessPage), typeof(ProductBusinessProfilePageCL), typeof(ProductAdditionalQuestionsCNAPageCL), typeof(ProductAdditionalQuestionsEmployersPageCL)],
                Required = true
            },
            [BusinessStartYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.StartYear')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2019",
                Pages = [typeof(Product_BusinessPage), typeof(ProductBusinessProfilePageCL)]
            },
            [PolicyDataAnnualSales] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnnualSales')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "250000",
                Pages = [typeof(Product_BusinessPage)]
            },
            [AnnualOwnerPayroll] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.AnnualOwnerPayroll')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "50000",
                Pages = [typeof(Product_BusinessPage)]
            },
            [LegalEntity] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.LegalEntity')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Individual",
                Pages = [typeof(Product_BusinessPage), typeof(ProductBusinessProfilePageCL)]
            },
            [ManagerExperience] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.YearsOfManagementExperience')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "15",
                Pages = [typeof(Product_BusinessPage), typeof(ProductBusinessProfilePageCL)],
                Required = true
            },
            [SubcontractedWork] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'AnySubcontractedWork')]//label",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "No",
                Pages = [typeof(Product_LocationsPage)]
            },
            [LocationSquareFootage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'SquareFootageOccupied')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2000",
                Pages = [typeof(Product_LocationsPage)],
                Required = true
            },
            [PersonalPropertyReplacementCost] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PersonalPropertyReplacementCost')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "10000",
                Pages = [typeof(Product_LocationsPage)],
                Required = true
            },
            [AnnualPayroll] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'AnnualPayroll')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "10000",
                Pages = [typeof(Product_LocationsPage)],
                Required = true
            },
            [InsureTheBuilding] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'DoYouNeedToInsureTheBuilding')]//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_LocationsPage)]
            },
            [NumberOfEmployees] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.NumberOfEmployees')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1",
                Pages = [typeof(Product_BusinessPage)]
            },
            [ExperienceModification] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.ExperienceModification')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1.00",
                Pages = [typeof(Product_EmployeePage)]
            },
            [WCWhoIsCovered] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'WCWhoIsCovered')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "My employees only",
                Pages = [typeof(Product_EmployeePage), typeof(ProductInsuranceHistoryPageCL)]
            },
            [NumberOfFullTimeEmployees] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@id, 'FullTime')]//*[contains(@class, 'icon-plus')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "1",
                Pages = [typeof(Product_EmployeePage)]
            },
            [NumberOfPartTimeEmployees] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@id, 'PartTime')]//*[contains(@class, 'icon-plus')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "0",
                Pages = [typeof(Product_EmployeePage)]
            },
            [EmployeeClassCode] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-employee-search//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "8868",
                Pages = [typeof(Product_EmployeePage)]
            },
            [EmployeePayroll] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'AnnualLocationPayroll')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "50000",
                Pages = [typeof(Product_EmployeePage)],
                Required = true
            },
            [EmployeeClassCodeText] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'EmployeeClassDetails')]//div[@role='option']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "College - professional employees and clerical",
                Pages = [typeof(Product_EmployeePage)]
            },
            [CreditCheckPermission] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CreditCheckPermission')]//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Yes",
                PreserveCasing = true,
                Pages = [typeof(Product_EmployeePage), typeof(Product_ApplicantPage), typeof(ProductBusinessProfilePageCL)]
            },
            [InsuranceFraud] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id='PolicyData.InsuranceFraud']//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_ApplicantPage)],
            },
            [OfficerAnnualPayroll] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'AnnualPayroll')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "50000",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            [OfficerDateOfBirth] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'DateOfBirth')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/1980",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            // The FirstName/LastName keys above are scoped to a "PolicyData.FirstName"-prefixed
            // class, which doesn't match here - this row's class is the nested
            // "PolicyData.Officers[<id>].FirstName", so it needs its own id-substring-based entry.
            [OfficerFirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@id,'FirstNameUnmaskedInput')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "Jane",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            [OfficerLastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@id,'LastNameUnmaskedInput')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "Doe",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            [OfficerTitle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'TitleRelationshipCustomDropdown')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Owner",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            [OfficerOwnershipPercent] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@id,'OwnershipPercentageUnmaskedInput')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "100",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            [OfficerPrimaryStateOfOperation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'AssignLocationsOfficerCustomDropdown')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "TX",
                Pages = [typeof(ProductOwnersAndOfficersPageCL)],
                Required = true
            },
            [WaiverSubrogation] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'WaiverSubrogation')]//label[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(ProductCoverageDetailsPageCL)],
                Required = true
            },
            [PrimaryNonContributory] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PrimaryNonContributory')]//label[contains(.,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(ProductCoverageDetailsPageCL)],
                Required = true
            },
            [QuoteAllEligible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(.,'Quote all eligible')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Pages = [typeof(ProductMarketSelectionsPageCL)]
            },
            [AcordAppetiteWC] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(concat(' ', normalize-space(@class), ' '), ' AppetiteSelection.AcordAppetite.WC ')]//span[contains(text(),'{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(ProductMarketSelectionsPageCL)]
            },
            // A synthetic checkbox the ACORD 130 form injects above the real questions; checking it
            // answers every question on the page "Yes". Its container is keyed off the first real
            // question's field name ("generated_PolicyData.ApplicantOwn"), not its own - that prefix
            // stays stable regardless of which question happens to render first.
            [AcordMarkAllAsYes] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'generated_PolicyData.ApplicantOwn')]//label[contains(@class,'checkbox-wrapper')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Pages = [typeof(ProductACORD130PageCL)],
                InteractionOptions = new ElementInteractionOptions { Timeout = 10000 }
            },
            #endregion

            #region Email Quotes Popup Fields
            [EmailRecipient] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-emails-input//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "boltautomation@boltinc.com",
                Pages = [typeof(Interview_EmailQuotesPopup)]
            },
            //[EmailQuoteSenderName] = new UIElement
            //{
            //    Strategy = LocatorType.XPath,
            //    Locators = "//*[contains(@class,'SenderName')]//input",
            //    FieldType = UIFieldType.Input,
            //    DefaultValue = "",
            //    Pages = [typeof(Interview_EmailQuotesPopup)]
            //},
            //[EmailQuoteSubject] = new UIElement
            //{
            //    Strategy = LocatorType.XPath,
            //    Locators = "//*[contains(@class,'Subject')]//input",
            //    FieldType = UIFieldType.Input,
            //    DefaultValue = "",
            //    Pages = [typeof(Interview_EmailQuotesPopup)]
            //},
            //[EmailQuoteMessage] = new UIElement
            //{
            //    Strategy = LocatorType.XPath,
            //    Locators = "//*[contains(@class,'Message')]//textarea",
            //    FieldType = UIFieldType.Input,
            //    DefaultValue = "",
            //    Pages = [typeof(Interview_EmailQuotesPopup)]
            //},
            [CarrierSelect] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-email-carrier-scroller//app-carrier-selector//button[1]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(Interview_EmailQuotesPopup)]
            },
            #endregion

            #region Common Popup Fields
            [PopupFreeText] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-custom-textarea",
                FieldType = UIFieldType.Input,
                DefaultValue = "AutoTest " + RandomManager.GetRandomString(8),
                Pages = [typeof(Interview_OfflineRequestPopup)]
            },
            #endregion

            #region Payment Fields
            [CardNumber] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='cardNumber']",
                FieldType = UIFieldType.Input,
                DefaultValue = "4111111111111111",
                Pages = [typeof(Product_PayPage)],
                Required = true
            },
            [NameOnCard] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[@id='NameOnAccountDiv']//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "AutoTest TestLast",
                Pages = [typeof(Product_PayPage)],
                Required = true
            },
            [ExpirationDate] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[name='expDate']",
                FieldType = UIFieldType.Input,
                DefaultValue = "1228",
                Pages = [typeof(Product_PayPage)],
                Required = true
            },
            [SecurityCode] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[@id='cvcComp']//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "123",
                Pages = [typeof(Product_PayPage)],
                Required = true
            },
            [BillingZipCode] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[@id='CardHolderZipCodeDiv']//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "75217",
                Pages = [typeof(Product_PayPage)],
                Required = true
            },
            #endregion

            #region Motorcycle Page Fields
            [MotorcycleYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//span[text()='Year']/ancestor::app-form-label/following-sibling::app-vehicle-dropdown//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "2023",
                Pages = [typeof(Product_MotorcyclePage)],
                Required = true
            },
            [MotorcycleMake] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//span[text()='Make']//ancestor::app-form-label/following-sibling::app-vehicle-dropdown//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Suzuki",
                Pages = [typeof(Product_MotorcyclePage)],
                Required = true,
                DependsOn = PLYear
            },
            [MotorcycleModel] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//span[text()='Model']//ancestor::app-form-label/following-sibling::app-vehicle-dropdown//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "SV650 ABS",
                Pages = [typeof(Product_MotorcyclePage)],
                Required = true,
                DependsOn = PLMake
            },
            [MotorcycleActualCashValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[contains(@id,'MotorcycleACV')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "8000",
                Pages = [typeof(Product_MotorcyclePage)]
            },
            [MotorcycleEstimatedAnnualMileage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[contains(@id,'MotorcycleAnnualMileAge')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "5000",
                Pages = [typeof(Product_MotorcyclePage)]
            },
            [MotorcyclePrimaryUse] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'MotorcycleUse')]//input[@id='{0}']/..",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Pleasure",
                PreserveCasing = true,
                Pages = [typeof(Product_MotorcyclePage)]
            },
            [MotorcyclePurchaseDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-datepicker//input[contains(@name,'MotorcyclePurchaseDate')]/preceding-sibling::input",
                FieldType = UIFieldType.DatePicker,
                DefaultValue = "03/03/2025",
                Pages = [typeof(Product_MotorcyclePage)]
            },
            [MotorcycleCostNewValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[contains(@id,'CostNewValue')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "11000",
                Pages = [typeof(Product_MotorcyclePage)]
            },
            [MotorcycleStorageType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//ng-select[contains(@id,'MotorcycleStorageType')]",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Carport",
                Pages = [typeof(Product_MotorcyclePage)]
            },
            [MotorcycleIsStoredAtDifferentAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'StorageAddressDifferent')]//span[contains(., '{0}')]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "No",
                PreserveCasing = true,
                Pages = [typeof(Product_MotorcyclePage)]
            },
            #endregion

            #region Commercial Auto Page Fields
            [PriorCarrierAutoCL] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[@id = 'PolicyData.PriorCarrier_AutoCL']//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Allstate",
                Pages = [typeof(ProductInsuranceHistoryPageCL)]
            },
            [CurrentCarrierExpirationDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id = 'PolicyData.CurrentCarrierExpirationDate_Auto']//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(30).ToString("MM/dd/yyyy"),
                Pages = [typeof(ProductInsuranceHistoryPageCL)],
                DependsOn = PriorCarrierAutoCL
            },
            [YearsWithContinuousPersonalAutoCarrierCL] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.YearsWithContinuousCoverageAuto')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "5",
                Pages = [typeof(ProductInsuranceHistoryPageCL)],
                DependsOn = PriorCarrierAutoCL
            },
            [CLAutoPolicyInsured] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id='PolicyData.CLAutoPolicyInsured']//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "Individual(s)",
                PreserveCasing = true,
                Pages = [typeof(ProductApplicantsAndDriversPageCL)],
            },
            [OperatorFirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'OperatorFirstName')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetFirstName(),
                Pages = [typeof(ProductApplicantsAndDriversPageCL)],
                Required = true
            },
            [OperatorLastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'OperatorLastName')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetLastName(),
                Pages = [typeof(ProductApplicantsAndDriversPageCL)],
                Required = true
            },
            [OperatorDOB] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'OperatorDOB')]//input[contains(@class,'form-control')]",
                FieldType = UIFieldType.Input,
                DefaultValue = "01/01/1985",
                Pages = [typeof(ProductApplicantsAndDriversPageCL)],
                Required = true
            },
            [OperatorMaritalStatus] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[contains(@id , 'OperatorMaritalStatus')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Single",
                Pages = [typeof(ProductApplicantsAndDriversPageCL)]
            },
            [CLOperatorLicenseState] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[contains(@id , 'CLOperatorLicenseState')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Texas",
                Pages = [typeof(ProductApplicantsAndDriversPageCL)],
                Required = true
            },
            [TypeOfVehicle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[contains(@id , 'TypeOfVehicle')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Car - Luxury",
                Pages = [typeof(ProductCommercialVehiclesPageCL)],
                DependsOn = BodyStyle,
                // The vehicle question group re-renders after the Body Style lookup resolves the
                // Category, so this control is not in the DOM yet when the fill pass reaches it.
                InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
            },
            [PurchaseOrLeases] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[contains(@id , 'PurchaseOrLeases')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Purchase",
                Pages = [typeof(ProductCommercialVehiclesPageCL)]
            },
            [CL_LengthVehicleOwnership] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-control[contains(@id , 'CL_LengthVehicleOwnership')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "At least 3 years but less than 5 years",
                Pages = [typeof(ProductCommercialVehiclesPageCL)]
            },
            [AnnualMileageCL] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'RadiusCustomDropdown')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "101-200 miles",
                Pages = [typeof(ProductCommercialVehiclesPageCL)],
            },
            [CL_RentalDowntime] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_RentalDowntime')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(ProductCommercialAutoCoveragesPageCL)],
                PreserveCasing = true,
                Required = true
            },
            [PrimaryUseOfVehicle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.PrimaryUseOfVehicle') or (contains(@class,'PolicyDataVehicles') and contains(@class,'PrimaryUseOfVehicleCustomDropdown'))]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Personal Only",
                Pages = [typeof(ProductCommercialVehiclesPageCL)],
                // "Use" only offers its options once the Vehicle type is set - without this ordering
                // hint the fill pass reaches it first and the selection does not stick.
                DependsOn = TypeOfVehicle,
                InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
            },
            #endregion

            #region KLX Old Interview - CL Auto Vehicle Page extras
            [VehicleAverageDailyTrips] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'AverageDailyNumberUnmaskedInput')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1",
                Pages = [typeof(Product_VehiclePage), typeof(ProductCommercialVehiclesPageCL)],
                Required = true
            },
            [VehicleGaragedDifferentAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'GarageAddressDifferent')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_VehiclePage)],
                PreserveCasing = true,
                Required = true
            },
            [VehicleHaulsGoodsForHire] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyDataVehicles') and contains(@class,'HaulGoods')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_VehiclePage)],
                PreserveCasing = true,
                Required = true
            },
            #endregion

            #region KLX Old Interview - CL Auto Business Page (Yes/No radios)
            [OwnerInvolvedInDailyOperations] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.OwnerInvolvedDailyOperations')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                Pages = [typeof(Product_BusinessPage), typeof(ProductBusinessProfilePageCL) ,typeof(ProductAdditionalQuestionsProgressivePageCL)],
                PreserveCasing = true,
                Required = true
            },
            [TowingOrHauling] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.TowingOrHauling')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                Pages = [typeof(Product_BusinessPage), typeof(ProductBusinessProfilePageCL)],
                PreserveCasing = true,
                Required = true
            },
            #endregion

            #region KLX Old Interview - CL Auto Start Page (Business Contact step)
            [Certification] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CLCertifyFarmersDeclined')]//label[contains(@class,'checkbox-wrapper')]",
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Pages = [typeof(Product_StartPage), typeof(ProductBusinessProfilePageCL)],
            },
            [DeclinationReason] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CL_IneligibleReason')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Class/SIC code not available with Farmers",
                Pages =  [typeof(Product_StartPage), typeof(ProductBusinessProfilePageCL)],
                PreserveCasing = true,
                DependsOn = Certification,
                DependsOnValue = "true",
                InteractionOptions = new ElementInteractionOptions { Timeout = 30000 }
            },
            #endregion
            #region KLX Old Interview - PL Home Start Page
            // PL Start page's own "Declination Reason" ng-select - a different schema field
            // (PolicyData.DeclinationReason) than KLX CL's (PolicyData.CL_IneligibleReason),
            // so it needs its own registry key rather than reusing DeclinationReason above.
            [PLDeclinationReason] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.DeclinationReason')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Farmer's Decline/Out of Appetite",
                Pages = [typeof(Product_StartPage)],
                PreserveCasing = true,
                InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
            },
            // Answering PLDeclinationReason reveals this required follow-up ng-select
            // ("Please provide a declination or ineligible reason").
            [PLIneligibleReason] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.IneligibleReason')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Building Condition",
                Pages = [typeof(Product_StartPage)],
                PreserveCasing = true,
                DependsOn = PLDeclinationReason,
                InteractionOptions = new ElementInteractionOptions { Timeout = 15000 }
            },
            #endregion

            #region KLX Old Interview - CL Auto Policy Page
            // Top-level "Combined Uninsured and Underinsured Motorist" - ng-select.
            [CombinedUninsuredUnderinsuredMotorist] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.CL_Combined_UM_UIM')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$100,000/$300,000",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },
            // Top-level "Uninsured Motorist Property Damage" - searchable ng-select.
            [UninsuredMotoristPropertyDamage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//*[contains(@class,'PolicyData.CL_UMPD')]//ng-select", "//*[contains(@class,'CL_UMPD')]//ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "25,000",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)],
                PreserveCasing = true
            },

            [CL_FullGlass] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_FullGlass')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)],
                PreserveCasing = true,
                Required = true
            },
            [CL_RoadSide] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_RoadSide')]//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)],
                PreserveCasing = true,
                Required = true
            },
            [CL_ComprehensiveDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_Comprehensive')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "500",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },
            [CL_CollisionDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_Collision')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "500",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },
            [CL_CurrentVehicleValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_StatedAmount')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "20000",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialVehiclesPageCL)],
                Required = true
            },
            // Per-vehicle "Fire & Theft with CAC" (Combined Additional Coverage) dropdown.
            [CL_FireTheftCAC] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'CL_FireTheftCAC')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "No Coverage",
                Pages = [typeof(Product_CLPolicyPage), typeof(ProductCommercialAutoCoveragesPageCL)]
            },
            // Top-level "Personal Injury Protection (PIP)" dropdown - only rendered for no-fault-state
            // risk addresses. Id-anchored (not class-contains) because its id is a literal prefix of
            // CL_PIPStacking's id and a contains() match would hit both controls.
            [CL_PIP] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id='PolicyData.CL_PIP']//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "$20,000 Medical Expense/$20,000 Economic Loss",
                Pages = [typeof(ProductCommercialAutoCoveragesPageCL)],
                Required = true
            },
            [CL_PIPStacking] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id='PolicyData.CL_PIPStacking']//label[contains(@class,'switcher-wrapper') and .//span[contains(normalize-space(.),'{0}')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                Pages = [typeof(ProductCommercialAutoCoveragesPageCL)],
                PreserveCasing = true,
                Required = true
            },
            #endregion

            #region CL Additional Questions - Travelers BOP Page Fields
            [TravelersBuildingReplacementCost] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-control[class*='PolicyDataLocationsidBuildingsidReplacementCost'] input",
                FieldType = UIFieldType.Input,
                DefaultValue = "250000",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
            },
            [TravelersBuildingYearConstructed] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-control[class*='PolicyDataLocationsidBuildingsidYearOriginalConstruction'] input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2022",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
            },
            [TravelersRoofYearRenovated] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-control[class*='PolicyDataLocationsidBuildingsidYearRoof'] input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2022",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
            },
            [TravelersTotalBuildingSquareFootage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-control[class*='PolicyDataLocationsidBuildingsidSquareFootage']:not([class*='PolicyDataLocationsidBuildingsidSquareFootageOccupied']) input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1450",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
            },
            [TravelersBusinessSquareFootage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-control[class*='PolicyDataLocationsidBuildingsidSquareFootageOccupied'] input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1300",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
            },
            [EmployeeWorkplaceSafetyProgram] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id='PolicyData.EmployeeWorkplaceSafetyProgram']//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "Yes",
                PreserveCasing = true,
                Pages = [typeof(ProductAdditionalQuestionsLibertyMutualPageCL)],
            },
            [HasSafetyProgramBeenCertified] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[@id='PolicyData.HasThisSafetyProgramBeenCertified']//label[contains(.,'{0}')]/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "No",
                PreserveCasing = true,
                InteractionOptions = new ElementInteractionOptions { Timeout = 5000 },
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL), typeof(ProductAdditionalQuestionsLibertyMutualPageCL)],
            },
            [MarketAppetitePanel] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-uud-info//h4[contains(normalize-space(.),'Market appetite')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = []
            },
            [NumberOfVehicles] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//*[contains(@class,'PolicyData.NumberOfVehicles')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "1",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
            },
            [TravelersNumberOfStories] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-control[class*='PolicyDataLocationsidBuildingsidNumberOfStories'] input",
                FieldType = UIFieldType.Input,
                DefaultValue = "2",
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
                Required = true
            },
            [TravelersConstructionType] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-select[id*='PolicyDataLocationsidBuildingsidConstructionType']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Frame",
                PreserveCasing = true,
                Pages = [typeof(ProductAdditionalQuestionsTravelersPageCL)],
                Required = true
            },
            #endregion

            #region buttons
            [OfflineRequestButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "button.Offline", "button.offline.request" },
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = []
            },
            [PaperApplicationCard] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-market h5.market-title:has-text(\"Do you already have a paper application?\")",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = []
            },
            #endregion
        };

        static FieldRegistryInterview() => FieldRegistryProvider.Register(FrontEndType.Interview, Fields);

    }
}
