using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Core;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData;

namespace Bolt.Automation.Tests.TestHelpers
{
    internal static class CarrierQuestionsHelper
    {
        public static List<string> GetHomeOwnersExpectedQuestions(CarrierEnums carrier, AddressKey addressKey)
        {
            if (carrier == CarrierEnums.TowerHill && addressKey != AddressKey.FL)
                return HomeownersTowerHillExceptFLQuestions;

            return HomeOwnersExpectedQuestions[carrier];
        }

        public static (List<string> Missing, List<string> Extra) GetCarrierQuestionsDiff(
            List<string> expectedFieldNames,
            List<string> actualQuestions,
            IAutomationLogger? logger = null)
        {
            var labels = expectedFieldNames
                .Select(fieldName => FieldRegistryHQXAgent.Fields[fieldName].Label)
                .ToList();

            var missing = labels.Except(actualQuestions).ToList();
            var extra = actualQuestions.Except(labels).ToList();

            logger?.Info($"Expected Questions: {string.Join(", ", labels)}");
            logger?.Info($"Actual Questions: {string.Join(", ", actualQuestions)}");
            logger?.Info($"Carrier questions diff — Missing: {string.Join(", ", missing)} | Extra: {string.Join(", ", extra)}");

            return (missing, extra);
        }

