using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Models.CarrierBridge;
using Environment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.TestDataProvider.Repositories.DataStores
{
    public static class CarrierBridgeUrlDataStore
    {
        public static readonly Dictionary<Environment, CarrierBridgeUrlCollection> All = new()
        {
            [Environment.Qa] = new CarrierBridgeUrlCollection
            {
                Urls = new Dictionary<CarrierEnums, string>
                {
                    [CarrierEnums.ASI] = "americanstrategic",
                    [CarrierEnums.Homesite] = "homesite",
                    [CarrierEnums.StillWater] = "stillwater",
                    [CarrierEnums.AmericanModern] = "amsuite.amig.com",
                    [CarrierEnums.Foremost] = "foremost",
                    [CarrierEnums.Nationwide] = "nationwide",
                    [CarrierEnums.Openly] = "openly",
                    [CarrierEnums.PlymouthRock] = "plymouthrock",
                    [CarrierEnums.TowerHill] = "thig",
                    [CarrierEnums.Assurant] = "assurant",
                    [CarrierEnums.BambooSurplus] = "b4mb00"

                }
            },
            [Environment.Uat] = new CarrierBridgeUrlCollection
            {
                Urls = new Dictionary<CarrierEnums, string>
                {
                    [CarrierEnums.ASI] = "americanstrategic",
                    [CarrierEnums.Homesite] = "homesite",
                    [CarrierEnums.StillWater] = "stillwater",
                    [CarrierEnums.AmericanModern] = "amsuite.amig.com",
                    [CarrierEnums.Foremost] = "foremost",
                    [CarrierEnums.Nationwide] = "nationwide",
                    [CarrierEnums.Openly] = "openly",
                    [CarrierEnums.PlymouthRock] = "plymouthrock",
                    [CarrierEnums.TowerHill] = "thig",
                    [CarrierEnums.Assurant] = "assurant",
                    [CarrierEnums.BambooSurplus] = "b4mb00"

                }
            },
            [Environment.Production] = new CarrierBridgeUrlCollection
            {
                Urls = new Dictionary<CarrierEnums, string>
                {
                    [CarrierEnums.ASI] = "americanstrategic",
                    [CarrierEnums.Homesite] = "homesite",
                    [CarrierEnums.StillWater] = "stillwater",
                    [CarrierEnums.AmericanModern] = "amsuite.amig.com",
                    [CarrierEnums.Foremost] = "foremost",
                    [CarrierEnums.Nationwide] = "nationwide",
                    [CarrierEnums.Openly] = "openly",
                    [CarrierEnums.PlymouthRock] = "plymouthrock",
                    [CarrierEnums.TowerHill] = "thig",
                    [CarrierEnums.Assurant] = "assurant",
                    [CarrierEnums.BambooSurplus] = "b4mb00"

                }
            },
        };
    }
}
