using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class InteriorFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [FullBathNum] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#FullBathNum", "input[id='FullBathNum']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_InteriorPage)]
            },
            [HalfBathNum] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#HalfBathNum", "input[id='HalfBathNum']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_InteriorPage)]
            },
            [PL_CentralAC] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_CentralAC')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_InteriorPage), typeof(HQXAgent_CarrierQuestionsPage)],
                Label= "Does your home have central air conditioning?"
            },
            [NumberOfFirePlaces] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#NumberOfFirePlaces", "input[id='NumberOfFirePlaces']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_InteriorPage)]
            },
            [PL_TypeFireplaces] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#PL_TypeFireplaces", "input[id='PL_TypeFireplaces']", "ng-select[name='PL_TypeFireplaces']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_InteriorPage)]
            },
            // "Utilities replaced" question set (feature flag: ba_utilities-replaced-question).
            // Parent Yes/No radio; selecting Yes reveals the plumbing/heating/electrical child radios.
            // TODO(verify-live): confirm the for= id and label against the QA Interior DOM once the flag is on.
            [UtilitiesUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'UtilitiesUpdated_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_InteriorPage)],
                Label = "Has the heating, plumbing or electrical been replaced?"
            },
            // Child radios: values CompleteUpdate / PartialUpdate / NotUpdated. Revealed only when the
            // parent UtilitiesUpdated is Yes (the wrappers carry aria-hidden/inert until then).
            [UtilitiesPlumbingUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PLPlumbingUpdated_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "NotUpdated",
                DependsOn = UtilitiesUpdated,
                DependsOnValue = "true",
                Pages = [typeof(HQXAgent_InteriorPage)],
                Label = "Has the plumbing system been renovated or replaced?"
            },
            [PLHeatingUpdate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PLHeatingUpdate_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "NotUpdated",
                DependsOn = UtilitiesUpdated,
                DependsOnValue = "true",
                Pages = [typeof(HQXAgent_InteriorPage)],
                Label = "Has the heating system been renovated or replaced?"
            },
            [PLElectricalUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PLElectricalUpdated_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "NotUpdated",
                DependsOn = UtilitiesUpdated,
                DependsOnValue = "true",
                Pages = [typeof(HQXAgent_InteriorPage)],
                Label = "Has the electrical system been renovated or replaced?"
            }
        };
    }
}
