using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Popups;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class NoteFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            // ── Initial modal ──────────────────────────────────────────────────────────

            [NoteType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'NoteType_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "NewNote",
                PreserveCasing = true,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [ActionValue] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='ActionValue']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [CallProductType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='CallProductType']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [ActivityDescription] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "textarea#ActivityDescription" },
                FieldType = UIFieldType.Input,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },

            // ── Sold Note modal ────────────────────────────────────────────────────────

            [QuoteOwner] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'QuoteOwner_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                PreserveCasing = true,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [SelectedAgentNote] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='SelectedAgent']/ancestor::ng-select" },
                FieldType = UIFieldType.SearchDropdown,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [PolicyNumberNote] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input#PolicyNumber" },
                FieldType = UIFieldType.Input,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)],
                Validation = new ValidationRules
                {
                    Locator = "#PolicyNumber_error p"
                }
            },
            [EffectiveDateNote] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input.masked-datepicker[id='EffectiveDate']" },
                FieldType = UIFieldType.DatePicker,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [ParentCompany] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='ParentCompany']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
            [PremiumNote] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "input#Premium" },
                FieldType = UIFieldType.Input,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_CreateNotePopup)]
            },
        };
    }
}
