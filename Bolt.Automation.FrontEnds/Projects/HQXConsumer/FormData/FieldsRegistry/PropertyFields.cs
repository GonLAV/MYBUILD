using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

public static class PropertyFields
{
    public static readonly Dictionary<string, UIElement> Fields = new()
    {
        [PLTypeOfDwelling] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PLTypeOfDwelling']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "Condominium",
            DependsOn = SingleFamilyHome,
            DependsOnValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [MHArchitectureStyle] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='MHArchitectureStyle']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [PL_NumOfFamilies] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input#PL_NumOfFamilies" },
            FieldType = UIFieldType.Input,
            DefaultValue = "1",
            Pages = []
        },
        [PLYearBuilt] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input#PLYearBuilt" },
            FieldType = UIFieldType.Input,
            DefaultValue = "2000",
            Pages = [typeof(HQXConusmer_3PQ)]
        },
        [PLSquareFootage] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input#PLSquareFootage" },
            FieldType = UIFieldType.Input,
            DefaultValue = "1500",
            Pages = [typeof(HQXConusmer_3PQ)]
        },
        [PLNumberOfStories] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PLNumberOfStories']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [ArchitectureStyle] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='ArchitectureStyle']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "Ranch",
            Pages = [typeof(HQXConusmer_3PQ)]
        },
        [TypeOfFoundation] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='TypeOfFoundation']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [PL_BasementFinish] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PL_BasementFinish']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [BuiltOnSlope] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='BuiltOnSlope']" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "No",
            Pages = []
        },
        [PLConstructionType] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PLConstructionType']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [NumberofAcres] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input#NumberofAcres" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [HomeLength] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input#HomeLength" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [HomeWidth] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input#HomeWidth" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        }
    };
}
