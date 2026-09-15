using Bolt.Automation.Common.Models.TestData.Data;

namespace Bolt.Automation.Common.Models.TestData.Interview.Models
{
    public class HomeDetails
    {
        public bool IsMailAddress { get; set; }
        public int ActualCashValue { get; set; }
        public int PLNumberOfUnits { get; set; }
        public int PL_NumberOfFloors { get; set; }
        public int PLFloorNumber { get; set; }
        public string? RoofUpdatedYear { get; set; }
        public string? PLRoofUpdated { get; set; }
        public int PLOtherStructures { get; set; }
        public int NumberOfMortgagees { get; set; }
        public int MonthsAtAddress { get; set; }
        public string? BasementType { get; set; }
        public int YearsAtAddress { get; set; }
        public int PL_NumOfFamilies { get; set; }
        public string? ConstructionQuality { get; set; }
        public string? MitRoofShape { get; set; }
        public string? RoofType { get; set; }
        public int PLPersonalProperty { get; set; }
        public string? PLNumberOfStories { get; set; }
        public string? PLConstructionType { get; set; }
        public int PLYearBuilt { get; set; }
        public int PLSquareFootage { get; set; }
        public string? ArchitectureStyle { get; set; }
        public string? PLHomeLength { get; set; }
        public string? PLHomeWidth { get; set; }
        public string? PLTypeOfDwelling { get; set; }
        public string? PLOccupiedOrPurchase { get; set; }
        public string? PurchaseDate { get; set; }
        public int? PurchasePrice { get; set; }
        public string? OccupancyType { get; set; }
        public string? ReasonVacant { get; set; }
        public string? OriginalOwner { get; set; }
        public string? DwellingForSale { get; set; }
        public bool HomeUnderConstruction { get; set; }
        public string? HomeRenovation { get; set; }
        public string? RenovationCost { get; set; }
        public string? RenovationOutOfProperty { get; set; }
        public string? NumberConsecMonths { get; set; }
        public string? UnitRentedToOthers { get; set; }
        public bool ShortTermRental { get; set; }
        public string? MonthsUnoccupied { get; set; }
        public int PL_Houseoccup { get; set; }
        public int NumberOfChildernsUnder18 { get; set; }
        public bool NonSmoker { get; set; }
        public int NumberOfDogsOnPremises { get; set; }
        public string? RestrictedDogs { get; set; }
        public bool BusinessOrDaycare { get; set; }
        public string? PrimaryHome { get; set; }
        public string? IsHomeUnderMajorRenovation { get; set; }
        public bool? ExistingDamageOnDwelling { get; set; }
        public string? SinkholeInvestigationOrClaim { get; set; }
        public string? NumOfDwelling { get; set; }
        public string? AnyResidentialEmployees { get; set; }
        public string? NumInServant { get; set; }
        public string? NumHoursWorked { get; set; }
        public string? DistanceToFireHydrant { get; set; }
        public string? DistanceToFireStation { get; set; }
        public string? DistanceToCoastRange { get; set; }
        public bool InsideCityLimits { get; set; }
        public string? InCitySuburbDistrict { get; set; }
        public bool HomeVisibleToNeighbors { get; set; }
        public bool GatedOrLimited { get; set; }
        public string? DistanceToCoast { get; set; }
        public string? DistanceToCoastFeet { get; set; }
        public string? LimitedAccessCommunity { get; set; }
        public string? Guarded24Hours { get; set; }
        public string? InsuranceDeclined { get; set; }
        public int HalfBathNum { get; set; }
        public int FullBathNum { get; set; }
        public string? TypeOfFoundation { get; set; }

        ///
        public bool? AnimalsOnThePremises_Farm3orMore { get; set; }
        public bool? AnimalsOnThePremises_Farm1to2 { get; set; }
        public bool? AnimalsOnThePremises_Dogs { get; set; }
        public bool? AnimalsOnThePremises_None { get; set; }
        public bool? AnimalsOnThePremises_Exotic { get; set; }
        public List<string>? DogsBreedsSelection { get; set; }
        public bool? BusinessOnResidencePremises { get; set; }
        public bool? RoofResponsible { get; set; }
        public bool? BuiltOnSlope { get; set; }
        public int? NumberofAcres { get; set; }
        public string? PerimeterSecurityDD { get; set; }
        public string? DateOccupied { get; set; }
        public string? PL_LivingTime { get; set; }
        public string? PL_RoofUpdateYearRange { get; set; }
        public int? NumberOfClaimsHistory { get; set; }
        public bool? IsForeignAddress { get; set; }
        public bool? ResHeldTrust { get; set; }
        public int? HeatingUpdateYear { get; set; }
        public int? ElectricalUpdatedYear { get; set; }
        public string? PlumbingType { get; set; }
        public int? PlumbingUpdatedYear { get; set; }
        public string? TypeGarageCarport { get; set; }
        public string? ExteriorWallsConstruction { get; set; }
        public bool? EligibilityFinancialHardship { get; set; }

        public Address? PreviousAddress { get; set; }

        public string? MHArchitectureStyle { get; set; }//manufactured home
        public string? ModularHome { get; set; }//manufactured home
        public int? HomeLength { get; set; }//manufactured home
        public int? HomeWidth { get; set; }//manufactured home
        public string? HomeTiedDown { get; set; }//manufactured home
        public string? IsLocatedInPark { get; set; }//manufactured home
        public string? UtilityHookup365 { get; set; }//manufactured home
        public bool? UnderMajorRenovation { get; set; }
        public bool? UndergroundFuelTank { get; set; }
    }
}
