namespace Bolt.Automation.FrontEnds.Projects.Interview.FormData
{
    /// <summary>
    /// Interview-specific field name constants.
    /// Extends CommonFieldNames with Interview v3 specific fields.
    /// </summary>
    public partial class FieldNames : Bolt.Automation.FrontEnds.FormData.Common.FieldNames
    {
        #region Start Page Fields
        public const string InterviewAddress = nameof(InterviewAddress);
        public const string OrganizationName = nameof(OrganizationName);
        public const string EposNaicDescription = nameof(EposNaicDescription);
        #endregion

        #region Home Page Fields
        public const string PLHomeLength = nameof(PLHomeLength);
        public const string PLHomeWidth = nameof(PLHomeWidth);
        public const string IsHomeNewPurchase = nameof(IsHomeNewPurchase);
        public const string PurchaseDate = nameof(PurchaseDate);
        public const string PLHomePurchasePrice = nameof(PLHomePurchasePrice);
        public const string CurrentlyForSale = nameof(CurrentlyForSale);
        public const string PLOriginalOwner = nameof(PLOriginalOwner);
        public const string HomeUnderConstruction = nameof(HomeUnderConstruction);
        public const string IsHomeUnderMajorRenovation = nameof(IsHomeUnderMajorRenovation);
        public const string IsHomeUnderRenovation = nameof(IsHomeUnderRenovation);
        public const string PriorRenovation = nameof(PriorRenovation);
        public const string ExistingDamageOnDwelling = nameof(ExistingDamageOnDwelling);
        public const string UnitRentedToOthersCoverage = nameof(UnitRentedToOthersCoverage);
        public const string IsPropertyRented = nameof(IsPropertyRented);
        public const string AnyResidenceEmployees = nameof(AnyResidenceEmployees);
        public const string BusinessOrDaycare = nameof(BusinessOrDaycare);
        public const string IsHomeInsideCityLimits = nameof(IsHomeInsideCityLimits);
        public const string IsCommunityGuarded = nameof(IsCommunityGuarded);
        public const string IsYourHomeVisibleToNeighbors = nameof(IsYourHomeVisibleToNeighbors);
        public const string DistanceToFireHydrant = nameof(DistanceToFireHydrant);
        public const string DistanceToFireStation = nameof(DistanceToFireStation);
        public const string StormShutters = nameof(StormShutters);
        public const string OpeningProtectionAndStrength = nameof(OpeningProtectionAndStrength);
        public const string ViciousExoticAnimals = nameof(ViciousExoticAnimals);
        public const string AnimalBites = nameof(AnimalBites);
        public const string AnyOfTheResidentsSmoke = nameof(AnyOfTheResidentsSmoke);
        public const string SinkholeInvestigationOrClaim = nameof(SinkholeInvestigationOrClaim);
        public const string IsInsuredCanceledDeclined = nameof(IsInsuredCanceledDeclined);
        public const string WhereIsYourDwellingLocated = nameof(WhereIsYourDwellingLocated);
        #endregion

        #region Structure Page Fields
        public const string ConstructionQuality = nameof(ConstructionQuality);
        public const string ConstructionPercentage = nameof(ConstructionPercentage);
        public const string ConstructionMasonryPercentage = nameof(ConstructionMasonryPercentage);
        public const string WhichAppliesTheConstructionDate = nameof(WhichAppliesTheConstructionDate);
        public const string SlabType = nameof(SlabType);
        public const string NumberOfFloorsInTheBuilding = nameof(NumberOfFloorsInTheBuilding);
        public const string MitRoofShapeType = nameof(MitRoofShapeType);
        public const string RoofRating = nameof(RoofRating);
        public const string YearRoofUpdated = nameof(YearRoofUpdated);
        public const string MitCreditForm = nameof(MitCreditForm);
        public const string RoofCover = nameof(RoofCover);
        public const string RoofDeckAttachment = nameof(RoofDeckAttachment);
        public const string RoofWall = nameof(RoofWall);
        public const string SecondaryWaterResistance = nameof(SecondaryWaterResistance);
        public const string WhatIsTypeOfTheBuilding = nameof(WhatIsTypeOfTheBuilding);
        public const string WhatIsThePurposeOfTheBuilding = nameof(WhatIsThePurposeOfTheBuilding);
        public const string IsTheBuildingLocatedOverWater = nameof(IsTheBuildingLocatedOverWater);
        public const string DoesTheBuildingHaveExtension = nameof(DoesTheBuildingHaveExtension);
        public const string ProtectiveSiding = nameof(ProtectiveSiding);
        public const string HomeTiedDown = nameof(HomeTiedDown);
        public const string HomePermanentFoundation = nameof(HomePermanentFoundation);
        public const string LandOwnedByApplicant = nameof(LandOwnedByApplicant);
        public const string ModularHome = nameof(ModularHome);
        public const string UndergroundFuelTank = nameof(UndergroundFuelTank);
        public const string BasementType = nameof(BasementType);
        public const string WindMitigationCreditForm = nameof(WindMitigationCreditForm);
        #endregion

