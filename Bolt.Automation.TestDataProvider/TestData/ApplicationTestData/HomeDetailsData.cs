using Bolt.Automation.Common.Models.TestData.Interview.Models;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class HomeDetailsTestData
        {
            public static HomeDetails PersonalHomeDetailsData
            {
                get
                {
                    var details = GetBaseHomeDetails();
                    return details;
                }
            }

            public static HomeDetails CondominiumDetailsData
            {
                get
                {
                    var details = GetBaseHomeDetails();
                    details.PLTypeOfDwelling = "Condominium";
                    details.ConstructionQuality = "Average";
                    details.PLFloorNumber = 4;
                    details.PL_Houseoccup = 3;
                    details.PLNumberOfUnits = 2;
                    details.PL_NumberOfFloors = 4;
                    details.MitRoofShape = "C_Hip";
                    return details;
                }
            }

            public static HomeDetails PGRHomeDetails
            {
                get
                {
                    var details = GetBaseHomeDetails();
                    details.NumberOfDogsOnPremises = 1;
                    details.DogsBreedsSelection = new List<string>();
                    details.NumberofAcres = null;// provider overrides with 0.18m
                    details.PerimeterSecurityDD = "No";
                    details.TypeGarageCarport = "Attached";
                    details.ExteriorWallsConstruction = "WoodFraming";
                    return details;
                }
            }

            public static HomeDetails PGRCondoDetails
            {
                get
                {
                    var details = GetBaseHomeDetails();
                    details.PLTypeOfDwelling = "Condominium";
                    details.PLFloorNumber = 2;
                    details.PL_NumberOfFloors = 2;
                    return details;
                }

            }

            public static HomeDetails PGRDFDetails
            {
                get
                {
                    var details = GetBaseHomeDetails();
                    details.PL_LivingTime = "Five";
                    details.PL_NumberOfFloors = 2;
                    details.OccupancyType = "Vacant";
                    return details;
                }

            }

            public static HomeDetails PGRMHDetails
            {
                get
                {
                    var details = GetBaseHomeDetails();
                    details.PLTypeOfDwelling = "ManufacturedHome";
                    details.ActualCashValue = 113000;
                    details.PurchasePrice = 113000;
                    details.MonthsAtAddress = 0;
                    details.ModularHome = "false";
                    details.MHArchitectureStyle = "SingleWide";
                    details.HomeLength = 13;
                    details.HomeWidth = 13;
                    details.HomeTiedDown = "true";
                    details.IsLocatedInPark = "false";
                    details.UtilityHookup365 = "true";
                    return details;
                }
            }

            private static HomeDetails GetBaseHomeDetails()
            {
                return new HomeDetails
                {
                    IsMailAddress = false,
                    PLConstructionType = "Frame",
                    PLTypeOfDwelling = "SingleFamilyHouse",
                    OccupancyType = "OwnerPrimary",
                    DistanceToFireHydrant = "_0_500ft",
                    DistanceToFireStation = "Lessthan5miles",
                    GatedOrLimited = false,
                    HomeUnderConstruction = false,
                    HomeVisibleToNeighbors = true,
                    InsideCityLimits = false,
                    MonthsAtAddress = 0,
                    NonSmoker = false,
                    NumberOfDogsOnPremises = 0,
                    PLRoofUpdated = "CompleteUpdate",
                    RoofUpdatedYear = "2024",
                    RoofType = "ARCHITECTURAL_SHINGLES",
                    PL_Houseoccup = 1,
                    PL_NumOfFamilies = 1,
                    YearsAtAddress = 3,
                    PLYearBuilt = 2022,
                    PLPersonalProperty = 25000,
                    BusinessOrDaycare = false,
                    PLNumberOfStories = "One",
                    PLOtherStructures = 0,
                    PLSquareFootage = 2172,
                    ActualCashValue = 515000,
                    PurchaseDate = "2023-10-09",
                    NumberOfChildernsUnder18 = 0,
                    ShortTermRental = false,
                    BusinessOnResidencePremises = false,
                    PurchasePrice = 694000,
                    TypeOfFoundation = "Slab",
                    ArchitectureStyle = "Contemporary",
                    IsForeignAddress = false,
                    NumberOfClaimsHistory = 0,
                    NumberOfMortgagees = 0,
                    ResHeldTrust = false,
                    RoofResponsible = false,
                    PLOccupiedOrPurchase = "LiveThereNow",
                    MitRoofShape = "B_Gable",
                    PrimaryHome = "true",
                    FullBathNum = 2,
                    HalfBathNum = 1,
                    BuiltOnSlope = false,
                    ExistingDamageOnDwelling = false,
                    DateOccupied = "2024-01-01",
                    PL_LivingTime = "NineToTwelve",
                    PL_RoofUpdateYearRange = "ZeroToFour",
                    PlumbingType = "EntirelyCopper",
                    HeatingUpdateYear = 2024,
                    ElectricalUpdatedYear = 2024,
                    EligibilityFinancialHardship = false,
                    AnimalsOnThePremises_None = false,
                    AnimalsOnThePremises_Dogs = false,
                    AnimalsOnThePremises_Farm3orMore = false,
                    AnimalsOnThePremises_Farm1to2 = false,
                    AnimalsOnThePremises_Exotic = false,
                    PLFloorNumber = 1,
                    UnderMajorRenovation = false,
                    UndergroundFuelTank = false,
                };
            }
        }
    }
}