        public static readonly Dictionary<CarrierEnums, List<string>> HomeOwnersExpectedQuestions = new()
        {
            [CarrierEnums.Homesite] = new List<string>
            {
                FieldNames.PerimeterSecurityDD,
                FieldNames.BuiltOnSlope,
                FieldNamesHQXAgent.PL_CeilingHeight,
                FieldNamesHQXAgent.PL_VaultedCeilings,
                FieldNamesHQXAgent.PL_CrownMolding,
                FieldNames.PL_InteriorWallMaterial,
                FieldNames.PL_FlooringMaterial,
                FieldNames.PL_PrimaryCounterMaterial,
                FieldNamesHQXAgent.ResHeldTrust,
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.StillWater] = new List<string>
            {
                FieldNames.GatedOrLimited,
                FieldNames.PurchasePrice,
                FieldNames.NumberOfMortgagees,
                FieldNames.CurrentPersonalHomeownerCarrier,
                FieldNames.PropertyInsuranceCancelled,
                FieldNamesHQXAgent.PL_Bankruptcy,
                FieldNamesHQXAgent.CurrentlyOnBankruptcy,
                FieldNamesHQXAgent.PastBankruptcy,
                FieldNames.PLAllPerilsDeductible,
                FieldNames.PLPersonalLiability,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.PlymouthRock] = new List<string>
            {
                FieldNames.PropertyInsuranceCancelled,
                FieldNames.InsuranceFraud,
                FieldNamesHQXAgent.PL_Bankruptcy,
                FieldNamesHQXAgent.CurrentlyOnBankruptcy,
                FieldNamesHQXAgent.PastBankruptcy,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.Nationwide] = new List<string>
            {
                FieldNames.GatedOrLimited,
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNames.YearsWithPriorCarrierHome,
                FieldNames.InsuranceFraud,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.ASI] = new List<string>
            {
                FieldNames.NumberofAcres,
                FieldNames.PL_AdditionalStructures_Deck,
                FieldNames.PurchasePrice,
                FieldNames.NonSmoker,
                FieldNames.GatedOrLimited,
                FieldNames.NumberOfChildren,
                FieldNames.PL_PrimaryCounterMaterial,
                FieldNames.NumberOfMortgagees,
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNames.PLAllPerilsDeductible,
                FieldNames.PLPersonalLiability,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.Openly] = new List<string>
            {
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.TowerHill] = new List<string>
            {
                FieldNames.GatedOrLimited,
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNames.InsuranceFraud,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.AII] = new List<string>
            {
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.ForemostSignature] = new List<string>
            {
                FieldNames.YearsWithPriorCarrierHome,
                FieldNames.PLAllPerilsDeductible,
                FieldNames.PLPersonalLiability,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.True] = new List<string>
            {
                FieldNames.NumberofAcres,
                FieldNames.BuiltOnSlope,
                FieldNamesHQXAgent.PL_CeilingHeight,
                FieldNamesHQXAgent.PL_VaultedCeilings,
                FieldNamesHQXAgent.PL_CrownMolding,
                FieldNames.PL_PrimaryCounterMaterial,
                FieldNamesHQXAgent.ResHeldTrust,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.Bamboo] = new List<string>
            {
                FieldNames.GatedOrLimited,
                FieldNamesHQXAgent.RoofOver24,
                FieldNames.PLPlumbingUpdated,
                FieldNames.PlumbingUpdatedYear,
                FieldNames.NumberOfMortgagees,
                FieldNamesHQXAgent.ResHeldTrust,
                FieldNamesHQXAgent.PL_Bankruptcy,
                FieldNamesHQXAgent.CurrentlyOnBankruptcy,
                FieldNamesHQXAgent.PastBankruptcy,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.BambooSurplus] = new List<string>
            {
                FieldNames.GatedOrLimited,
                FieldNames.NumberOfMortgagees,
                FieldNamesHQXAgent.ResHeldTrust,
                FieldNamesHQXAgent.PL_Bankruptcy,
                // Answering PL_Bankruptcy = Yes reveals these two sub-checkboxes in the DOM
                // (confirmed against the live QA E&S page), same as the standard Bamboo flow.
                FieldNamesHQXAgent.CurrentlyOnBankruptcy,
                FieldNamesHQXAgent.PastBankruptcy,
                FieldNamesHQXAgent.FuelTanksBelowGround,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
        };

        public static readonly List<string> HomeownersTowerHillExceptFLQuestions = new()
        {
                FieldNames.GatedOrLimited,
                FieldNames.PurchasePrice,
                FieldNames.NumberOfMortgagees,
                FieldNames.InsuranceFraud,
                FieldNames.PLAllPerilsDeductible,
                FieldNames.PLPersonalLiability,
                FieldNamesHQXAgent.InterestedInFloodQuote,
        };

        public static readonly Dictionary<CarrierEnums, List<string>> DwellingFireExpectedQuestions = new()
        {
            [CarrierEnums.Foremost] = new List<string>
            {
                FieldNames.NumberofAcres,
                FieldNames.PL_PrimaryCounterMaterial,
                FieldNames.NonSmoker,
                FieldNames.NumberOfMortgagees,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.AmericanModern] = new List<string>
            {
                FieldNamesHQXAgent.InterestedInFloodQuote,
            }
        };

        public static readonly Dictionary<CarrierEnums, List<string>> CondominiumExpectedQuestions = new()
        {
            [CarrierEnums.Homesite] = new List<string>
            {
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.StillWater] = new List<string>
            {
                FieldNames.RoofType,
                FieldNames.PurchasePrice,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.PlymouthRock] = new List<string>
            {
                FieldNames.RoofType,
                FieldNames.InsuranceFraud,
                FieldNamesHQXAgent.PL_Bankruptcy,
                FieldNamesHQXAgent.CurrentlyOnBankruptcy,
                FieldNamesHQXAgent.PastBankruptcy,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.Nationwide] = new List<string>
            {
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNames.YearsWithPriorCarrierHome,
                FieldNames.InsuranceFraud,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
            [CarrierEnums.ASI] = new List<string>
            {
                FieldNames.RoofShape,
                FieldNames.PL_CentralAC,
                FieldNames.NumberOfMortgagees,
                FieldNames.NumberOfChildren,
                FieldNamesHQXAgent.PL_Houseoccup,
                FieldNames.NonSmoker,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            },
        };

        public static readonly Dictionary<CarrierEnums, List<string>> ManufacturedHomeExpectedQuestions = new()
        {
            [CarrierEnums.Foremost] =
            [
                FieldNames.NumberofAcres,
                FieldNamesHQXAgent.DealershipPurchase,
                FieldNames.PurchasePrice,
                FieldNamesHQXAgent.ResHeldTrust_2,
                FieldNamesHQXAgent.PropertyInsuranceCancelled_2,
                FieldNamesHQXAgent.InterestedInFloodQuote,
            ],
            [CarrierEnums.AmericanModern] =
            [
               FieldNamesHQXAgent.PropertyInsuranceCancelled_2,
               FieldNamesHQXAgent.InterestedInFloodQuote,
            ],
            [CarrierEnums.Assurant] =
            [
               FieldNames.NumberOfMortgagees,
               FieldNamesHQXAgent.InterestedInFloodQuote,
            ],
        };
    }
}