        #region Features Page Fields
        public const string WaterHeaterType = nameof(WaterHeaterType);
        public const string ElectricalType = nameof(ElectricalType);
        public const string PlumbingType = nameof(PlumbingType);
        public const string WiringType = nameof(WiringType);
        public const string PLElectricalUpdated = nameof(PLElectricalUpdated);
        public const string ElectricalUpdatedYear = nameof(ElectricalUpdatedYear);
        public const string PLHvacUpdated = nameof(PLHvacUpdated);
        public const string HvacUpdatedYear = nameof(HvacUpdatedYear);
        public const string PLWaterHeaterUpdated = nameof(PLWaterHeaterUpdated);
        public const string WaterHeaterUpdatedYear = nameof(WaterHeaterUpdatedYear);
        public const string PLRoofUpdated = nameof(PLRoofUpdated);
        public const string RoofUpdatedYear = nameof(RoofUpdatedYear);
        public const string SupplementalHeatSource = nameof(SupplementalHeatSource);
        public const string SupplementalHeatSourceType = nameof(SupplementalHeatSourceType);
        public const string WoodBurningStove = nameof(WoodBurningStove);
        public const string WoodBurningStoveType = nameof(WoodBurningStoveType);
        #endregion

        #region Vehicle Page Fields
        public const string VehicleVIN = nameof(VehicleVIN);
        public const string VehicleYear = nameof(VehicleYear);
        public const string VehicleMake = nameof(VehicleMake);
        public const string VehicleModel = nameof(VehicleModel);
        public const string VehicleBodyStyle = nameof(VehicleBodyStyle);
        public const string VehicleGaragingAddress = nameof(VehicleGaragingAddress);
        public const string VehicleCostNew = nameof(VehicleCostNew);
        public const string VehicleAnnualMiles = nameof(VehicleAnnualMiles);
        public const string VehiclePrimaryUse = nameof(VehiclePrimaryUse);
        public const string VehicleOwnership = nameof(VehicleOwnership);
        public const string VehicleAntiTheft = nameof(VehicleAntiTheft);
        public const string VehiclePassiveRestraint = nameof(VehiclePassiveRestraint);
        public const string VehicleAntiLockBrakes = nameof(VehicleAntiLockBrakes);
        public const string VehicleDaytimeRunningLights = nameof(VehicleDaytimeRunningLights);
        #endregion

        #region Motorcycle
        public const string MotorcycleVIN = nameof(MotorcycleVIN);
        public const string MotorcycleYear = nameof(MotorcycleYear);
        public const string MotorcycleMake = nameof(MotorcycleMake);
        public const string MotorcycleModel = nameof(MotorcycleModel);
        public const string MotorcycleActualCashValue = nameof(MotorcycleActualCashValue);
        public const string MotorcycleEstimatedAnnualMileage = nameof(MotorcycleEstimatedAnnualMileage);
        public const string MotorcyclePrimaryUse = nameof(MotorcyclePrimaryUse);
        public const string MotorcyclePurchaseDate = nameof(MotorcyclePurchaseDate);
        public const string MotorcycleCostNewValue = nameof(MotorcycleCostNewValue);
        public const string MotorcycleStorageType = nameof(MotorcycleStorageType);
        public const string MotorcycleIsStoredAtDifferentAddress = nameof(MotorcycleIsStoredAtDifferentAddress);
        #endregion

