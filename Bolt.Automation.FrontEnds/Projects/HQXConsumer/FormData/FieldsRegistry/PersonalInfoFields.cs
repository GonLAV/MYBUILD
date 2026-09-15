using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

public static class PersonalInfoFields
{
    public static readonly Dictionary<string, UIElement> Fields = new()
    {
        [FirstName] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='FirstName'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [MiddleName] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='MiddleName'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [LastName] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='LastName'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [DateOfBirth] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "input[name='DateOfBirth'][type='text'].unmasked-input" },
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = []
        },
        [AnyAdditionalInsured] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//label[contains(@for,'AnyAdditionalInsured_{0}')]" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [EffectiveDate] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = "//input[@id='EffectiveDate']",
            FieldType = UIFieldType.Input,
            DefaultValue = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy"),
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddressDifferent] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='MailingAddressDifferent_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddress] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='MailingAddress.AddressLine1']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddressUnit] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='MailingAddress.AddressLine2']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddressLine1] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='MailingAddressLine1']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddressCity] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='MailingAddressCity']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddressState] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["ng-select[name='MailingAddressState']", "input[name='MailingAddressState']"],
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [MailingAddressZip] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='MailingAddressZip']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [PLPersonalProperty] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[id='PLPersonalProperty']"],
            FieldType = UIFieldType.Input,
            DefaultValue = "50000",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
    };
}
