using Bolt.Automation.Common.Models.TestData.Data;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.Models.TestData.Interview.Models;
using Bolt.Automation.Common.Models.TestData.PersonalAutoModels;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;

namespace Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider
{
    public static class PersonalLineDataMapper
    {
        public static void MapAddress(PersonalLineData data, Address? address)
        {
            CommonDataMapper.MapAddress(data, address);
            data.PreviousAddress = AddressData.CT;
        }

        public static void MapPersonalInfo(PersonalLineData data, PersonalInfoModel personalInfo)
        {
            CommonDataMapper.MapPersonalInfo(data, personalInfo);
        }

        public static void MapHomeDetails(PersonalLineData data, HomeDetails homeDetails)
        {
            data.IsMailAddress = homeDetails.IsMailAddress;
            data.PLSquareFootage = homeDetails.PLSquareFootage;
            data.RoofUpdatedYear = homeDetails.RoofUpdatedYear;
            data.PLConstructionType = homeDetails.PLConstructionType;
            data.PLTypeOfDwelling = homeDetails.PLTypeOfDwelling;
            data.OccupancyType = homeDetails.OccupancyType;
            data.DistanceToFireHydrant = homeDetails.DistanceToFireHydrant;
            data.DistanceToFireStation = homeDetails.DistanceToFireStation;
            data.BusinessOrDaycare = homeDetails.BusinessOrDaycare;           
            data.GatedOrLimited = homeDetails.GatedOrLimited;
            data.HomeUnderConstruction = homeDetails.HomeUnderConstruction;
            data.HomeVisibleToNeighbors = homeDetails.HomeVisibleToNeighbors;
            data.InsideCityLimits = homeDetails.InsideCityLimits;
            data.MonthsAtAddress = homeDetails.MonthsAtAddress;
            data.NonSmoker = homeDetails.NonSmoker;
            data.NumberOfDogsOnPremises = homeDetails.NumberOfDogsOnPremises;
            data.PLRoofUpdated = homeDetails.PLRoofUpdated;
            data.PL_Houseoccup = homeDetails.PL_Houseoccup;
            data.PL_NumOfFamilies = homeDetails.PL_NumOfFamilies;
            data.PL_NumberOfFloors= homeDetails.PL_NumberOfFloors;
            data.PLFloorNumber = homeDetails.PLFloorNumber;
            data.YearsAtAddress = homeDetails.YearsAtAddress;
            data.PLYearBuilt = homeDetails.PLYearBuilt;
            data.PLPersonalProperty = homeDetails.PLPersonalProperty;
            data.ActualCashValue = homeDetails.ActualCashValue;
            data.PurchaseDate = homeDetails.PurchaseDate;
            data.PurchasePrice = homeDetails.PurchasePrice;
            data.TypeOfFoundation = homeDetails.TypeOfFoundation;
            data.ArchitectureStyle = homeDetails.ArchitectureStyle;
            data.RoofType = homeDetails.RoofType;
            data.MitRoofShape = homeDetails.MitRoofShape;
            data.RoofUpdatedYear = homeDetails.RoofUpdatedYear;
            data.ExistingDamageOnDwelling = homeDetails.ExistingDamageOnDwelling;
            data.BuiltOnSlope = homeDetails.BuiltOnSlope;
            data.ShortTermRental = homeDetails.ShortTermRental;
            data.NumberofAcres = homeDetails.NumberofAcres;
            data.PerimeterSecurityDD = homeDetails.PerimeterSecurityDD;
            data.AnimalsOnThePremises_Farm3orMore = homeDetails.AnimalsOnThePremises_Farm3orMore;
            data.AnimalsOnThePremises_Farm1to2 = homeDetails.AnimalsOnThePremises_Farm1to2;
            data.AnimalsOnThePremises_Dogs = homeDetails.AnimalsOnThePremises_Dogs;
            data.AnimalsOnThePremises_None = homeDetails.AnimalsOnThePremises_None;
            data.AnimalsOnThePremises_Exotic = homeDetails.AnimalsOnThePremises_Exotic;
            data.DogsBreedsSelection = homeDetails.DogsBreedsSelection;
            data.BusinessOnResidencePremises = homeDetails.BusinessOnResidencePremises;
            data.RoofResponsible = homeDetails.RoofResponsible;
            data.NumberOfMortgagees = homeDetails.NumberOfMortgagees;
            data.PLOtherStructures = homeDetails.PLOtherStructures;
            data.FullBathNum = homeDetails.FullBathNum;
            data.HalfBathNum = homeDetails.HalfBathNum;
            data.DateOccupied = homeDetails.DateOccupied;
            data.PL_LivingTime = homeDetails.PL_LivingTime;
            data.PL_RoofUpdateYearRange = homeDetails.PL_RoofUpdateYearRange;
            data.NumberOfClaimsHistory = homeDetails.NumberOfClaimsHistory;
            data.TypeGarageCarport = homeDetails.TypeGarageCarport;
            data.HeatingUpdateYear = homeDetails.HeatingUpdateYear;
            data.ElectricalUpdatedYear = homeDetails.ElectricalUpdatedYear;
            data.PlumbingType = homeDetails.PlumbingType;
            data.PlumbingUpdatedYear = homeDetails.PlumbingUpdatedYear;
            data.ResHeldTrust = homeDetails.ResHeldTrust;
            data.IsForeignAddress = homeDetails.IsForeignAddress;
            data.PL_OccupiedOrPurchase = homeDetails.PLOccupiedOrPurchase;
            data.ModularHome = homeDetails.ModularHome;
            data.MHArchitectureStyle = homeDetails.MHArchitectureStyle;
            data.HomeLength = homeDetails.HomeLength;
            data.HomeWidth = homeDetails.HomeWidth;
            data.HomeTiedDown = homeDetails.HomeTiedDown;
            data.IsLocatedInPark = homeDetails.IsLocatedInPark;
            data.UtilityHookup365 = homeDetails.UtilityHookup365;
            data.DistanceToCoast = homeDetails.DistanceToCoast;
            data.InCitySuburbDistrict = homeDetails.InCitySuburbDistrict;
            data.NumOfDwelling = homeDetails.NumOfDwelling;
        }