        #region Operator Page Fields
        public const string OperatorDateOfBirth = nameof(OperatorDateOfBirth);
        public const string OperatorGender = nameof(OperatorGender);
        public const string OperatorMaritalStatus = nameof(OperatorMaritalStatus);
        public const string OperatorRelationship = nameof(OperatorRelationship);
        public const string OperatorLicenseState = nameof(OperatorLicenseState);
        public const string CLOperatorLicenseState = nameof(CLOperatorLicenseState);
        public const string OperatorLicenseNumber = nameof(OperatorLicenseNumber);
        public const string OperatorLicenseStatus = nameof(OperatorLicenseStatus);
        public const string OperatorDateLicensed = nameof(OperatorDateLicensed);
        public const string OperatorSR22Required = nameof(OperatorSR22Required);
        public const string OperatorGoodStudent = nameof(OperatorGoodStudent);
        public const string OperatorDriverTraining = nameof(OperatorDriverTraining);
        public const string OperatorDefensiveDriver = nameof(OperatorDefensiveDriver);
        public const string OperatorMatureDriver = nameof(OperatorMatureDriver);
        public const string HasCommercialLicense = nameof(HasCommercialLicense);
        public const string ExcludedDriver = nameof(ExcludedDriver);

        public const string PermanentResidenceOfHousehold = nameof(PermanentResidenceOfHousehold);

        public const string VehicleUsage = nameof(VehicleUsage);
        #endregion

        #region Policy Page Fields
        // Prior Insurance - Home
        public const string CurrentPersonalHomeownerCarrierInterview = nameof(CurrentPersonalHomeownerCarrierInterview);
        public const string YearsWithContinuousCoverageHome = nameof(YearsWithContinuousCoverageHome);
        public const string PriorCarrierExpirationDate = nameof(PriorCarrierExpirationDate);
        public const string PriorBOPCarrierExpirationDate = nameof(PriorBOPCarrierExpirationDate);
        public const string PriorPersonalHomeLiability = nameof(PriorPersonalHomeLiability);

        // Prior Insurance - Auto
        public const string MonthsWithPriorCarrierAuto = nameof(MonthsWithPriorCarrierAuto);
        public const string PriorPersonalAutoLiability = nameof(PriorPersonalAutoLiability);
        public const string CurrentAutoAnnualPremium = nameof(CurrentAutoAnnualPremium);
        // PriorCarrierExpirationDateAuto is in CommonFieldNames

        // Dwelling Coverages
        public const string PLOtherStructures = nameof(PLOtherStructures);
        public const string PLPersonalProperty = nameof(PLPersonalProperty);
        public const string LossOfUse = nameof(LossOfUse);
        public const string ActualCashValue = nameof(ActualCashValue);
        public const string PersonalPropertyReplacementCost = nameof(PersonalPropertyReplacementCost);

        // Liability Coverages
        public const string DwellingMedicalPayments = nameof(DwellingMedicalPayments);
        public const string MedicalPayments = nameof(MedicalPayments);
        public const string PersonalUmbrellaLimit = nameof(PersonalUmbrellaLimit);
        public const string GeneralLiabilityLimit = nameof(GeneralLiabilityLimit);
        public const string UnderlyingAutoLiabilityLimit = nameof(UnderlyingAutoLiabilityLimit);

        // Property Deductibles
        public const string HurricaneDeductible = nameof(HurricaneDeductible);
        public const string WindHailDeductible = nameof(WindHailDeductible);
        public const string WindstormDeductible = nameof(WindstormDeductible);

        // Auto Liability
        public const string LiabilityPropertyDamage = nameof(LiabilityPropertyDamage);
        public const string CL_BIPD = nameof(CL_BIPD);
        public const string CL_MedPay = nameof(CL_MedPay);

