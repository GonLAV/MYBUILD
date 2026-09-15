using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class DiscountsFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [HomeAutoInsurance] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = ["//label[contains(@for,'HomeAutoInsurance_{0}')]"],
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            ["BundelingAutoPolicyNum"] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#BundelingAutoPolicyNum", "input[id='BundelingAutoPolicyNum']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            [SmokeDetector] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#SmokeDetector", "input[id='SmokeDetector'][type='checkbox']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            [FireExtinguisher] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#FireExtinguisher", "input[id='FireExtinguisher'][type='checkbox']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            [SprinklerSystem] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#SprinklerSystem", "input[id='SprinklerSystem'][type='checkbox']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            [FireDetection] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#FireDetection", "input[id='FireDetection'][type='checkbox']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            ["SprinklerSystemType_Full"] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#SprinklerSystemType_Full", "input[id='SprinklerSystemType_Full'][type='radio']" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "Full",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            [DeadBoltLocks] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#DeadBoltLocks", "input[id='DeadBoltLocks'][type='checkbox']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
            [BurglarAlarm] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#BurglarAlarm", "input[id='BurglarAlarm'][type='checkbox']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_DiscountsPage)]
            },
        };
    }
}
