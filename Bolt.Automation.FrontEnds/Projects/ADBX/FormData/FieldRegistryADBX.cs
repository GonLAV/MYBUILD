using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Base;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Cases;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Communications;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Leads;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Policies;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.ADBX.Popups;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.ADBX.FormData.ADBX_FieldNames;
using FrontEndType = Bolt.Automation.Common.Enums.FrontEndType;

namespace Bolt.Automation.FrontEnds.Projects.ADBX.FormData
{
    public static class FieldRegistryADBX
    {
        [FieldRegistry]
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            #region Note
            [NoteSubject] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//select[@formcontrolname='noteSubject']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Other",
                Pages = [typeof(ADBX_AddYourNoteInformationPopUp)],
                Required = true
            },

            [NoteDescription] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//textarea[@formcontrolname='noteText']",
                FieldType = UIFieldType.Input,
                DefaultValue = "some default value",
                Pages = [typeof(ADBX_AddYourNoteInformationPopUp)],
                Required = true
            },
            #endregion

            #region Account
            [AccountSource] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//select[@formcontrolname='source']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Bolt PL",
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountBusinessLine] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[input[@formcontrolname='ConsumerLine' and @id='{0}']]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Personal",
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true,
                PreserveCasing = true
            },

            // Only rendered when AccountBusinessLine=Commercial is selected ("Is the insured a
            // business or an individual?"). DependsOn/DependsOnValue gates the fill so Personal-line
            // account creation - which never reveals this radio - doesn't attempt it. This reveal is a
            // client-side reactive-form toggle (no backend round trip, unlike e.g. Occupation's
            // server-fetched dropdown), but an explicit Timeout still buys real headroom over the
            // 500ms grace IgnoreIfNotFound falls back to when InteractionOptions is left unset.
            [AccountInsuredType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[input[@formcontrolname='businessEntityType' and @id='{0}']]",
                FieldType = UIFieldType.Radio,
                DefaultValue = "Business",
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true,
                PreserveCasing = true,
                DependsOn = AccountBusinessLine,
                DependsOnValue = "Commercial",
                InteractionOptions = new ElementInteractionOptions { Timeout = 5000 }
            },

            [AccountBusinessName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname='businessName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Automation" +RandomManager.GetRandomString(5),
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountFirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname='firstName']",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetFirstName(),
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountLastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname='lastName']",
                FieldType = UIFieldType.Input,
                DefaultValue = NameSelector.GetLastName(),
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountEmail] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname='email']",
                FieldType = UIFieldType.Input,
                DefaultValue = RandomManager.GetRandomEmail(),
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountPhone] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-phone[@formcontrolname='primaryPhoneNumber']/input",
                FieldType = UIFieldType.Input,
                DefaultValue = "52" + RandomManager.GetRandomDigits(8),
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountAddress] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-address-suggestion[@formcontrolname='oneLineAddress']/input",
                FieldType = UIFieldType.Input,
                DefaultValue = "318 Village Cir Winters, CA US 95694",
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
                Required = true
            },

            [AccountSummaryTCPAConsent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "span#tcpa-consent-download-link-tooltip",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_AccountSummaryPage)],
                Required = true
            },

            [EditAccountButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[contains(.,'Edit')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "EDIT",
                Pages = [typeof(ADBX_AccountSummaryPage)],
            },

            [AccountPopupTCPAConsent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "label.tcpa-label ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_EnterUpdateAccountInformationPopup)],
            },
            #endregion

            [NotificationsIcon] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[@data-test-id='all-notifications-header-button']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
                Required = true
            },

            [SideMenuToggle] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.side-menu-toggle",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
                Required = true
            },
            [SideMenuUserSelfEdit] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "button.user-self-edit",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
                Required = true
            },
            [LogoutButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[text()= 'Log Out']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
                Required = true
            },
            [DownloadLink] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//li[1]//a[contains(@class, 'download-link')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
                Required = true
            },
            [NotificationsExportDownloadLink] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//datatable-body-row[.//div[contains(@class,'grid-prop-subject')]//span[normalize-space()='Export File Ready.']]//a[contains(@class,'download-link')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NotificationsPage)],
                Required = true
            },
            [NotificationsExportCreatedDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//datatable-body-row[.//div[contains(@class,'grid-prop-subject')]//span[normalize-space()='Export File Ready.']]//div[contains(@class,'grid-prop-dateCreated')]//span",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(ADBX_NotificationsPage)],
                Required = true
            },

            [LeadsTabSearchResults] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//li/a[contains(text(),'Leads')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            [QuotesTabSearchResults] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//li/a[contains(text(),'Quotes')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            #region PageGrid
            [PageGridSizeDropdown] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "datatable-footer select.page-limit",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "10",
                Pages = [typeof(ADBX_LeadsTabPage)],
            },
            [PageGridSizeMessage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "datatable-footer .page-limit-container span",
                FieldType = UIFieldType.Input,
                DefaultValue = "10",
                Pages = [typeof(ADBX_LeadsTabPage)],
            },
            [PageGridPagination] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "datatable-pager li.pages[aria-label='page {0}']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadsTabPage)],
            },
            [PageGridDownloadButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[@iconname='download']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_DefaultsManagementPage)],
            },
            [PageGridRefreshButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[@iconname='refresh']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_DefaultsManagementPage)],
            },
            [LeadsGridFilterBySource] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = ".search-container app-dropdown:nth-of-type(1) ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadsTabPage)],
            },
            [LeadsGridFilterByProduct] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = ".search-container app-dropdown:nth-of-type(2) ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadsTabPage)],
            },
            [LeadsGridFilterSourceClear] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = ".search-container app-dropdown:nth-of-type(1) ng-select .ng-value-icon.right",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadsTabPage)],
            },
            #endregion
            #region QuoteSummary
            [AddPolicyButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = ["//button[contains(@class,'add-policy')]", "//span[contains(text(), 'NEW POLICY')]"],
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_QuoteSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [EditButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[@iconname='edit']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_QuoteSummaryPage)],
            },
            [QuoteSummaryCancelButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(text(),'Cancel')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_QuoteSummaryPage)],
            },
            #endregion

            #region Policy
            // A native <select>, so HandleSelectDropdown matches on the visible label, not the option
            // value. The commercial popup's labels are "Business Owners Policy", "Commercial Auto",
            // "Workers Compensation", ... - use LobType.<x>.ToToken(), which already evaluates to the
            // label. The personal-lines default below is not among them, so a commercial caller that
            // leaves it in place gets a bare dropdown timeout rather than an "option not found".
            [PolicyProduct] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label/div[contains(text(),'Product')]//select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Homeowners",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyPackageProducts] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label/div[contains(text(),'Package')]//ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "Homeowners",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)]
            },
            [PolicyCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label/div[contains(text(),'Carrier')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Bamboo",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Policy Number')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "RenewTest" + RandomManager.GetRandomString(2)+ RandomManager.GetRandomDigits(2),
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyRenewal] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label/div[contains(text(),'Renewal')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyEffectiveDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Effective')]//app-datepicker/input/following-sibling::input",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Today.ToString("MM-dd-yyyy"),
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyTerm] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(text(), '{0}')]/preceding-sibling::input",
                FieldType = UIFieldType.Radio,
                DefaultValue = "6 months",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
            },
            [PolicyExpirationDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Expiration')]//app-datepicker/input/following-sibling::input",
                FieldType = UIFieldType.Input,
                DefaultValue = DateTime.Today.AddYears(1).ToString("MM-dd-yyyy"),
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyBillingMethod] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label/div[contains(text(),'Billing Method')]//select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Agency Bill",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyPremium] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Premium')]//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "100",
                Pages = [typeof(ADBX_AddPolicyInformationPopUp)],
                Required = true
            },
            [PolicyRequoteButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(.,'REQUOTE')]",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = [typeof(ADBX_RenewalsTabPage)],
            },
            [RenewalsRefreshIcon] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//span[contains(@class,'refresh')]",
                FieldType = UIFieldType.Button,
                DefaultValue = string.Empty,
                Pages = [typeof(ADBX_RenewalsTabPage)],
            },
            #endregion
            #region Lead
            [LeadMoreOptionsButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[contains(., 'MORE OPTIONS')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadSummaryPage)],
            },

            [LeadEditContactInfoButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//span[text()='EDIT CONTACT INFO']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadSummaryPage)],
            },

            [LeadSummaryTCPAConsent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "span#tcpa-consent-download-link-tooltip",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadSummaryPage)],
                Required = true
            },
            #endregion
            #region User Self edit
            [UserSelfEditFirstName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname = 'firstName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Andrii" + RandomManager.GetRandomString(7),
                Pages = [typeof(ADBX_UserSelfEditPopUp)],
                Required = true
            },
            [UserSelfEditLastName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname = 'lastName']",
                FieldType = UIFieldType.Input,
                DefaultValue = "Test" + RandomManager.GetRandomString(6),
                Pages = [typeof(ADBX_UserSelfEditPopUp)],
                Required = true
            },
            [UserSelfEditTimeZone] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//select[@formcontrolname = 'timeZone']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Eastern Time (ET)",
                Pages = [typeof(ADBX_UserSelfEditPopUp)],
                Required = true
            },
            #endregion

            [EditCaseButton] = new UIElement
            {
                // locator depends on feature flag temp-enable-ba-placement-servicecase
                Strategy = LocatorType.XPath,
                Locators = "//app-button[contains(., 'Edit Case')] | //app-button[contains(., 'Edit Service Request')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage)],
            },
            [QueueTabs] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-tabs//button//span[contains(normalize-space(.),'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
                PreserveCasing = true
            },
            [LeadCommunicationsTabs] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-communications-new//button/div[contains(@class,'{0}')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadSummaryPage)],
                PreserveCasing = true
            },
            [CaseStageDropdown] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//select[@formcontrolname='stage']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_EditCasePopup)],
            },

            [KickoutErrorMessage] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-kickout-page p.subtitle",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [],
                Required = true
            },
            [FreeText] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div/textarea",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage), typeof(ADBX_NewCommunicationsSmsTemplatePage), typeof(ADBX_NewCommunicationsEmailTemplatePage)],
                Required = true,
                Validation = new ValidationRules
                {
                    Locator = "label[appformitem]:has(textarea[formcontrolname='templateBody']) .form-item-sub .ng-star-inserted",
                }
            },
            [AddFile] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "input[type='file']",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
                Required = true
            },
            [AddViewButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-button#create-view-btn",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CasesTabPage), typeof(ADBX_LeadsTabPage)],
                Required = true
            },
            #region EmailSmsTemplates
            [EmailTemplateList] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//select[@formcontrolname='emailTemplateId']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [SmsTemplateList] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//select[@formcontrolname='textMessageTemplateId']",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [SmsContent] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-sms-form textarea[formcontrolname='content']",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [SmsSendButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-sms-form button.submit.btn-primary",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [SmsNotificationsCountBadge] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[data-test-id='sms-notifications-header-button'] span.notifications-count-badge",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [],
            },
            [SmsNotificationsButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "[data-test-id='sms-notifications-header-button']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            [SmsNotificationsDropdown] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.notification.active",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [],
            },
            [SmsNotificationsFirstItemTimestamp] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.notification.active ul li:first-child div.date-time",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [],
            },
            [SmsNotificationsFirstItemSubtitle] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.notification.active ul li:first-child div.subtitle",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [],
            },
            [SeeAllSmsNotificationsLink] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.notification.active div.go-to-notifications",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [],
            },
            [EmailRecipient] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-emails-input//input",
                FieldType = UIFieldType.Input,
                DefaultValue = "boltautomation@boltinc.com",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [SendEmailButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-email-form button.submit.btn-primary",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CaseSummaryPage), typeof(ADBX_LeadSummaryPage)],
            },
            [NewMessageTemplateButton] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "app-button button:has-text('New Text Message Template')",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [TemplateName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//input[@formcontrolname='name']",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
                Validation = new ValidationRules
                {
                    Locator = "label[appformitem]:has(input[formcontrolname='name']) .form-item-sub .ng-star-inserted",
                }
            },
            [TemplateLineOfBusiness] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-dropdown[@formcontrolname='line']//ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "Personal",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
                Validation = new ValidationRules
                {
                  Locator= "label[appformitem]:has(app-dropdown[formcontrolname='line']) .form-item-sub .ng-star-inserted",
                }
            },
            [TemplateUseType] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-dropdown[@formcontrolname='useType']//ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "Lead",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
                Validation = new ValidationRules
                {
                    Locator = "label[appformitem]:has(app-dropdown[formcontrolname='useType']) .form-item-sub .ng-star-inserted",
                }
            },
            [VariableAgentEmail] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Agent Email')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableAgentFullName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Agent Full Name')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableAgentPhone] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Agent Phone')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableBusinessName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Business Name')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableCarrierName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Carrier Name')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableCompanyWorkPhone] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Company(work) Phone')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableCustomerFullName] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Customer FullName')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableDateToday] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Date (Today)')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableEffectiveDate] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Effective Date')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableLob] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'LOB')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariablePremium] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'Premium')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [VariableQsNumber] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-variable-list//button[.//span[contains(@class,'first')][contains(normalize-space(.),'QS#')]]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [TemplateActiveToggle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-slide-toggle[@formcontrolname='isActive']//button[@role='switch'][@aria-checked='true']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [TemplateAddGroupButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button//span[contains(text(),'ADD GROUP')] | //button[text()='Add Group']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage), typeof(ADBX_CommunicationsEmailTemplatePage)],
            },
            [TemplateSelectGroup] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(text(),'Groups')]//app-dropdown//ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_CommunicationsTemplateAddGroupPopUp)],
            },
            [TemplateCreateButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(text(),'Create')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_NewCommunicationsSmsTemplatePage)],
            },
            [TemplateConfirmButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(text(),'Confirm')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CommunicationsEmailTemplatePage)],
            },
            [TemplateGroupsConfirmButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[@id='addGroupConfirmPopUp']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_CommunicationsEmailTemplatePage)],
            },
            [TemplateCancelButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//button[contains(text(),'Cancel')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_EditCommunicationsSmsTemplatePage), typeof(ADBX_EditCommunicationsEmailTemplatePage)],
            },
            #endregion
            #region Defaults
            [DefaultsGridSearchButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[contains(., 'Search')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            [DefaultsGridDownloadButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[contains(., 'Download')]",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            [DefaultsGridAddButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-button[@text='Add']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [],
            },
            [DefaultsPopupSourceDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-autocomplete[@placeholder='Source']/ng-select",
                FieldType = UIFieldType.SearchDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddDefaultsRecordPopup)],
                InteractionOptions = new ElementInteractionOptions
                {
                    PressTab = false
                }
            },
            [DefaultsPopupFlowDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(text(),'Flow')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddDefaultsRecordPopup)],
            },
            [DefaultsPopupLobDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(text(),'LOB')]//ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddDefaultsRecordPopup)],
                InteractionOptions = new ElementInteractionOptions
                {
                    PressTab = true
                }
            },
            [DefaultsPopupStateDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(text(),'State')]//ng-select",
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddDefaultsRecordPopup)],
                InteractionOptions = new ElementInteractionOptions
                {
                    PressTab = true
                }
            },
            [DefaultsPopupFieldDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//app-autocomplete[@placeholder='Field']/ng-select",
                FieldType = UIFieldType.SearchDropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddDefaultsRecordPopup)],
                InteractionOptions = new ElementInteractionOptions
                {
                    PressTab = false
                }
            },
            [DefaultsPopupValueDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//label[contains(text(),'Value')]//ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_AddDefaultsRecordPopup)],
            },
            [DefaultsGridSourceDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Source')]/../../../../ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_DefaultsManagementPage)],
            },
            [DefaultsGridFieldDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'Field')]/../../../../ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_DefaultsManagementPage)],
            },
            [DefaultsGridLobDrodpodwn] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(text(),'LOB')]/../../../../ng-select",
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(ADBX_DefaultsManagementPage)],
            },
            #endregion

            [CommercialLinesTab] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "li#tab-CL button.tab-button",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_AccountsTabPage)],
            },

            [PageTitle] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "//div[contains(@class,'page-title')]//h1",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [typeof(ADBX_PolicySummaryPage), typeof(ADBX_QuoteSummaryPage), typeof(ADBX_CaseSummaryPage)],
            },

            #region Lead Sources
            // A bare reactive-form input with no submit - the grid filters as you type. Searching is
            // what lets a test name the source it wants: lead source ids differ per environment, and
            // on a hub with more sources than fit a page the row is otherwise not on screen at all.
            [LeadSourcesSearch] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.search-container input.search-field",
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadsSourcesPage)],
            },

            // Name and AMS short name are both plain cell text, so :has-text on the row matches either -
            // the caller names the source without having to know which column carries the name.
            [LeadSourcesGridEditIcon] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "datatable-body-row:has-text(\"{0}\") .edit-icon",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_LeadsSourcesPage)],
                PreserveCasing = true
            },

            // The Consumer Journeys control carries no formcontrolname, and the form renders a second
            // identical ng-select ("Share Data With"), so the visible label is the only stable anchor.
            [LeadSourceConsumerJourneys] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.form-item-container:has-text(\"Consumer Journeys\") ng-select",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            // Opening the picker must never click the ng-select itself: Playwright clicks an element's
            // centre, and once chips fill the value container that centre can land on a chip's remove
            // icon, silently detaching a journey the test believes is still attached. ng-select's own
            // chevron is not an option - the app hides it (.ng-arrow-wrapper { display: none }). The
            // search input is: in multiple mode it is flex:1 with z-index 2, so it always covers the
            // space the chips do not, and clicking it opens the panel.
            [LeadSourceJourneysSearchInput] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.form-item-container:has-text(\"Consumer Journeys\") .ng-input input",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            // ng-select adds .ng-has-value only once the bound journeys arrive, which is the one signal
            // that separates "this source has no journeys" from "they have not loaded yet".
            [LeadSourceJourneysLoaded] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.form-item-container:has-text(\"Consumer Journeys\") .ng-select-container.ng-has-value",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            // ng-select renders its panel outside the field's own container, so this is unscoped; only
            // one picker can be open at a time.
            [LeadSourceJourneyPicker] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-dropdown-panel",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            // Journey labels are "{Description} {Journey Type}" (e.g. "Homeowners Farmers"), and
            // "Homeowners Farmers" is a prefix of nothing else only by luck - :text-is keeps the match
            // exact so a future "Homeowners Farmers Plus" cannot silently satisfy it.
            [LeadSourceJourneyOption] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "ng-dropdown-panel div.ng-option:has(span.ng-option-label:text-is(\"{0}\"))",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
                PreserveCasing = true
            },

            [LeadSourceJourneyChips] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.form-item-container:has-text(\"Consumer Journeys\") .ng-value-label",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            // The clear-all control is the app-clearable-form-control reset button, rendered inside the
            // field on its left - not ng-select's own .ng-clear-wrapper.
            [LeadSourceJourneysClearAll] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.form-item-container:has-text(\"Consumer Journeys\") button.reset-input-btn",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            [LeadSourceJourneysWarning] = new UIElement
            {
                Strategy = LocatorType.CSS,
                Locators = "div.form-item-container:has-text(\"Consumer Journeys\") span.warning",
                FieldType = UIFieldType.Link,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },

            // The form nests child components (partner-portal setup, media kit) that each render their
            // own .modal-footer with a Confirm/Cancel pair. The lead source's own footer is the first
            // in document order, so index rather than component id - the latter is a build-generated
            // _ngcontent attribute and changes on every rebuild.
            [LeadSourceCancelButton] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = "(//app-lead-source//div[contains(@class,'modal-footer')])[1]//button[normalize-space()='Cancel']",
                FieldType = UIFieldType.Button,
                DefaultValue = "",
                Pages = [typeof(ADBX_UpdateLeadSourcePage)],
            },
            #endregion

        };

        static FieldRegistryADBX() => FieldRegistryProvider.Register(FrontEndType.ADBX, Fields);
    }
}