        // UM/UIM Coverages
        public const string UninsuredMotorist = nameof(UninsuredMotorist);
        public const string UnderinsuredMotorist = nameof(UnderinsuredMotorist);
        public const string HowMuchExcessUninsuredUnderinsuredMotorist = nameof(HowMuchExcessUninsuredUnderinsuredMotorist);
        public const string UninsuredMotoristOption = nameof(UninsuredMotoristOption);
        public const string UMPD = nameof(UMPD);
        public const string UIMPD = nameof(UIMPD);
        public const string UMStacking = nameof(UMStacking);
        public const string UIMStacking = nameof(UIMStacking);

        // PIP Coverages
        public const string PIPDeductible = nameof(PIPDeductible);
        public const string PIPApplies = nameof(PIPApplies);
        public const string WageLoss = nameof(WageLoss);

        // Auto Additional Coverages
        public const string CL_TransportationExpense = nameof(CL_TransportationExpense);
        public const string RoadSideAssistance = nameof(RoadSideAssistance);

        // Medical/Disability Coverages
        public const string AutoDeathIndemnity = nameof(AutoDeathIndemnity);
        public const string TotalDisability = nameof(TotalDisability);
        public const string IncomeLoss = nameof(IncomeLoss);
        public const string FirstPartyMedicalBenefits = nameof(FirstPartyMedicalBenefits);
        public const string FirstPartyBenefits = nameof(FirstPartyBenefits);
        public const string ExtraordinaryMedicalExpense = nameof(ExtraordinaryMedicalExpense);
        public const string TortThreshold = nameof(TortThreshold);

        // Yes/No Toggle Questions
        public const string PLHaveAnyLosses = nameof(PLHaveAnyLosses);
        public const string HasInsuranceCompanyCancelledDeclinedRefusedRenewal = nameof(HasInsuranceCompanyCancelledDeclinedRefusedRenewal);
        public const string IsCoverageDeclinedCancelledPastThreeYears = nameof(IsCoverageDeclinedCancelledPastThreeYears);
        public const string IsExistingClientInAgency = nameof(IsExistingClientInAgency);
        public const string IsAdditionalInterests = nameof(IsAdditionalInterests);
        public const string GapInCoverage = nameof(GapInCoverage);
        public const string AutoHomeInsurance = nameof(AutoHomeInsurance);
        public const string CurrentAnnualPremium = nameof(CurrentAnnualPremium);
        // HomeAutoInsurance is in CommonFieldNames

        // Special Coverages
        public const string EarthquakeCoverage = nameof(EarthquakeCoverage);
        public const string IsNFIPFloodPolicy = nameof(IsNFIPFloodPolicy);
        public const string LiabilityExtension = nameof(LiabilityExtension);
        public const string ExcessLiability = nameof(ExcessLiability);
        public const string ValuableArticles = nameof(ValuableArticles);

        // Commercial/Business
        public const string CurrentBopCarrier = nameof(CurrentBopCarrier);
        public const string CurrentWCCarrier = nameof(CurrentWCCarrier);
        public const string IsNewBusiness = nameof(IsNewBusiness);
        public const string AnnualRevenue = nameof(AnnualRevenue);
        public const string AnnualPayroll = nameof(AnnualPayroll);
        public const string CurrentPremium = nameof(CurrentPremium);
        public const string WCContinuousCoverage = nameof(WCContinuousCoverage);
        public const string OfficerFirstName = nameof(OfficerFirstName);
        public const string OfficerLastName = nameof(OfficerLastName);
        public const string OfficerTitle = nameof(OfficerTitle);
        public const string OfficerOwnershipPercent = nameof(OfficerOwnershipPercent);
        public const string OfficerPrimaryStateOfOperation = nameof(OfficerPrimaryStateOfOperation);
        public const string HasSafetyProgramBeenCertified = nameof(HasSafetyProgramBeenCertified);
        public const string NumberOfVehicles = nameof(NumberOfVehicles);
        public const string AcordAppetiteWC = nameof(AcordAppetiteWC);
        public const string EmployeeWorkplaceSafetyProgram = nameof(EmployeeWorkplaceSafetyProgram);
        public const string MarketAppetitePanel = nameof(MarketAppetitePanel);
        public const string AcordMarkAllAsYes = nameof(AcordMarkAllAsYes);

