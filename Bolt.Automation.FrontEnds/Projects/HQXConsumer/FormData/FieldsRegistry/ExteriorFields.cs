using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

public static class ExteriorFields
{
    public static readonly Dictionary<string, UIElement> Fields = new()
    {
        [PL_AdditionalStructures_Garage] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='PL_AdditionalStructures_Garage']" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "No",
            Pages = []
        },
        [TypeGarageCarport] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='TypeGarageCarport']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [PL_NumberCarSpace] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='PL_NumberCarSpace'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [ExteriorWallsConstruction] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='ExteriorWallsConstruction']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [RoofShape] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='MitRoofShape']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        },
        [RoofType] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "ng-select[name='RoofType']" },
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = []
        }
    };
}
