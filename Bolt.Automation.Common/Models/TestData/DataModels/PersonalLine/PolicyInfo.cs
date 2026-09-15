
namespace Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine
{
    public partial class PersonalLineData : BaseLineData
    {
        public int? YearsWithPriorCarrierAuto { get; set; }
        public string? TypeOfResidence { get; set; }
        public string? YearsWithContinuousPersonalHomeCarrier { get; set; }
        public bool? LapseInCoverage { get; set; }
        public int? PLOtherStructures { get; set; }
        public string? HurricaneDeductible { get; set; }
        public string? EarthquakeCoverage { get; set; }
        public string? PersonalUmbrellaLimit { get; set; }
        public string? IsTheNamedInsuredOrAnyResident { get; set; }
        public string? DoYoOwnPersonalWatercraft { get; set; }
        public string? DoyYouLiveInRecreationalVehicle { get; set; }
        public string? HowManyAcres { get; set; }
        public string? HaveYouOrAnyMemberOfYourHousehold { get; set; }
        public string? HasTheNamedInsuredBeenIndictedConvictedOfFelony { get; set; }
        public string? ExcessLiability { get; set; }
        public string? ValuableArticles { get; set; }
        public bool? AutoHomeInsurance { get; set; }
        public bool? IsNFIPFloodPolicy { get; set; }
        public bool? DiscloseHomeCurrentPremium { get; set; }
        public string? IsCoverageDeclinedCancelledPastThreeYears { get; set; }
        public string? IsExistingClientInAgency { get; set; }
        public string? IsAdditionalInterests { get; set; }
        public string? CurrentInsuranceCompany { get; set; }
        public string? PriorPersonalHomeLiability { get; set; }
        public string? MoldRemediationPercenteage { get; set; }
        public string? LandLimit { get; set; }
        public string? WindstormDeductible { get; set; }
        public string? DateOfLoss { get; set; }
        public string? PersonalLineLossAmount { get; set; }
        public string? PLLossDescription { get; set; }
        public int? NumberOfPayments { get; set; }
        public string? PariorLiabilityCoverageHome { get; set; }
 
        public bool? PL_Bankruptcy { get; set; }
        public bool? PL_foreclosure { get; set; }
        public bool? EligibilityFinancialHardship { get; set; }
        public List<string>? FinancialHardshipsMulti { get; set; }
        public bool? InsuranceFraud { get; set; }
        public string? CurrentPersonalHomeownerCarrier { get; set; }
        public bool? ForeclosureOrRepossessionOrBankruptcy { get; set; }

        // Progressive / carrier specific extension fields (optional)
        public string? BundelingAutoPolicyNum { get; set; }
        public string? Occupation { get; set; }
        public string? OtherProductType { get; set; }
    }
}
