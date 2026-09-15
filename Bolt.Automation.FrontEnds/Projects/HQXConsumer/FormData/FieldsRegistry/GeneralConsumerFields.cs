using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

public static class GeneralConsumerFields
{
    public static readonly Dictionary<string, UIElement> Fields = new()
    {
        [NextButton] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "button.next-button" },
            FieldType = UIFieldType.Button,
            DefaultValue = string.Empty,
            Pages = []
        },
        [PopupCloseButton] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "#popupCloseButton" },
            FieldType = UIFieldType.Button,
            DefaultValue = string.Empty,
            Pages = []
        },
        [EnterAddressManuallyLink] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["button[aria-label=\"Enter it manually.\"]"],
            FieldType = UIFieldType.Button,
            DefaultValue = null,
            Pages = []
        },
        // Sits directly below the Continue button on every interview stage. Pages = [] keeps it out of
        // the FillForm sweep — a link that abandons the quote must never be clicked incidentally.
        [ExitToAutoQuoteLink] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["a.text:has-text(\"see auto rate now\")", "a.text:has-text(\"Exit home quote\")"],
            FieldType = UIFieldType.Link,
            DefaultValue = string.Empty,
            Pages = []
        },
        [CCPALink] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[]
            { "a.link[href*='do-not-sell-my-information']" },
            FieldType = UIFieldType.Link,
            DefaultValue = null,
            Pages = null
        },
        [CANoticeLink] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[]
            { "a.link[href*='privacy-data-request']" },
            FieldType = UIFieldType.Link,
            DefaultValue = null,
            Pages = null
        },
        [ProgressivePreferences1] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//label[contains(@for,'Progressive_Preferences1_{0}')]" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "12",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        [ProgressivePreferences2] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//label[contains(@for,'Progressive_Preferences2_{0}')]" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "10",
            Pages = [typeof(HQXConsumer_OwnerPage)]
        },
        // Coverage title elements (span inside .coverage-title or .info-title)
        ["CoverageTitles"] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { ".coverage-title", ".info-title span" },
            FieldType = UIFieldType.Link, // treated as read-only text
            DefaultValue = null,
            Pages = null
        },
        // Learn more buttons adjacent to coverage titles
        ["CoverageLearnMoreButtons"] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "button.link-label" },
            FieldType = UIFieldType.Button,
            DefaultValue = string.Empty,
            Pages = null
        },
        // Generic overlay content selector for tooltip dialogs
        ["CoverageTooltipContent"] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { ".cdk-overlay-container .mat-mdc-dialog-content", ".cdk-overlay-container .dialog-content", ".cdk-overlay-container .content" },
            FieldType = UIFieldType.Link,
            DefaultValue = null,
            Pages = null
        }
    };
}
