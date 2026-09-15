using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

public static class InteriorFields
{
    public static readonly Dictionary<string, UIElement> Fields = new()
    {
        [FullBathNum] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='FullBathNum'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
            //Pages = []
        },
        [HalfBathNum] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='HalfBathNum'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
            //Pages = []
        },
        [PL_FlooringMaterial] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PL_FlooringMaterial[]']" },
            FieldType = UIFieldType.MultiDropdown,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OverviewPage), typeof(HQXConsumer_DiscountsPage)],
            Label = "What type of flooring is in this home?"
        },
        [PL_InteriorWallMaterial] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PL_InteriorWallMaterial[]']" },
            FieldType = UIFieldType.MultiDropdown,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OverviewPage), typeof(HQXConsumer_DiscountsPage)],
            Label = "What type of interior walls are in this home?"
        },
        [NumberOfFirePlaces] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='NumberOfFirePlaces'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OverviewPage)],
            Label = "Number of fireplaces in this home?"
        },
        [PL_TypeFireplaces] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PL_TypeFireplaces']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "Gas",
            Pages = []
            //Pages = []
        },
        [PL_PrimaryCounterMaterial] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//input[@id='PL_PrimaryCounterMaterial']/ancestor::ng-select" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "Concrete",
            Pages = [typeof(HQXConsumer_DiscountsPage), typeof(HQXConsumer_InteriorPage)],
        },
        [PL_CentralAC] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='PL_CentralAC']" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "No",
            Pages = []
            //Pages = []
        },
        [PLHeatingType] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='PLHeatingType']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
            //Pages = []
        }
    };
}
