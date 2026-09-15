using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;

namespace Bolt.Automation.FrontEnds.FormData
{
    public class UIElement
    {
        public required LocatorType Strategy { get; set; }
        public required LocatorSet Locators { get; set; }
        public required UIFieldType FieldType { get; set; }
        public required string? DefaultValue { get; set; }
        public required HashSet<Type>? Pages { get; set; } = [];
        public ValidationRules? Validation { get; set; }
        public string? DependsOn { get; set; }
        public string? DependsOnValue { get; set; }
        public bool Required { get; set; }
        public ElementInteractionOptions? InteractionOptions { get; set; }
        public bool PreserveCasing { get; set; }
        public string? CustomErrorMessage { get; set; }
        public string? Version { get; set; }
        public string? Label { get; set; }
        public string? FieldName { get; set; }

        // Indexer for clean syntax: element[value]
        public UIElement this[string value] => new()
        {
            Strategy = Strategy,
            Locators = Locators,
            FieldType = FieldType,
            DefaultValue = value,
            Pages = Pages,
            Validation = Validation,
            DependsOn = DependsOn,
            DependsOnValue = DependsOnValue,
            Required = Required,
            InteractionOptions = InteractionOptions,
            PreserveCasing = PreserveCasing,
            CustomErrorMessage = CustomErrorMessage,
            Version = Version,
            Label = Label,
            FieldName = FieldName
        };

        public static ElementAction GetActionForFieldType(UIFieldType fieldType, string value = "")
        {
            return fieldType switch
            {
                UIFieldType.Input => ElementAction.Fill,
                UIFieldType.Dropdown => ElementAction.Select,
                UIFieldType.MultiDropdown => ElementAction.MultiSelect,
                UIFieldType.Radio => ElementAction.Check,
                UIFieldType.DatePicker => ElementAction.Fill,
                UIFieldType.Checkbox => bool.TryParse(value, out var checkVal) && checkVal
                                       ? ElementAction.Check
                                       : ElementAction.Uncheck,
                UIFieldType.Button => ElementAction.Click,
                UIFieldType.Link =>ElementAction.Click,
                UIFieldType.SearchDropdown => ElementAction.SearchSelect,
                _ => ElementAction.Fill
            };
        }

        public LocatorSet GetLocators(string value = "")
        {
            var resolvedLocators = new LocatorSet();
            foreach (var locator in Locators)
            {
                if (locator.Contains('{') && locator.Contains('}'))
                {
                    var formattedValue = PreserveCasing ? value : value.ToLower();
                    resolvedLocators.Add(string.Format(locator, formattedValue));
                }
                else
                {
                    resolvedLocators.Add(locator);
                }
            }
            return resolvedLocators;
        }
    }
}