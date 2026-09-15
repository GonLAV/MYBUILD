using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

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
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PoliciesWithAgent] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'PoliciesWithAgent_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        // Fire safety feature checkboxes
        [SmokeDetector] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='SmokeDetector_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [FireExtinguisher] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='FireExtinguisher_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [SprinklerSystem] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='SprinklerSystem_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [FireDetection] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='FireDetection_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [FireDetectionType_HomeLocal] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='FireDetectionType_HomeLocal_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [FireDetectionType_PhoneAlerts] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='FireDetectionType_PhoneAlerts_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [FireDetectionType_PoliceDirect] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='FireDetectionType_PoliceDirect_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [FireDetectionType_SystemCentral] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='FireDetectionType_SystemCentral_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [DeadBoltLocks] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='DeadBoltLocks_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)],
            Label= "Deadbolts (all exterior doors)"
        },
        [BurglarAlarm] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='BurglarAlarm_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [BurglarAlarmType_HomeLocal] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='BurglarAlarmType_HomeLocal_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [BurglarAlarmType_PhoneAlerts] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='BurglarAlarmType_PhoneAlerts_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [BurglarAlarmType_PoliceDirect] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='BurglarAlarmType_PoliceDirect_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [BurglarAlarmType_SystemCentral] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='BurglarAlarmType_SystemCentral_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [YearsAtAddress] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='YearsAtAddress'][type='text']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "3",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PL_RoofUpdateYearRange] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["ng-select[name='PL_RoofUpdateYearRange']"],
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "0-4 years",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [WindowAndOpeningProtection] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = "ng-select[name='MitWindowOpening']",
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "None",
            Pages = [typeof(HQXConsumer_DiscountsPage)],
            Label = "How much windstorm protection is on your home?"
        },
        [PreviousAddress] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PreviousAddress.AddressLine1']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PreviousAddressUnit] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PreviousAddress.AddressLine2']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PreviousAddressLine1] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PreviousAddress.AddressLine1']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PreviousAddressCity] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PreviousAddress.City']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PreviousAddressState] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["ng-select[name='PreviousAddress.State']"],
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PreviousAddressZip] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PreviousAddress.ZipCode']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PLHighRiseCondo] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PLHighRiseCondo_{0}']"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PL_NumberOfFloors] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input#PL_NumberOfFloors"],
            FieldType = UIFieldType.Input,
            DefaultValue = "1",
            DependsOn = PLHighRiseCondo,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        [PLFloorNumber] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input#PLFloorNumber"],
            FieldType = UIFieldType.Input,
            DefaultValue = "1",
            DependsOn = PLHighRiseCondo,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)]
        },
        // "Utilities replaced" question set (feature flag: ba_utilities-replaced-question).
        // Parent Yes/No radio; selecting Yes reveals the plumbing/heating/electrical child radios.
        [UtilitiesUpdated] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'UtilitiesUpdated_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DiscountsPage)],
            Label = "Has the heating, plumbing or electrical been replaced?"
        },
        // Child radios: values are CompleteUpdate / PartialUpdate / NotUpdated. Revealed only when
        // the parent UtilitiesUpdated is Yes (the wrappers carry aria-hidden/inert until then).
        [PLPlumbingUpdated] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'PLPlumbingUpdated_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "NotUpdated",
            DependsOn = UtilitiesUpdated,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)],
            Label = "Has the plumbing system been replaced?"
        },
        [PLHeatingUpdate] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'PLHeatingUpdate_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "NotUpdated",
            DependsOn = UtilitiesUpdated,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)],
            Label = "Has the heating system been replaced?"
        },
        // Note: DOM id carries the backend's "Electircal" typo — keep the locator verbatim.
        [PLElectircalUpdated] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'PLElectircalUpdated_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "NotUpdated",
            DependsOn = UtilitiesUpdated,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DiscountsPage)],
            Label = "Has the electrical system been replaced?"
        },
    };
}
