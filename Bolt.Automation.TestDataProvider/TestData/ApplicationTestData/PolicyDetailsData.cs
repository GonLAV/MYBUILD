using Bolt.Automation.Common.Models.TestData.Interview.Models;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class PolicyTestData
        {
            public static PolicyDetails PersonalAutoPolicyData = new()
            {
                CurrentPersonalAutoCarrier = "TwentyFirstCentury",
                EffectiveDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                PriorCarrierExpirationDateAuto = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                AutoDeathIndemnity = "NoCoverage",
                AutoHomeInsurance = false,
                UBIDiscount = true,
                CreditCheckPermission = true,
                PIP = "Reject",
                PD = "cov50000",
                BI = "cov50to100",
                TypeOfResidence = "OwnHome",
                AutoInsuranceCancelled = false,
                SelectPriorLiabilityLimitsAuto = "FiftyToHundred",
                IHerebyConfirm = true,
                UM = "cov50to100",
                YearsAtAddress = 3,
                MonthsAtAddress = 0,
                UIM = "cov50to100",
                UMPD = "Reject",
                YearsWithContinuousCoverageAuto = 5,
                MP = "cov1000",
                YearsWithPriorCarrierAuto = 2,
                MonthsWithPriorCarrierAuto = 0,
                OccupationStr = "Other",
                EmploymentIndustry = "Other",
                PIPDeductible = "cov0",
            };

            /// <summary>
            /// Policy data for Safeco (Liberty Mutual) carrier — uses No_Coverage for PIP
            /// which is required by the Safeco carrier's valid values.
            /// </summary>
            public static PolicyDetails SafecoAutoPolicyData = new()
            {
                CurrentPersonalAutoCarrier = "TwentyFirstCentury",
                EffectiveDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                PriorCarrierExpirationDateAuto = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                AutoDeathIndemnity = "NoCoverage",
                AutoHomeInsurance = false,
                UBIDiscount = true,
                CreditCheckPermission = true,
                PIP = "No_Coverage",
                PD = "cov50000",
                BI = "cov50to100",
                TypeOfResidence = "OwnHome",
                AutoInsuranceCancelled = false,
                SelectPriorLiabilityLimitsAuto = "FiftyToHundred",
                IHerebyConfirm = true,
                UM = "cov50to100",
                YearsAtAddress = 3,
                MonthsAtAddress = 0,
                UIM = "cov50to100",
                UMPD = "Reject",
                YearsWithContinuousCoverageAuto = 5,
                MP = "cov1000",
                YearsWithPriorCarrierAuto = 2,
                MonthsWithPriorCarrierAuto = 0,
                OccupationStr = "Other",
                EmploymentIndustry = "Other",
                PIPDeductible = "cov0",
            };

            public static PolicyDetails UsaaPolicyTestData = new()
            {
                CurrentPersonalAutoCarrier = "OtherStandard",
                EffectiveDate = DateTime.Today.ToString("yyyy-MM-dd"),
                AutoHomeInsurance = false,
                TypeOfResidence = "OwnHome",
            };

            public static PolicyDetails PersonalHomePolicyDetails = new()
            {
                PriorCarrierExpirationDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                PLHaveAnyLosses = "false",
                IAgreeToReceiveEmailsByBolt = true,
                IHerebyConfirm = true,
                CreditCheckPermission = true,
                AnyAdditionalInsured = false,
                CurrentPersonalHomeownerCarrier = "AAA",
                DwellingMedicalPayments = "cov1000",
                EffectiveDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                YearsWithContinuousCoverageHome = 3,
                YearsWithPriorCarrierHome = 3,
                HomeAutoInsurance = false,
                PLAllPerilsDeductible = "OneThousand",
                PLPersonalLiability = "OneHundred",
                PropertyInsuranceCancelled = false,
                PersonalLineReplacementCost = 250000,
                LossOfUse = 25000,
                DiscloseHomeCurrentPremium = false,
                LapseInCoverage = false,
                ForeclosureOrRepossessionOrBankruptcy = false
            };

            public static PolicyDetails CondominiumPolicyDetails = new()
            {
                PriorLiabilityCoverageHome = "Threehundredthousand",
                PLPersonalLiability = "ThreeHundred",
                AnyAdditionalInsured = false,
                PropertyInsuranceCancelled = false,
                PersonalLineReplacementCost = 551000,
                IAgreeToReceiveEmailsByBolt = true,
                YearsWithPriorCarrierHome = 2,
                YearsWithPriorCarrierAuto = 1,
                CurrentPersonalHomeownerCarrier = "OtherStandard",
                CreditCheckPermission = true,
                PLAllPerilsDeductible = "OneThousand",
                PLHaveAnyLosses = "false",
                IHerebyConfirm = true,
                DwellingMedicalPayments = "cov5000",
                LossOfUse = 8000,
                EffectiveDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
            };

            public static PolicyDetails PGRPolicyDetails = new()
            {
                EffectiveDate = DateTime.Today.AddDays(20).ToString("yyyy-MM-dd"),
                PriorCarrierExpirationDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                PersonalLineReplacementCost = 694000,
                PLPersonalLiability = "ThreeHundred",
                PLAllPerilsDeductible = "OneThousand",
                DwellingMedicalPayments = "cov5000",
                YearsWithPriorCarrierHome = 3,
                CurrentPersonalHomeownerCarrier = "Other",
                PropertyInsuranceCancelled = false,
                PLHaveAnyLosses = "false",
                OccupationStr = "OtherOccupation",
                EmploymentIndustry = "Other",
                IHerebyConfirm = true,
                CreditCheckPermission = true,
                AnyAdditionalInsured = false,
                HomeAutoInsurance = true,
                YearsAtAddress = 3,
                MonthsAtAddress = 0,
                PL_Bankruptcy = false,
                PL_foreclosure = false,
                EligibilityFinancialHardship = false,
                FinancialHardshipsMulti = new List<string> { "None" },
                InsuranceFraud = false,
                Progressive_Preferences1 = 11,
                Progressive_Preferences2 = 11,
                MarketValue = 694000,
                PoliciesWithAgent = false,
                PrimaryHome = true,
                PriorInsuranceProperty = true,
                BundelingAutoPolicyNum = "11111",
                NumberOfChildren = 1,
                PLDfForm = "DP3_Special",
                PLHeatingUpdate = "CompleteUpdate",
                PLPlumbingUpdated = "CompleteUpdate",
                PL_AdditionalStructures_None = false,
                Occupation = "OtherOccupation",
                OtherProductType = "None",
            };

            public static PolicyDetails CLPolicyDetails = new()
            {
                CurrentPremiumWC = "2400",
                EffectiveDate = DateTime.Today.ToString("yyyy-MM-dd"),
                CurrentWCCarrier = "SelfInsured",
                DeclinedCanceledOrNonRenewed = false,
                WCContinousCoverage = true,
                WcExpirationDate = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd"),
                CreditCheckPermission = true,
            };
        }
    }
}