        public static void MapHomeFeatures(PersonalLineData data, HomeFeaturesDetails homeFeatures)
        {
            data.BurglarAlarm = homeFeatures.BurglarAlarm;
            data.DeadBoltLocks = homeFeatures.DeadBoltLocks;
            data.BurglarAlarmType = homeFeatures.BurglarAlarmType;
            data.SmokeDetectorType = homeFeatures.SmokeDetectorType;
            data.ElectricCircuitBreaker = homeFeatures.ElectricCircuitBreaker;
            data.ElectricalUpdateYN = homeFeatures.ElectricalUpdateYN;
            data.FireDetection = homeFeatures.FireDetection;
            data.FireExtinguisher = homeFeatures.FireExtinguisher;
            data.HeatingUpdateYN = homeFeatures.HeatingUpdateYN;
            data.PL_AdditionalStructures_Pool = homeFeatures.PL_AdditionalStructures_Pool;
            data.PL_AdditionalStructures_Garage = homeFeatures.PL_AdditionalStructures_Garage;
            data.PlumbingUpdateYN = homeFeatures.PlumbingUpdateYN;
            data.SmokeDetector = homeFeatures.SmokeDetector;
            data.SprinklerSystem = homeFeatures.SprinklerSystem;
            data.PL_FlooringMaterial = homeFeatures.PL_FlooringMaterial;
            data.PL_PrimaryCounterMaterial = homeFeatures.PL_PrimaryCounterMaterial;
            data.PL_InteriorWallMaterial = homeFeatures.PL_InteriorWallMaterial;
            data.PL_VaultedCeilings = homeFeatures.PL_VaultedCeilings;
            data.PL_CrownMolding = homeFeatures.PL_CrownMolding;
            data.PL_CeilingHeight = homeFeatures.PL_CeilingHeight;
            data.PL_CentralAC = homeFeatures.PL_CentralAC;
            data.PL_HeatedByOil = homeFeatures.PL_HeatedByOil;
            data.NumberOfFirePlaces = homeFeatures.NumberOfFirePlaces;
            data.PL_TypeFireplaces = homeFeatures.PL_TypeFireplaces;
            data.PLElectricalUpdated = homeFeatures.PLElectricalUpdated;
            data.BurglarAlarmTypeMulti = homeFeatures.BurglarAlarmTypeMulti;
            data.FireDetectionTypeMulti = homeFeatures.FireDetectionTypeMulti;
            data.PL_AdditionalStructures_HotTub = homeFeatures.PL_AdditionalStructures_HotTub;
            data.PL_AdditionalStructures_Deck = homeFeatures.PL_AdditionalStructures_Deck;
            data.PL_AdditionalStructures_Trampoline = homeFeatures.PL_AdditionalStructures_Trampoline;
            data.PL_NumberCarSpace = homeFeatures.PL_NumberCarSpace;
            data.PLHeatingType = homeFeatures.PLHeatingType;
            data.MitRoofWall = homeFeatures.MitRoofWall;
            data.MitRoofDeck = homeFeatures.MitRoofDeck;
            data.MitRoofCover =homeFeatures.MitRoofCover;
            data.MitWindowOpening = homeFeatures.MitWindowOpening;
            data.ViciousExoticAnimals = homeFeatures.ViciousExoticAnimals;
            data.MitCreditForm = homeFeatures.MitCreditForm;
        }

