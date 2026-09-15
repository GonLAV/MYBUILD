using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class TriageFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [PriorInsuranceProperty] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PriorInsuranceProperty_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_TriagePage)]
            },
            ["PL_LivingTime"] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='PL_LivingTime']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "9-12 months",
                Pages = [typeof(HQXAgent_TriagePage)]
            },
            [YearsAtAddress] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[id='YearsAtAddress'][type='text']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "3",
                Pages = [typeof(HQXAgent_TriagePage)]
            },
            [EffectiveDate] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[id='EffectiveDate'][type='tel']" },
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy"),
                Pages = [typeof(HQXAgent_TriagePage), typeof(HQXAgent_CustomerIntroPage)]
            },
            [ShortTermRental] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'ShortTermRental_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_TriagePage)]
            },
            [PersonalLineReplacementCost] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[id='PersonalLineReplacementCost'][type='text']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_TriagePage)]
            }
        };
    }
}
