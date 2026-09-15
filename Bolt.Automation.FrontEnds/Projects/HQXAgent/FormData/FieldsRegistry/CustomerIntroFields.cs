using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class CustomerIntroFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [PL_AddressApproval] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_AddressApproval_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_CustomerIntroPage)]
            },
            [PL_MortgageProperty] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_MortgageProperty_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_CustomerIntroPage)]
            },
            ["PL_ShoppingReason"] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='PL_ShoppingReason']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "First time home buyer",
                Pages = [typeof(HQXAgent_CustomerIntroPage)],
                Required = true
            },
            ["PL_WhatsImportant_LowestRate"] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[@id='LowestRate_label']" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_CustomerIntroPage)],
                Required = true
            },
            [PLTypeOfDwelling] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='PLTypeOfDwelling']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CustomerIntroPage)]
            },
        };
    }
}
