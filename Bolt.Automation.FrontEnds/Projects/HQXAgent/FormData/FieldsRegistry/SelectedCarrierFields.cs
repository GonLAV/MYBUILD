using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class SelectedCarrierFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [ContinueToFullQuote] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_ContinueToFullQuote_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_SelectedCarrierPage)],
            },
            [SelectedCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//table[contains(@class,'desktop')]//*[contains(@id,'carrier-dt-{0}') or contains(@aria-label,'select {0}')]" },
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = [typeof(HQXAgent_SelectedCarrierPage)],
                PreserveCasing = true
            },
            // Get E&S / Get DF rate buttons are only ever driven out-of-band — clicked
            // deliberately (ClickGetESRatesButton) or checked via ElementExists. They are
            // NOT FillForm targets: tagging them to the page would sweep them into
            // GetSmartFormData and (being Buttons) click them on every FillForm pass
            // whenever present. Untagged (Pages = []) so the Fields[...] lookups still
            // resolve while FillForm skips them. See troubleshooting:default-reinjected.
            [GetESRates] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "button.get-es-rates-btn" },
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = [],
            },
            [GetDFRates] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "button.get-df-rates-btn" },
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = [],
            },

        };
    }
}
