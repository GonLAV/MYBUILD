using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;

namespace Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldsRegistry
{
    public static class CarrierQuestionsFields
    {
        public static readonly Dictionary<string, UIElement> Fields = new()
        {
            [PL_Bankruptcy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_Bankruptcy_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions= new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label= "In the last 5 years have you filed for bankruptcy or currently in the process of bankruptcy?"
            },
            [InterestedInFloodQuote] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'PL_FloodOffer_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label= "Is customer interested in flood quote?"
            },
            [DealershipPurchase] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'DealershipPurchase_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "true",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Did you purchase your home directly from a dealership?"
            },
            [BuiltOnSlope] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Is your home built on a slope?"
            },
            [PerimeterSecurityDD] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Does your home have perimeter fencing?"
            },
            [PL_CeilingHeight] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "What is the ceiling height for the majority of rooms in the home?"
            },
            [PL_VaultedCeilings] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many rooms have cathedral/vaulted ceilings?"
            },
            [PL_CrownMolding] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many rooms have crown molding?"
            },
            [PL_InteriorWallMaterial] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "What type of interior walls are in this home?"
            },
            [PL_FlooringMaterial] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.MultiDropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "What type of flooring is in this home?"
            },
            [PL_PrimaryCounterMaterial] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//input[@id='PL_PrimaryCounterMaterial']/ancestor::ng-select" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "Concrete",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage), typeof(HQXConsumer_DiscountsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "What is the primary kitchen counter material?"
            },
            [ResHeldTrust] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Is the property required to be in the name of a Trustee, Estate, LLC or LLP listed on the deed for the home?"
            },
            [ResHeldTrust_2] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Is the property in a land trust or required to be in the name of an estate?"
            },
            [PL_Houseoccup] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many people will be living in the home?"
            },
            [GatedOrLimited] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Is your home in a gated or guarded community?"
            },
            [PurchasePrice] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "What was the purchase price of your home?"
            },
            [NumberOfMortgagees] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many mortgages do you have on the property?"
            },
            [CurrentPersonalHomeownerCarrier] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Who is your current property insurance carrier?"
            },
            [PropertyInsuranceCancelled] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Has your property insurance been cancelled, declined or non renewed in the last 5 years?"
            },
            [PropertyInsuranceCancelled_2] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Have you had similar insurance declined, cancelled or nonrenewed?"
            },
            [CurrentlyOnBankruptcy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Currently in bankruptcy"
            },
            [PastBankruptcy] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Past bankruptcy"
            },
            [PLAllPerilsDeductible] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "All Perils Deductible"
            },
            [PLPersonalLiability] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Dropdown,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Liability Limit"
            },
            [InsuranceFraud] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Have you been convicted of insurance fraud in the last 10 years?"
            },
            [YearsWithPriorCarrierHome] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many years have you been with your current carrier?"
            },
            [NumberofAcres] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many acres is this property?"
            },
            [NonSmoker] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Do any of the residents smoke?"
            },
            [NumberOfChildren] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "How many children under the age of 21 live in the household?"
            },
            [RoofOver24] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Is your roof over 24 years old?"
            },
            [PLPlumbingUpdated] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Has the plumbing system been renovated or replaced?"
            },
            [PlumbingUpdatedYear] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "" },
                FieldType = UIFieldType.Input,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "What year was the plumbing system replaced?"
            },
            [PL_AdditionalStructures_Deck] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = ["label[for='PL_AdditionalStructures_Deck_label']"],
                FieldType = UIFieldType.Checkbox,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                Label = "Does your home have a deck?"
            },
            [FuelTanksBelowGround] = new UIElement
            {
                Strategy = LocatorType.XPath,
                Locators = new[] { "//label[contains(@for,'FuelTanksBelowGround_{0}')]" },
                FieldType = UIFieldType.Radio,
                DefaultValue = "",
                Pages = [typeof(HQXAgent_CarrierQuestionsPage)],
                InteractionOptions = new ElementInteractionOptions { IgnoreIfNotFound = true },
                Label = "Does the property have any fuel tanks (for kerosene, oil, propane) located below ground?"
            },
        };
    }
}
