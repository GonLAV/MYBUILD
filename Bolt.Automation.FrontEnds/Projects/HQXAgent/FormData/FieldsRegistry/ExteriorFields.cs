using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class ExteriorFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [PLConstructionType] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#PLConstructionType", "input[id='PLConstructionType']", "ng-select[name='PLConstructionType']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_ExteriorPage)]
            },
            [RoofShape] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#MitRoofShape", "input[id='MitRoofShape']", "ng-select[name='MitRoofShape']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_ExteriorPage), typeof(HQXAgent_CarrierQuestionsPage)],
                Label= "What is the shape of your roof?"
            },
            [RoofType] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#RoofType", "input[id='RoofType']", "ng-select[name='RoofType']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_ExteriorPage), typeof(HQXAgent_CarrierQuestionsPage)],
                Label = "What material is your roof made of?"
            },
            [ExteriorWallsConstruction] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#ExteriorWallsConstruction", "input[id='ExteriorWallsConstruction']", "ng-select[name='ExteriorWallsConstruction']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_ExteriorPage)]
            },
            [PL_AdditionalStructures_Garage] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_AdditionalStructures_Garage')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_ExteriorPage)]
            },
            [PL_AdditionalStructures_Pool] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_AdditionalStructures_Pool')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_ExteriorPage)]
            },
            [PL_AdditionalStructures_Trampoline] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_AdditionalStructures_Trampoline')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_ExteriorPage)]
            },
            ["EligibilityFinancialHardship"] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'EligibilityFinancialHardship')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_ExteriorPage)]
            },
        };
    }
}