        public static void MapPolicyDetails(PersonalLineData data, PolicyDetails policyDetails)
        {
            data.IAgreeToReceiveEmailsByBolt = policyDetails.IAgreeToReceiveEmailsByBolt;
            data.IHerebyConfirm = policyDetails.IHerebyConfirm;
            data.PriorCarrierExperationDate = policyDetails.PriorCarrierExpirationDate;
            data.PLHaveAnyLosses = policyDetails.PLHaveAnyLosses;
            data.CreditCheckPermission = policyDetails.CreditCheckPermission;
            data.AnyAdditionalInsured = policyDetails.AnyAdditionalInsured;
            data.CurrentPersonalHomeownerCarrier = policyDetails.CurrentPersonalHomeownerCarrier;
            data.DwellingMedicalPayments = policyDetails.DwellingMedicalPayments;
            data.EffectiveDate = policyDetails.EffectiveDate;
            data.LapseInCoverage = policyDetails.LapseInCoverage;
            data.DiscloseHomeCurrentPremium = policyDetails.DiscloseHomeCurrentPremium;
            data.ForeclosureOrRepossessionOrBankruptcy= policyDetails.ForeclosureOrRepossessionOrBankruptcy;
            data.YearsWithContinuousCoverageHome = policyDetails.YearsWithContinuousCoverageHome;
            data.YearsWithPriorCarrierHome = policyDetails.YearsWithPriorCarrierHome;
            data.HomeAutoInsurance = policyDetails.HomeAutoInsurance;
            data.PLAllPerilsDeductible = policyDetails.PLAllPerilsDeductible;
            data.PLPersonalLiability = policyDetails.PLPersonalLiability;
            data.PropertyInsuranceCancelled = policyDetails.PropertyInsuranceCancelled;
            data.PersonalLineReplacementCost = policyDetails.PersonalLineReplacementCost;
            data.LossOfUse = policyDetails.LossOfUse;
            data.PL_Bankruptcy = policyDetails.PL_Bankruptcy;
            data.PL_foreclosure = policyDetails.PL_foreclosure;
            data.EligibilityFinancialHardship = policyDetails.EligibilityFinancialHardship;
            data.FinancialHardshipsMulti = policyDetails.FinancialHardshipsMulti;
            data.InsuranceFraud = policyDetails.InsuranceFraud;
            data.ForeclosureOrRepossessionOrBankruptcy = policyDetails.ForeclosureOrRepossessionOrBankruptcy;
            data.Progressive_Preferences1 = policyDetails.Progressive_Preferences1;
            data.Progressive_Preferences2 = policyDetails.Progressive_Preferences2;
            data.MarketValue = policyDetails.MarketValue;
            data.PoliciesWithAgent = policyDetails.PoliciesWithAgent;
            data.PrimaryHome = policyDetails.PrimaryHome;
            data.PriorInsuranceProperty = policyDetails.PriorInsuranceProperty;
            data.BundelingAutoPolicyNum = policyDetails.BundelingAutoPolicyNum;
            data.NumberOfChildren = policyDetails.NumberOfChildren;
            data.PLDfForm = policyDetails.PLDfForm;
            data.PLHeatingUpdate = policyDetails.PLHeatingUpdate;
            data.PLPlumbingUpdated = policyDetails.PLPlumbingUpdated;
            data.PL_AdditionalStructures_None = policyDetails.PL_AdditionalStructures_None;
            data.Occupation = policyDetails.Occupation;
            data.OtherProductType = policyDetails.OtherProductType;
        }

