
using Bolt.Automation.Common.Models.TestData.Interview.Models;

namespace Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine
{
    public partial class PersonalLineData : BaseLineData
    {
        public int? PLYearBuilt { get; set; }
        public string? PLTypeOfDwelling { get; set; }
        public string? OccupancyType { get; set; }
        public string? PLConstructionType { get; set; }
        public string? PLNumberOfStories { get; set; }
        public int? PLPersonalProperty { get; set; }
        public int? PersonalLineReplacementCost { get; set; }
        public int? LossOfUse { get; set; }
        public bool? DeadBoltLocks { get; set; }
        public bool? SprinklerSystem { get; set; }
        public bool? BurglarAlarm { get; set; }
        public string? BurglarAlarmType { get; set; }
        public bool? FireDetection { get; set; }
        public bool? FireExtinguisher { get; set; }
        public bool? SmokeDetector { get; set; }
        public bool? ElectricCircuitBreaker { get; set; }
        public bool? BusinessOrDaycare { get; set; }
        public int? NumberOfDogsOnPremises { get; set; }
        public bool? InsideCityLimits { get; set; }
        public bool? PL_AdditionalStructures_Pool { get; set; }
        public bool? PL_AdditionalStructures_Garage { get; set; }
        public string? PLPersonalLiability { get; set; }
        public string? PLAllPerilsDeductible { get; set; }
        public string? DwellingMedicalPayments { get; set; }
        public string? PriorCarrierExpirationDate { get; set; }
        public string? PriorLiabilityCoverageHome { get; set; }
        public bool? PropertyInsuranceCancelled { get; set; }
        public int? YearsWithPriorCarrierHome { get; set; }
        public string? DistanceToFireHydrant { get; set; }
        public string? DistanceToFireStation { get; set; }
        public string? ExteriorWallsConstruction { get; set; }
        public string? TypeOfFoundation { get; set; }
        public string? RoofType { get; set; }
        public string? MitRoofShape { get; set; }
        public string? ArchitectureStyle { get; set; }
        public string? ConstructionQuality { get; set; }
        public int? PL_Houseoccup { get; set; }
        public int? PL_NumOfFamilies { get; set; }
        public int? YearsAtAddress { get; set; }
        public bool? CreditCheckPermission { get; set; }
        public bool? AnyAdditionalInsured { get; set; }
        public string? PLHaveAnyLosses { get; set; }
        public bool? IAgreeToReceiveEmailsByBolt { get; set; }
        public bool? IHerebyConfirm { get; set; }
        public string? SmokeDetectorType { get; set; }
        public int? YearsWithContinuousCoverageHome { get; set; }
        public bool? ElectricalUpdateYN { get; set; }
        public bool? GatedOrLimited { get; set; }
        public bool? HeatingUpdateYN { get; set; }
        public bool? HomeAutoInsurance { get; set; }
        public bool? HomeUnderConstruction { get; set; }
        public bool? HomeVisibleToNeighbors { get; set; }
        public int? MonthsAtAddress { get; set; }
        public bool? NonSmoker { get; set; }
        public string? PLRoofUpdated { get; set; }
        public bool? PlumbingUpdateYN { get; set; }
        public int NumberOfMortgagees { get; set; }
        public string? BasementType { get; set; }
        public string? PLHeatingType { get; set; }
        public int? PLSquareFootage { get; set; }
        public bool? PL_AdditionalStructures_Trampoline { get; set; }
        public string? PlumbingType { get; set; }
        public int? PlumbingUpdatedYear { get; set; }
        public List<string>? PL_FlooringMaterial { get; set; }
        public string? PL_PrimaryCounterMaterial { get; set; }
        public List<string>? PL_InteriorWallMaterial { get; set; }
        public int? PL_VaultedCeilings { get; set; }
        public int? PL_CrownMolding { get; set; }
        public string? PL_CeilingHeight { get; set; }
        public bool? PL_CentralAC { get; set; }
        public bool? PL_HeatedByOil { get; set; }
        public int? NumberOfFirePlaces { get; set; }
        public string? PL_TypeFireplaces { get; set; }
        public int? ElectricalUpdatedYear { get; set; }
        public string? PLElectricalUpdated { get; set; }
        public string? MitWindowOpening { get; set; }
        public string? MitRoofCover { get; set; }
        public string? MitRoofDeck { get; set; }
        public string? MitRoofWall { get; set; }
        // home details additions
        public bool? AnimalsOnThePremises_Farm3orMore { get; set; }
        public bool? AnimalsOnThePremises_Farm1to2 { get; set; }
        public bool? AnimalsOnThePremises_Dogs { get; set; }
        public bool? AnimalsOnThePremises_None { get; set; }
        public bool? AnimalsOnThePremises_Exotic { get; set; }
        public bool? ViciousExoticAnimals { get; set; }
        public bool? MitCreditForm { get; set; }
        public List<string>? DogsBreedsSelection { get; set; }
        public bool? BusinessOnResidencePremises { get; set; }
        public bool? RoofResponsible { get; set; }
        public bool? BuiltOnSlope { get; set; }
        public decimal? NumberofAcres { get; set; }
        public string? PerimeterSecurityDD { get; set; }
        public int? FullBathNum { get; set; }
        public int? HalfBathNum { get; set; }
        public bool? ResHeldTrust { get; set; }