        public const string MotorcycleCarrier = nameof(MotorcycleCarrier);
        public const string MotorcyclePriorCarrierExpirationDate = nameof(MotorcyclePriorCarrierExpirationDate);
        public const string MotorcycleIndictedOrConvictedFelony = nameof(MotorcycleIndictedOrConvictedFelony);
        #endregion

        #region Motorcycle Coverage Page Fields

        public const string MotorcycleLiabilityLimit = nameof(MotorcycleLiabilityLimit);
        public const string MotorcyclePropertyDamage = nameof(MotorcyclePropertyDamage);
        public const string MotorcycleSupplementaryUninsured = nameof(MotorcycleSupplementaryUninsured);
        public const string MotorcycleUninsuredMotorist = nameof(MotorcycleUninsuredMotorist);
        public const string MotorcycleUninsuredMotoristPropertyDamage = nameof(MotorcycleUninsuredMotoristPropertyDamage);
        public const string MotorcycleMedicalPayments = nameof(MotorcycleMedicalPayments);
        public const string MotorcycleRoadAssistance = nameof(MotorcycleRoadAssistance);
        public const string MotorcycleComprehensiveDeductible = nameof(MotorcycleComprehensiveDeductible);
        public const string MotorcycleCollisionDeductible = nameof(MotorcycleCollisionDeductible);
        public const string MotorcycleAccesoriesValue = nameof(MotorcycleAccesoriesValue);
        #endregion

        #region Applicant Page Fields
        public const string ApplicantSSN = nameof(ApplicantSSN);
        public const string ApplicantMiddleName = nameof(ApplicantMiddleName);
        public const string ApplicantSuffix = nameof(ApplicantSuffix);
        public const string ApplicantMailingAddress = nameof(ApplicantMailingAddress);
        public const string ApplicantCity = nameof(ApplicantCity);
        public const string ApplicantState = nameof(ApplicantState);
        public const string ApplicantZip = nameof(ApplicantZip);
        public const string ForeclosureOrRepossessionOrBankruptcy = nameof(ForeclosureOrRepossessionOrBankruptcy);
        public const string IsMailAddress = nameof(IsMailAddress);
        #endregion

        #region Results Page Fields
        public const string EmailQuoteButton = nameof(EmailQuoteButton);
        public const string DownloadAllFormsButton = nameof(DownloadAllFormsButton);
        public const string DownloadAllFormsButtonPopup = nameof(DownloadAllFormsButtonPopup);
        public const string OfflineRequestButton = nameof(OfflineRequestButton);
        public const string PaperApplicationCard = nameof(PaperApplicationCard);
        public const string VinSubmitButton = nameof(VinSubmitButton);
        #endregion