        public static void MapAutoPolicyDetails(PersonalLineData data, PolicyDetails policyDetails)
        {
            data.EffectiveDate = policyDetails.EffectiveDate;
            data.PriorCarrierExpirationDateAuto = policyDetails.PriorCarrierExpirationDateAuto;
            data.AutoDeathIndemnity = policyDetails.AutoDeathIndemnity;
            data.CreditCheckPermission = policyDetails.CreditCheckPermission;
            data.PIP = policyDetails.PIP;
            data.PD = policyDetails.PD;
            data.BI = policyDetails.BI;
            data.IsMailAddress = policyDetails.IsMailAddress;
            data.TypeOfResidence = policyDetails.TypeOfResidence;
            data.AutoInsuranceCancelled = policyDetails.AutoInsuranceCancelled;
            data.SelectPriorLiabilityLimitsAuto = policyDetails.SelectPriorLiabilityLimitsAuto;
            data.IHerebyConfirm = policyDetails.IHerebyConfirm;
            data.UM = policyDetails.UM;
            data.YearsAtAddress = policyDetails.YearsAtAddress;
            data.MonthsAtAddress = policyDetails.MonthsAtAddress;
            data.UIM = policyDetails.UIM;
            data.UMPD = policyDetails.UMPD;
            data.YearsWithContinuousCoverageAuto = policyDetails.YearsWithContinuousCoverageAuto;
            data.MP = policyDetails.MP;
            data.YearsWithPriorCarrierAuto = policyDetails.YearsWithPriorCarrierAuto;
            data.MonthsWithPriorCarrierAuto = policyDetails.MonthsWithPriorCarrierAuto;
            data.OccupationStr = policyDetails.OccupationStr;
            data.PIPDeductible = policyDetails.PIPDeductible;
            data.CurrentPersonalAutoCarrier = policyDetails.CurrentPersonalAutoCarrier;
            data.AutoHomeInsurance = policyDetails.AutoHomeInsurance;
            data.UBIDiscount = policyDetails.UBIDiscount;
            data.PriorInsuranceProperty = policyDetails.PriorInsuranceProperty;
    
        }

        public static void MapAutoDrivers(PersonalLineData data, List<DriverModel> drivers)
        {
            data.Drivers = drivers;
        }

        public static void MapAutoVehicles(PersonalLineData data, List<VehicleModel> vehicles)
        {
            data.PersonalVehicles = vehicles;
        }

        public static void MapCustomFields(PersonalLineData data, CustomFieldsModel customFields)
        {
            data.CustomFields = customFields;
        }
    }
}
