namespace Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData;

public partial class FieldsNameHQXConsumer
{
    public const string SingleFamilyHome = nameof(SingleFamilyHome);
    public const string NumberOfClaimsHistoryYesNo = nameof(NumberOfClaimsHistoryYesNo);
    public const string PLHighRiseCondo = nameof(PLHighRiseCondo);
    public const string PL_NumberOfFloors = nameof(PL_NumberOfFloors);
    public const string PLFloorNumber = nameof(PLFloorNumber);
    public const string PLPersonalProperty = nameof(PLPersonalProperty);

    // "Utilities replaced" question set (feature flag: ba_utilities-replaced-question).
    // Presented on Discounts only in FL / HO3 when the home's yearBuilt is 20-49 years old.
    // Parent Yes/No question; selecting Yes reveals the three utility "update" child questions.
    // Field names match the rendered DOM ids/names verbatim — including the backend's
    // "PLElectircalUpdated" typo and the "PLHeatingUpdate" (no trailing 'd') spelling.
    // (PLPlumbingUpdated already lives in the shared FieldNames.)
    public const string UtilitiesUpdated = nameof(UtilitiesUpdated);
    public const string PLHeatingUpdate = nameof(PLHeatingUpdate);
    public const string PLElectircalUpdated = nameof(PLElectircalUpdated);

    //Buttons & Links
    public const string PopupCloseButton = nameof(PopupCloseButton);
    public const string CCPALink = nameof(CCPALink);
    public const string CANoticeLink = nameof(CANoticeLink);
    public const string DoNotSellLink = nameof(DoNotSellLink);
    public const string EnterAddressManuallyLink = nameof(EnterAddressManuallyLink);

    // FSD 8.1.1 calls this "Back to Progressive"; the UI renders it as
    // "Exit home quote & see auto rate now".
    public const string ExitToAutoQuoteLink = nameof(ExitToAutoQuoteLink);

    // Section Edit/View All Buttons
    public const string PropertySectionEditViewAll = nameof(PropertySectionEditViewAll);
    public const string ExteriorSectionEditViewAll = nameof(ExteriorSectionEditViewAll);
    public const string InteriorSectionEditViewAll = nameof(InteriorSectionEditViewAll);
    public const string PersonalInfoSectionEditViewAll = nameof(PersonalInfoSectionEditViewAll);
    public const string ProgressivePreferences1 = nameof(ProgressivePreferences1);
    public const string ProgressivePreferences2 = nameof(ProgressivePreferences2);

}