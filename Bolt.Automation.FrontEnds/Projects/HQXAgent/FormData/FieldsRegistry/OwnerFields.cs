using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class OwnerFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [FirstName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#FirstName", "input[id='FirstName']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [MiddleName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#MiddleName", "input[id='MiddleName']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [LastName] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#LastName", "input[id='LastName']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [DateOfBirth] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#DateOfBirth", "input[id='DateOfBirth']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [PrimaryPhoneNumber] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#PrimaryPhoneNumber", "input[id='PrimaryPhoneNumber']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [Email] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#Email", "input[id='Email']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [MaritalStatus] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#MaritalStatus", "input[id='MaritalStatus']", "ng-select[name='MaritalStatus']" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [AnyAdditionalInsured] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'AnyAdditionalInsured')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [BusinessOnResidencePremises] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'BusinessOnResidencePremises')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [AnimalsOnThePremises_None] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'AnimalsOnThePremises_None')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "false",
                Pages = [typeof(HQXAgent_OwnerPage)]
            },
            [NumberOfDogsOnPremises] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "#NumberOfDogsOnPremises", "input[id='NumberOfDogsOnPremises']" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_OwnerPage)]
            }
        };
    }
}