        #region Commercial Fields
        public const string BusinessDescription = nameof(BusinessDescription);
        public const string BusinessStartDate = nameof(BusinessStartDate);
        public const string NumberOfEmployees = nameof(NumberOfEmployees);
        public const string BusinessOwnershipType = nameof(BusinessOwnershipType);
        public const string LocationSquareFootage = nameof(LocationSquareFootage);
        public const string LocationBuildingValue = nameof(LocationBuildingValue);
        public const string LocationContentsValue = nameof(LocationContentsValue);
        public const string EmployeeClassCode = nameof(EmployeeClassCode);
        public const string EmployeePayroll = nameof(EmployeePayroll);
        public const string EmployeeCount = nameof(EmployeeCount);
        public const string FederalIDNumber = nameof(FederalIDNumber);
        public const string BusinessStartYear = nameof(BusinessStartYear);
        public const string ManagerExperience = nameof(ManagerExperience);
        public const string SubcontractedWork = nameof(SubcontractedWork);
        public const string LegalEntity = nameof(LegalEntity);
        public const string NumberOfFullTimeEmployees = nameof(NumberOfFullTimeEmployees);
        public const string NumberOfPartTimeEmployees = nameof(NumberOfPartTimeEmployees);
        public const string EmployeeClassCodeText = nameof(EmployeeClassCodeText);
        public const string EmployeeClassificationSearch = nameof(EmployeeClassificationSearch);
        public const string EmployeeClassificationPayroll = nameof(EmployeeClassificationPayroll);
        public const string OfficerEmployeeClassificationSearch = nameof(OfficerEmployeeClassificationSearch);
        public const string ExperienceModification = nameof(ExperienceModification);
        public const string WCWhoIsCovered = nameof(WCWhoIsCovered);
        public const string CreditCheckPermission = nameof(CreditCheckPermission);
        public const string AnnualOwnerPayroll = nameof(AnnualOwnerPayroll);
        public const string OfficerAnnualPayroll = nameof(OfficerAnnualPayroll);
        public const string OfficerDateOfBirth = nameof(OfficerDateOfBirth);
        public const string PolicyDataAnnualSales = nameof(PolicyDataAnnualSales);
        public const string InsureTheBuilding = nameof(InsureTheBuilding);
        public const string WaiverSubrogation = nameof(WaiverSubrogation);
        public const string PrimaryNonContributory = nameof(PrimaryNonContributory);
        public const string QuoteAllEligible = nameof(QuoteAllEligible);
        #endregion

        #region  Commercial Auto Fields
        public const string PriorCarrierAutoCL = nameof(PriorCarrierAutoCL);
        public const string CL_ReasonNoPriorAutoInsurance = nameof(CL_ReasonNoPriorAutoInsurance);
        public const string CurrentCarrierExpirationDate = nameof(CurrentCarrierExpirationDate);
        public const string YearsWithContinuousPersonalAutoCarrierCL = nameof(YearsWithContinuousPersonalAutoCarrierCL);
        public const string CLAutoPolicyInsured = nameof(CLAutoPolicyInsured);
        public const string OperatorFirstName = nameof(OperatorFirstName);
        public const string OperatorLastName = nameof(OperatorLastName);
        public const string OperatorDOB = nameof(OperatorDOB);
        public const string TypeOfVehicle = nameof(TypeOfVehicle);
        public const string PurchaseOrLeases = nameof(PurchaseOrLeases);
        public const string CL_LengthVehicleOwnership = nameof(CL_LengthVehicleOwnership);
        public const string AnnualMileageCL = nameof(AnnualMileageCL);
        public const string CL_RentalDowntime = nameof(CL_RentalDowntime);

        #endregion
        #region Email Quotes Popup Fields
        public const string EmailQuoteSenderName = nameof(EmailQuoteSenderName);
        public const string EmailQuoteSubject = nameof(EmailQuoteSubject);
        public const string EmailQuoteMessage = nameof(EmailQuoteMessage);
        public const string CarrierSelect = nameof(CarrierSelect);
        #endregion

        #region Common Popup Fields
        public const string PopupFreeText = nameof(PopupFreeText);
        #endregion

        #region Payment Fields
        public const string PaymentPlan = nameof(PaymentPlan);
        public const string PaymentMethod = nameof(PaymentMethod);
        public const string AccountNumber = nameof(AccountNumber);
        public const string RoutingNumber = nameof(RoutingNumber);
        public const string BankName = nameof(BankName);
        public const string AccountHolderName = nameof(AccountHolderName);
        #endregion

        #region KLX Old Interview - CL Auto Start Page
        public const string Certification = nameof(Certification);
        public const string DeclinationReason = nameof(DeclinationReason);
        #endregion

        #region KLX Old Interview - PL Home Start Page
        public const string PLDeclinationReason = nameof(PLDeclinationReason);
        public const string PLIneligibleReason = nameof(PLIneligibleReason);
        #endregion

        #region KLX Old Interview - CL Auto Business Page
        public const string OwnerInvolvedInDailyOperations = nameof(OwnerInvolvedInDailyOperations);
        public const string TowingOrHauling = nameof(TowingOrHauling);
        #endregion

