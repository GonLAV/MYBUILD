using Bolt.Automation.Common.Models.TestData.Interview.Models;

namespace Bolt.Automation.TestDataProvider.TestData.ApplicationTestData
{
    public static partial class ApplicationTestData
    {
        public static class HomeFeaturesTestData
        {
            public static HomeFeaturesDetails PersonalHomeFeaturesData = new()
            {
                BurglarAlarm = false,
                DeadBoltLocks = false,
                BurglarAlarmType = "Direct",
                SmokeDetectorType = "Direct",
                ElectricCircuitBreaker = true,
                ElectricalUpdateYN = false,
                FireDetection = false,
                FireExtinguisher = false,
                HeatingUpdateYN = false,
                PL_AdditionalStructures_Pool = false,
                PlumbingUpdateYN = false,
                SmokeDetector = false,
                SprinklerSystem = false,
                ViciousExoticAnimals = false,
                MitCreditForm = false,
                PLHeatingType = "GasForcedAir",
            };

            public static HomeFeaturesDetails PGRHomeFeatureDetails = new()
            {
                // Security Features
                BurglarAlarm = true,
                BurglarAlarmType = "Direct",
                BurglarAlarmTypeMulti = new List<string>
                {
                    "HomeLocal", "PhoneAlerts", "PoliceDirect", "SystemCentral"
                },
                DeadBoltLocks = true,
                SmokeDetector = true,
                FireDetection = true,
                FireDetectionType = "Direct",
                FireDetectionTypeMulti = new List<string>
                {
                    "HomeLocal", "PhoneAlerts", "PoliceDirect", "SystemCentral"
                },
                FireExtinguisher = true,
                SprinklerSystem = false,

                // Electrical & Systems
                ElectricCircuitBreaker = true,
                ElectricalUpdateYN = true,
                ElectricalUpdatedYear = 2021,
                PLElectricalUpdated = "CompleteUpdate",
                HeatingUpdateYN = true,
                HeatingUpdateYear = 2021,
                PLHeatingUpdate = "CompleteUpdate",
                PLHeatingType = "GasForcedAir",
                PlumbingUpdateYN = true,
                PlumbingUpdatedYear = 2021,
                PLPlumbingUpdated = "CompleteUpdate",
                PlumbingType = "EntirelyCopper",
                PL_CentralAC = true,
                PL_HeatedByOil = false,

                // Additional Structures
                PL_AdditionalStructures_Garage = true,
                PL_AdditionalStructures_Pool = false,
                PL_AdditionalStructures_HotTub = false,
                PL_AdditionalStructures_Deck = false,
                PL_AdditionalStructures_Trampoline = false,
                TypeGarageCarport = "Attached",
                PL_NumberCarSpace = 3,
                MitWindowOpening = "HurricaneProtection",
                MitRoofCover = "FBCRoof",
                MitRoofDeck = "EightDAt12",
                MitRoofWall = "SingleWraps",

                // Construction & Interior Details
                ExteriorWallsConstruction = "WoodFraming",
                PersonalPropertyRC = true,
                PL_FlooringMaterial = new List<string> { "Carpet", "LaminateWood" },
                PL_PrimaryCounterMaterial = "GraniteorMarble",
                PL_InteriorWallMaterial = new List<string> { "DrywallVeneerPlaster" },
                PL_VaultedCeilings = 0,
                PL_CrownMolding = 0,
                PL_CeilingHeight = "EightftOrLess",

                // Fireplace
                NumberOfFirePlaces = 1,
                PL_TypeFireplaces = "Gas",
            };
        }
    }
}
