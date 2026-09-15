using Bolt.Automation.FrontEnds.FormData;
using Bolt.Automation.FrontEnds.FormData.Enums;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using static Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsNameHQXConsumer;

namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;

public static class DetailsFields
{
    public static readonly Dictionary<string, UIElement> Fields = new()
    {
        [IsPrimaryResidence] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'PrimaryHome_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "true",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [SingleFamilyHome] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = new[] { "label[for='SingleFamilyHome_{0}']" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        // An ng-select here, unlike D2C's radio of the same name.
        [DwellingUsage] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["ng-select[name='DwellingUsage']"],
            FieldType = UIFieldType.Dropdown,
            DefaultValue = "",
            Label = "What is your home type?",
            DependsOn = IsPrimaryResidence,
            DependsOnValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [ShortTermRental] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='ShortTermRental_{0}']"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "false",
            Label = "Is the property rented on a weekly or short term basis?",
            DependsOn = DwellingUsage,
            DependsOnValue = "Rental",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },

        [PriorInsuranceProperty] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//label[contains(@for,'PriorInsuranceProperty_{0}')]" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "true",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [YearsWithPriorCarrierHome] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//input[@id='YearsWithPriorCarrierHome']" },
            FieldType = UIFieldType.Input,
            DefaultValue = "2",
            Pages = [typeof(HQXConsumer_DetailsPage)],
            DependsOn= PriorInsuranceProperty,
            InteractionOptions = new ElementInteractionOptions { PressTab = false }
        },
        [NumberOfClaimsHistoryYesNo] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = new[] { "//label[contains(@for,'NumberOfClaimsHistoryYesNo_{0}')]" },
            FieldType = UIFieldType.Radio,
            DefaultValue = "0",
            Pages = [typeof(HQXConsumer_DetailsPage)],
        },
        [PL_AdditionalStructures_Deck] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_AdditionalStructures_Deck_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [RoofResponsible] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='RoofResponsible_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Label = "The exterior of this home is insured by someone else",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [BusinessOnResidencePremises] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='BusinessOnResidencePremises_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [EligibilityFinancialHardship] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='EligibilityFinancialHardship_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_AdditionalStructures_Pool] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_AdditionalStructures_Pool_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolType] = new UIElement
        {
            Strategy = LocatorType.XPath,
            Locators = ["//label[contains(@for,'PL_PoolType_{0}')]"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "AboveGround",
            DependsOn = PL_AdditionalStructures_Pool,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolFeatures_DivingBoard] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_PoolFeatures_DivingBoard_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [RemovableLockableLadder] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='RemovableLockableLadder_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolFeatures_Slide] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_PoolFeatures_Slide_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolFeatures_4ftFence] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_PoolFeatures_4ftFence_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolFeatures_SurroundingWall] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_PoolFeatures_SurroundingWall_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolFeatures_ScreenEnclosure] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_PoolFeatures_ScreenEnclosure_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_PoolFeatures_OtherPoolBarrier] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_PoolFeatures_OtherPoolBarrier_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_HeatedByOil] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_HeatedByOil_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)],
            Label= "Previously heated by oil"
        },
        [PL_AdditionalStructures_HotTub] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_AdditionalStructures_HotTub_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [PL_HotTubFeatures_4ftFence] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_HotTubFeatures_4ftFence_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [PL_HotTubFeatures_ScreenEnclosure] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_HotTubFeatures_ScreenEnclosure_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [PL_HotTubFeatures_SurroundingWall] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_HotTubFeatures_SurroundingWall_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [PL_HotTubFeatures_OtherHotTubBarrier] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_HotTubFeatures_OtherHotTubBarrier_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [PL_HotTubFeatures_LockableLid] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_HotTubFeatures_LockableLid_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [PL_AdditionalStructures_Trampoline] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PL_AdditionalStructures_Trampoline_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [PL_NettedTrampoline] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PL_NettedTrampoline']"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "",
            Pages = []
        },
        [PL_TrampolineInFence] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='PL_TrampolineInFence']"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "",
            Pages = []
        },
        [PerimeterSecurityDD] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='PerimeterSecurityDD_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = [typeof(HQXConsumer_DetailsPage)],
            Label = "Includes perimeter fencing"
        },
        // Parent of the "animals on the premises" group. Despite the "_None" name, its DOM label is
        // "There are farm animals or exotic pets living on this property"; checking it reveals the
        // exotic/farm/dog child checkboxes. Defaults to false so a normal Details fill leaves it
        // unchecked; tests that want a child override the relevant field to "true".
        [AnimalsOnThePremises_None] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='AnimalsOnThePremises_None_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Label = "There are farm animals or exotic pets living on this property",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [AnimalsOnThePremises_Exotic] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='AnimalsOnThePremises_Exotic_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Label = "There are exotic pets living at this home",
            // Revealed only once the parent is checked. DependsOn gates the fill: a normal Details
            // fill (parent = false) skips this hidden child; when a test sets the parent to "true",
            // FillForm orders the parent first, then fills this child.
            DependsOn = AnimalsOnThePremises_None,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [AnimalsOnThePremises_Farm1to2] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='AnimalsOnThePremises_Farm1to2_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [AnimalsOnThePremises_Farm3orMore] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='AnimalsOnThePremises_Farm3orMore_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            // Same reveal as the exotic-pets child: gated on the parent so a normal Details fill skips it.
            DependsOn = AnimalsOnThePremises_None,
            DependsOnValue = "true",
            Pages = [typeof(HQXConsumer_DetailsPage)]
        },
        [AnimalsOnThePremises_Dogs] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["label[for='AnimalsOnThePremises_Dogs_label']"],
            FieldType = UIFieldType.Checkbox,
            DefaultValue = "false",
            Pages = []
        },
        [NumberOfDogsOnPremises] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='NumberOfDogsOnPremises'][type='text'].unmasked-input"],
            FieldType = UIFieldType.Input,
            DefaultValue = "1",
            Pages = []
        },
        [DogsWithBiteHistory] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["input[name='DogsWithBiteHistory']"],
            FieldType = UIFieldType.Radio,
            DefaultValue = "No",
            Pages = []
        },
        [DogsBreedsSelection] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = ["ng-select[name='DogsBreedsSelection[]']"],
            FieldType = UIFieldType.MultiDropdown,
            DefaultValue = "",
            Pages = []
        },
        // Section Edit/View All Buttons
        [PropertySectionEditViewAll] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = [".section.property .section-toggle"],
            FieldType = UIFieldType.Button,
            DefaultValue = "",
            Pages = []
        },
        [ExteriorSectionEditViewAll] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = [".section.exterior .section-toggle"],
            FieldType = UIFieldType.Button,
            DefaultValue = "",
            Pages = []
        },
        [InteriorSectionEditViewAll] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = [".section.interior .section-toggle"],
            FieldType = UIFieldType.Button,
            DefaultValue = "",
            Pages = []
        },
        [PersonalInfoSectionEditViewAll] = new UIElement
        {
            Strategy = LocatorType.CSS,
            Locators = [".section.personal-info .section-toggle"],
            FieldType = UIFieldType.Button,
            DefaultValue = "",
            Pages = []
        },
    };

}