        #region KLX Old Interview - CL Auto Vehicle Page
        // "Average daily number of job sites / trips / deliveries"
        public const string VehicleAverageDailyTrips = nameof(VehicleAverageDailyTrips);
        // "Is the vehicle garaged at a different address than the primary business address?"
        public const string VehicleGaragedDifferentAddress = nameof(VehicleGaragedDifferentAddress);
        // "Is this vehicle used to haul goods on a for-hire basis?"
        public const string VehicleHaulsGoodsForHire = nameof(VehicleHaulsGoodsForHire);
        public const string TruckSubCategory = nameof(TruckSubCategory);
        public const string TrailerLength = nameof(TrailerLength);
        #endregion

        #region KLX Old Interview - CL Auto Policy Page
        // Top-level "Combined Uninsured and Underinsured Motorist"
        public const string CombinedUninsuredUnderinsuredMotorist = nameof(CombinedUninsuredUnderinsuredMotorist);
        // Top-level "Uninsured Motorist Property Damage"
        public const string UninsuredMotoristPropertyDamage = nameof(UninsuredMotoristPropertyDamage);

        // Per-vehicle CL coverages (different class names + Yes/No radio interaction than PL).
        public const string CL_FullGlass = nameof(CL_FullGlass);
        public const string CL_RoadSide = nameof(CL_RoadSide);
        public const string CL_ComprehensiveDeductible = nameof(CL_ComprehensiveDeductible);
        public const string CL_CollisionDeductible = nameof(CL_CollisionDeductible);
        public const string CL_CurrentVehicleValue = nameof(CL_CurrentVehicleValue);
        public const string CL_FireTheftCAC = nameof(CL_FireTheftCAC);

        // Top-level, no-fault-state-only ("PIP") coverage. Required only when the risk
        // address is in a no-fault state (e.g. MN) - see ProductCommercialAutoCoveragesPageCL.
        public const string CL_PIP = nameof(CL_PIP);
        public const string CL_PIPStacking = nameof(CL_PIPStacking);
        #endregion

        #region CL Additional Questions - Travelers BOP
        /// <summary>Any losses or claims in the past 3 years?</summary>
        public const string TravelersAnyLossesThreeYears = nameof(TravelersAnyLossesThreeYears);
        /// <summary>Does the insured own or rent the building?</summary>
        public const string TravelersBuildingOwnership = nameof(TravelersBuildingOwnership);
        /// <summary>Does the business have any additional insureds?</summary>
        public const string TravelersAdditionalInsured = nameof(TravelersAdditionalInsured);
        /// <summary>Does the business use subcontractors?</summary>
        public const string TravelersAnySubcontractors = nameof(TravelersAnySubcontractors);
        /// <summary>Does the building have an automatic sprinkler system?</summary>
        public const string TravelersSprinklerSystem = nameof(TravelersSprinklerSystem);
        /// <summary>Building replacement cost ($)</summary>
        public const string TravelersBuildingReplacementCost = nameof(TravelersBuildingReplacementCost);
        /// <summary>What year was the building constructed?</summary>
        public const string TravelersBuildingYearConstructed = nameof(TravelersBuildingYearConstructed);
        /// <summary>What year was the roof renovated or replaced?</summary>
        public const string TravelersRoofYearRenovated = nameof(TravelersRoofYearRenovated);
        /// <summary>Total building square footage</summary>
        public const string TravelersTotalBuildingSquareFootage = nameof(TravelersTotalBuildingSquareFootage);
        /// <summary>Total square footage your business occupies</summary>
        public const string TravelersBusinessSquareFootage = nameof(TravelersBusinessSquareFootage);
        /// <summary># of stories in the building</summary>
        public const string TravelersNumberOfStories = nameof(TravelersNumberOfStories);
        /// <summary>Construction type dropdown (e.g. Fire resistive)</summary>
        public const string TravelersConstructionType = nameof(TravelersConstructionType);
        #endregion
    }
}