        // Newly added properties to match API response / request JSON
        public List<string>? Lobs { get; set; }
        public int? NumberOfChildren { get; set; }
        public int? Progressive_Preferences1 { get; set; }
        public int? Progressive_Preferences2 { get; set; }
        public string? PLDfForm { get; set; }
        public string? PLHeatingUpdate { get; set; }
        public bool? ExistingDamageOnDwelling { get; set; }

        //condo
        public int? PL_NumberOfFloors { get; set; }
        public int? PLFloorNumber { get; set; }

        public string? RoofUpdatedYear { get; set; }
        public string? PLPlumbingUpdated { get; set; }
        public string? PurchaseDate { get; set; }
        public int? PurchasePrice { get; set; }
        public bool? ShortTermRental { get; set; }
        public bool? SinkholeInvestigationOrClaim { get; set; }
        public bool? UnderMajorRenovation { get; set; }
        public bool? UndergroundFuelTank { get; set; }
        public bool? PL_AdditionalStructures_None { get; set; }
        public bool? PL_AdditionalStructures_HotTub { get; set; }
        public bool? PL_AdditionalStructures_Deck { get; set; }
        public int? PL_NumberCarSpace { get; set; }
        public bool? IsForeignAddress { get; set; }
        public int? MarketValue { get; set; }
        public int? ActualCashValue { get; set; }
        public bool? PoliciesWithAgent { get; set; }
        public bool? PrimaryHome { get; set; }
        public string? PL_OccupiedOrPurchase { get; set; }
        public string? FireDetectionType { get; set; }
        public int? NumberOfChildernsUnder18 { get; set; }
        public string? TypeGarageCarport { get; set; }
        public int? HeatingUpdateYear { get; set; }
        public string? DateOccupied { get; set; }
        public string? PL_LivingTime { get; set; }
        public string? PL_RoofUpdateYearRange { get; set; }
        public int? NumberOfClaimsHistory { get; set; }
        public List<string>? BurglarAlarmTypeMulti { get; set; }
        public List<string>? FireDetectionTypeMulti { get; set; }
        public string? MHArchitectureStyle { get; set; }//manufactured home
        public string? ModularHome { get; set; }//manufactured home
        public int? HomeLength { get; set; }//manufactured home
        public int? HomeWidth { get; set; }//manufactured home
        public string? HomeTiedDown { get; set; }//manufactured home
        public string? IsLocatedInPark { get; set; }//manufactured home
        public string? UtilityHookup365 { get; set; }//manufactured home
        public CustomFieldsModel? CustomFields { get; set; }
        public string? InCitySuburbDistrict { get; set; }
        public string? DistanceToCoast { get; set; }
        public string? NumOfDwelling { get; set; }
    }
}
