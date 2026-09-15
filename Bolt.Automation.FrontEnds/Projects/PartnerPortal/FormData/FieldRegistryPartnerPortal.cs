using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.PartnerPortal.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData.PartnerPortal_FieldNames;

namespace Bolt.Automation.FrontEnds.Projects.PartnerPortal.FormData
{
    public static class FieldRegistryPartnerPortal
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            #region HomePageDetails
            [FirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='firstName']",
                FieldType = UIFieldType.Input,
                DefaultValue= NameSelector.GetFirstName(),
                Pages = [typeof(PartnerPortal_HomePage)],
                Required = true,
            },
            [LastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='lastName']",
                FieldType = UIFieldType.Input,
                DefaultValue= NameSelector.GetLastName(),
                Pages = [typeof(PartnerPortal_HomePage)],
                Required = true,
            },
            [NotificationsIcon] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(@class,'notification')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(PartnerPortal_HomePage)],
            },
            [ReferralsCount] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Referrals')]/preceding-sibling::div",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [],
            },
            [PoliciesCount] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Policies')]/preceding-sibling::div",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [],
            },
            [CopyLink] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[@class='copy-link']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            [ConsumerFlowUrl] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[@class='generate-url']/p",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [],
            },
            #endregion
            #region InviteDetails
            [OnlineAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='address_suggestion']",
                FieldType = UIFieldType.Input,
                DefaultValue = "318 Village Cir, Winters, CA 95694",
                Pages = new HashSet<Type> { typeof(PartnerPortal_InvitePage) },
            },
            [PrimaryPhoneNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='PhoneNumber']",
                FieldType = UIFieldType.Input,
                DefaultValue = 52 + RandomManager.GetRandomDigits(8),
                Pages = new HashSet<Type> { typeof(PartnerPortal_InvitePage) },

            },
            [Email] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='EmailAddress']",
                FieldType = UIFieldType.Input,
                DefaultValue = "boltautomation@boltinc.com",
                Pages = new HashSet<Type> { typeof(PartnerPortal_InvitePage) },

            },
            [Lob] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[normalize-space(text())='{0}']/span",
                FieldType = UIFieldType.Button,
                DefaultValue = "Personal Auto",
                PreserveCasing = true,
                Pages = new HashSet<Type> { typeof(PartnerPortal_InvitePage) },

            },
            #endregion
            #region CommonLocators
            [NextButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = new[] { "button#sendInviteButton.btn.btn-secondary", "button.btn.btn-primary" },
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),

            },
            [HomePageButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//a[contains(@href,'home')]",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),
            },
            [RecentSearch] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@id='search-users-input']",
                FieldType = UIFieldType.Input,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),
            },
            [ReferralsButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(@class,'value-content') and normalize-space()='Referrals']",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),
            },
            [QuotesButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(@class,'value-content') and normalize-space()='Quotes']",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),
            },
            [PoliciesButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(@class,'value-content') and normalize-space()='Policies']",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),
            },
            [EnvelopeIcon] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-nav-item[text='Invites'] a",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = new HashSet<Type>(),
            },
            #endregion
        };
        static FieldRegistryPartnerPortal() => FieldRegistryProvider.Register(FrontEndType.PartnerPortal, Fields);

    }
}
