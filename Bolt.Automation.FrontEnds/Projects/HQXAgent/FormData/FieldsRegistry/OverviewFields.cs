using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class OverviewFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [PLYearBuilt] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = ["input[id='PLYearBuilt']"],
                FieldType = UIFieldType.Input,
                DefaultValue = "2024",
                Pages = [typeof(HQXAgent_OverviewPage)],
                Required = true
            },
            [PLSquareFootage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = ["input[id='PLSquareFootage']"],
                FieldType = UIFieldType.Input,
                DefaultValue = "2748",
                Pages = [typeof(HQXAgent_OverviewPage)],
                Required = true
            },
            [RoofResponsible] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input[id='RoofResponsible_{0}']" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_OverviewPage)]
            },
            [ArchitectureStyle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='ArchitectureStyle']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Contemporary",
                Pages = [typeof(HQXAgent_OverviewPage)],
                Required = true
            },
            [MailingAddressDifferent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "//label[contains(@for,'IsMailAddress')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_OverviewPage)]
            },
        };
    }
}
